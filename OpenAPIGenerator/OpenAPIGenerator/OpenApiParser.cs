using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Syntax;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenAPIGenerator.Builders;

namespace OpenAPIGenerator;

public static class OpenApiParser
{
	public static string Parse(OpenApiDocument document, string rootNamespace)
	{
		return $"""
			{Builder.ToString(CreateClient(document, rootNamespace))}

			#region Schema's

			{String.Join("\n\n", document.Components.Schemas
				.Where(w => w.Value.Type is "object" or null)
				.Select(s => ToType(s.Value, s.Key)))}

			#endregion
			""";
	}

	private static TypeBuilder CreateClient(OpenApiDocument document, string rootNamespace)
	{
		var defaultHeaders = document.SecurityRequirements
			.SelectMany(s => s.Keys)
			.Where(w => w.In == ParameterLocation.Header)
			.Select(s => (s.Reference.Id, s.Description))
			.Distinct()
			.ToList();

		var constructorContent = new List<IBuilder>
		{
			Builder.Line($"BaseAddress = new Uri(\"{document.Servers[0].Url}\"),")
		};

		if (defaultHeaders.Any())
		{
			constructorContent.Add(Builder.Line("DefaultRequestHeaders ="));
			constructorContent.Add(Builder.Block(defaultHeaders
				.Select(s => Builder.Line($"{{ \"{s.Id}\", {s.Id} }},"))));
		}

		var type = new TypeBuilder(document.Info.Title)
		{
			Usings =
			[
				"System",
				"System.Text.RegularExpressions",
				"System.ComponentModel.DataAnnotations",
				"System.Text.Json.Serialization",
				"System.Runtime.Serialization",
				"System.Net",
				"System.Net.Http",
				"System.Net.Http.Json",
				"System.Net.Http.Headers",
				"System.Text",
				"System.Threading",
				"System.Threading.Tasks",
				"System.Runtime.CompilerServices",
				"System.Collections.Generic",
			],
			Namespace = rootNamespace,
			Summary = ParseComment(document.Info.Description),
			Properties = [Builder.Property("HttpClient", "Client", modifier: PropertyModifier.Get, accessModifier: AccessModifier.Private)],
			Constructors =
			[
				new ConstructorBuilder
				{
					Parameters = defaultHeaders.Select(s => new ParameterBuilder("string", s.Id) { Documentation = ParseComment(s.Description) }),
					Content =
					[
						Builder.Line("Client = new HttpClient()"),
						Builder.Block(constructorContent, ";"),
					],
				}
			],
			Methods = ParsePaths(document.Paths).Append(Builder.Method("Dispose", Builder.Line("Client.Dispose();"))),
		};

		type.Methods = type.Methods.Append(Builder.Method("ParseResponse<T>", "virtual Task<T?>", false, AccessModifier.Public,
			[Builder.Parameter("HttpResponseMessage", "response"), Builder.Parameter("CancellationToken", "token")], [Builder.Line("return response.Content.ReadFromJsonAsync<T>(token);")]));

		type.Methods = type.Methods
			.Concat(document.Paths.Values
				.SelectMany(s => s.Operations)
				.Select(s => s.Value.RequestBody)
				.SelectMany(s => s?.Content?.Values?.Select(x => x.Schema) ?? [])
				.Distinct()
				.Select(GetValidation)
				.Where(w => w.Content.Any()));

		// type.Methods = type.Methods
		// 	.Concat(document.Components.Schemas
		// 		.Select(s => GetValidation(s.Value))
		// 		.Where(w => w.Content.Any()));

		return type;
	}

	private static IEnumerable<MethodBuilder> ParsePaths(OpenApiPaths paths)
	{
		return paths.SelectMany(s => ParsePath(s.Key, s.Value));
	}

	private static IEnumerable<MethodBuilder> ParsePath(string path, OpenApiPathItem pathItem)
	{
		return pathItem.Operations.Select(s => ParseOperation(path, s.Key, s.Value));
	}

	private static MethodBuilder ParseOperation(string path, OperationType type, OpenApiOperation operation)
	{
		var method = Builder.Method($"{Builder.ToTypeName(operation.OperationId)}Async") with
		{
			Summary = ParseComment(operation.Description),
			IsAsync = true,
			ReturnType = operation.Responses
				.Where(a => Int32.TryParse(a.Key, out var code) && code is >= 200 and <= 299)
				.Select(s => Builder.ToTypeName(GetTypeName(s.Value.Content.Values.FirstOrDefault()?.Schema)))
				.First() + '?',
			Content = ParseRequestPath(path, operation.Parameters),
		};

		var parameters = new List<ParameterBuilder>();
		var wasAdded = operation.RequestBody is null;
		var hasBody = TryGetBody(operation.RequestBody, out var mediaType);

		foreach (var parameterGroup in operation.Parameters.GroupBy(g => g.Required))
		{
			foreach (var parameter in parameterGroup)
			{
				parameters.Add(Builder.Parameter(GetTypeName(parameter.Schema ?? parameter.Content.FirstOrDefault().Value.Schema) + (parameter.Required ? String.Empty : "?"), parameter.Name, parameter.Required ? null : "null", documentation: ParseParameterComment(parameter)));
			}

			if (!wasAdded && operation.RequestBody!.Required == parameterGroup.Key && hasBody)
			{
				var typeName = GetTypeName(mediaType.Value.Schema);

				if (operation.RequestBody.Required)
				{
					parameters.Add(Builder.Parameter(typeName, "body", null, "The body of the request."));					
				}
				else
				{
					parameters.Add(Builder.Parameter($"{typeName}?", "body", "null", "The body of the request. (optional)"));
				}
					
				wasAdded = true;
			}
		}

		if (!wasAdded && hasBody)
		{
			var typeName = GetTypeName(mediaType.Value.Schema);

			if (operation.RequestBody!.Required)
			{
				parameters.Insert(0, Builder.Parameter(typeName, "body", null, "The body of the request."));
			}
			else
			{
				parameters.Add(Builder.Parameter($"{typeName}?", "body", "null", "The body of the request. (optional)"));
			}
		}

		parameters.Add(Builder.Parameter("CancellationToken", "token", "default", "The cancellation token to cancel the request (optional)."));
		
		method.Parameters = parameters;

		var parameterCheck = operation.Parameters
			.Where(w => w.Required)
			.Select(ParseParametersCheck)
			.Where(w => w != null)
			.OfType<IBuilder>()
			.ToList();

		if (operation.RequestBody?.Required == true)
		{
			parameterCheck.Add(Builder.Line("ArgumentNullException.ThrowIfNull(body);"));
		}

		if (parameterCheck.Count > 0)
		{
			parameterCheck.Add(Builder.WhiteLine());
		}

		if (TryGetBody(operation.RequestBody, out var mediaTypes))
		{
			var typeName = GetTypeName(mediaTypes.Value.Schema);

			if (GetValidation(mediaTypes.Value.Schema).Content.Any())
			{
				if (operation.RequestBody!.Required)
				{
					parameterCheck.Add(Builder.Line($"Validate{typeName}(body);"));
				}
				else
				{
					parameterCheck.Add(Builder.If("body is not null", Builder.Line($"Validate{typeName}(body);")));
				}

				parameterCheck.Add(Builder.WhiteLine());
			}
		}

		parameterCheck.AddRange(method.Content);
		method.Content = parameterCheck;

		if (operation.Parameters.Any(a => !a.Required && a.In is ParameterLocation.Query)) //  || operation.Parameters.Any(a => a.In is ParameterLocation.Path))
		{
			Builder.Append(method, Builder.WhiteLine());
		}

		if (operation.Parameters.Any(w => w.In is ParameterLocation.Query or ParameterLocation.Path))
		{
			Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{type}, new Uri(url.ToStringAndClear()));"));
		}
		else
		{
			Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{type}, url);"));
		}

		Builder.Append(method, ParseHeaders(operation.Parameters.Where(w => w.In == ParameterLocation.Header).ToList()));

		if (TryGetBody(operation.RequestBody, out _))
		{
			Builder.Append(method, Builder.WhiteLine());

			if (operation.RequestBody?.Required == true)
			{
				Builder.Append(method, Builder.Line($"request.Content = JsonContent.Create(body, MediaTypeHeaderValue.Parse(\"{mediaTypes.Key}\"));"));
			}
			else
			{
				Builder.Append(method, Builder.If("body is not null",
					[Builder.Line($"request.Content = JsonContent.Create(body, MediaTypeHeaderValue.Parse(\"{mediaTypes}\"));")]));
			}
		}

		//ParseResponse(operation.Responses, method, false);

		var hasReturnType = operation.Responses.Any(a => Int32.TryParse(a.Key, out var code) && code is >= 200 and <= 299 && a.Value.Content.Values.FirstOrDefault()?.Schema is not null);

		// if (hasHeaders)
		// {
		Builder.Append(method, Builder.WhiteLine());
		// }

		Builder.Append(method, Builder.Line("using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);"));
		Builder.Append(method, Builder.WhiteLine());
		Builder.Append(method, ParseResponse(operation.Responses, hasReturnType));


		return method;
	}

	private static IEnumerable<IBuilder> ParseRequestPath(string path, IList<OpenApiParameter> parameters)
	{
		var hasQueryHoles = false;
		var hasHoles = false;
		var result = path;

		var optionalParameters = parameters.Where(w => !w.Required && w.In == ParameterLocation.Query).ToList();

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Path))
		{
			result = result.Replace('{' + parameter.Name + '}', "{" + Builder.ToParameterName(parameter.Name) + "}");
			hasHoles = true;
		}

		var isFirst = true;

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Query && w.Required))
		{
			if (isFirst)
			{
				result += '?';
				isFirst = false;
			}

			var name = Builder.ToParameterName(parameter.Name);

			result += $"{parameter.Name}={{{name}}}&";
			hasQueryHoles = true;
		}

		if (!hasQueryHoles)
		{
			result += "?";
		}

		if (!optionalParameters.Any())
		{
			result = result.TrimEnd('&', '?');
		}

		if (hasQueryHoles || hasHoles || optionalParameters.Any())
		{
			yield return Builder.Line($"var url = new UrlBuilder($\"{result.TrimEnd('?')}\");");
		}
		else
		{
			yield return Builder.Line($"var url = \"{result.TrimEnd('?', '&')}\";");
		}

		if (optionalParameters.Any())
		{
			yield return Builder.WhiteLine();
		}

		for (var i = 0; i < optionalParameters.Count; i++)
		{
			var parameter = optionalParameters[i];

			yield return Builder.Line($"url.AppendQuery(\"{parameter.Name}\", {Builder.ToParameterName(parameter.Name)});");
		}
	}

	private static IEnumerable<IBuilder> ParseHeaders(IList<OpenApiParameter> headers)
	{
		var hasRequired = headers.Any(w => w.Required);

		var result = headers
			.OrderByDescending(o => o.Required)
			.SelectMany<OpenApiParameter, IBuilder>(s =>
			{
				var parameterName = Builder.ToParameterName(s.Name);

				var headerText = s.Schema.Format switch
				{
					"uri"       => $"{parameterName}.ToString()",
					"uuid"      => $"{parameterName}.ToString()",
					"int32"     => $"{parameterName}.ToString()",
					"int64"     => $"{parameterName}.ToString()",
					"float"     => $"{parameterName}.ToString()",
					"double"    => $"{parameterName}.ToString()",
					"byte"      => $"Convert.ToBase64String({parameterName})",
					"binary"    => $"Convert.ToBase64String({parameterName})",
					"date"      => $"{parameterName}.ToString()",
					"date-time" => $"{parameterName}.ToString()",
					"password"  => $"{parameterName}.ToString()",
					_           => parameterName,
				};

				if (s.Required)
				{
					return [Builder.Line($"request.Headers.Add(\"{s.Name}\", {headerText});")];
				}

				return
				[
					Builder.WhiteLine(),
					Builder.If($"{parameterName} != null",
					[
						Builder.Line($"request.Headers.Add(\"{s.Name}\", {headerText});"),
					]),
				];
			});

		if (hasRequired && headers.Any())
		{
			result = result.Prepend(Builder.WhiteLine());
		}

		return result;
	}

	private static IEnumerable<IBuilder> ParseResponse(OpenApiResponses responses, bool hasReturnType)
	{
		var length = responses.Max(m =>
		{
			var code = Int32.Parse(m.Key);
			var result = ((System.Net.HttpStatusCode) code).ToString();

			return result.Length;
		});

		if (hasReturnType)
		{
			yield return Builder.Line($"return response.StatusCode switch");
		}
		else
		{
			yield return Builder.Line($"throw response.StatusCode switch");
		}

		yield return Builder.Block(responses
			.Select(s =>
			{
				var code = Int32.Parse(s.Key);
				var result = ((System.Net.HttpStatusCode) code).ToString();
				var type = Builder.ToTypeName(GetTypeName(s.Value.Content.Values.FirstOrDefault()?.Schema));
				var padding = new String(' ', length - result.Length);

				var caseText = s.Value.Description.TrimEnd().Replace("\n", @"\n");

				if (String.IsNullOrWhiteSpace(type))
				{
					if (hasReturnType)
					{
						return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException(\"{caseText}\", response),");
					}

					return Builder.Line($"HttpStatusCode.{result}{padding} => new ApiException(\"{caseText}\", response),");
				}

				if (code is >= 200 and <= 299)
				{
					if (type is "byte[]")
					{
						return Builder.Line($"HttpStatusCode.{result}{padding} => await response.Content.ReadAsByteArrayAsync(token),");	
					}
					
					return Builder.Line($"HttpStatusCode.{result}{padding} => await ParseResponse<{type}>(response, token),");
				}

				if (hasReturnType)
				{
					return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException<{type}>(\"{caseText}\", response, await ParseResponse<{type}>(response, token).ConfigureAwait(false)),");
				}

				return Builder.Line($"HttpStatusCode.{result}{padding} => new ApiException<{type}>(\"{caseText}\", response, await ParseResponse<{type}>(response, token).ConfigureAwait(false)),");
			})
			.Append(Builder.Line($"_{new String(' ', length + "HttpStatusCode".Length)} => {(hasReturnType ? "throw " : String.Empty)}new InvalidOperationException(\"Unknown status code has been returned.\"),")), ";");
	}

	private static string ToType(OpenApiSchema schema, string typeName)
	{
		typeName = Builder.ToTypeName(typeName);

		if (schema.Enum.Any())
		{
			var enumBuilder = new EnumBuilder
			{
				TypeName = typeName,
				Summary = schema.Description,
				Attributes = [Builder.Attribute("JsonConverter", $"typeof(JsonStringEnumConverter<{typeName}>)")],
				Members = schema.Enum
					.OfType<OpenApiString>()
					.Select(s => new EnumMemberBuilder(s.Value, null, Builder.Attribute("EnumMember", $"Value = \"{s.Value}\"")))
			};

			return Builder.ToString(enumBuilder);
		}

		var builder = new TypeBuilder(typeName)
		{
			Summary = ParseComment(schema.Description),
			Properties = schema.Properties
				.Select(s =>
				{
					var isRequired = schema.Required.Contains(s.Key);
					var type = GetTypeName(s.Value);
					var isString = type == "string";

					if (!isRequired)
					{
						type += "?";
					}
					else
					{
						type = $"required {type}";
					}

					var attributes = new List<AttributeBuilder>()
					{
						Builder.Attribute("JsonPropertyName", $"\"{s.Key}\""),
					};

					if (isString && !String.IsNullOrWhiteSpace(s.Value.Pattern))
					{
						attributes.Add(Builder.Attribute("RegularExpression", $"@\"{s.Value.Pattern}\""));
					}

					if (isString && s.Value.MaxLength.HasValue || s.Value.MinLength.HasValue)
					{
						var attribute = Builder.Attribute("StringLength");

						if (s.Value.MaxLength.HasValue)
						{
							attribute.Parameters = attribute.Parameters.Append(s.Value.MaxLength.Value.ToString());
						}

						if (s.Value.MinLength.HasValue && s.Value.MinLength.Value > 0)
						{
							attribute.Parameters = attribute.Parameters.Append($"MinimumLength = {s.Value.MinLength.Value}");
						}

						attributes.Add(attribute);
					}
					else
					{
						if (s.Value.MaxLength.HasValue)
						{
							attributes.Add(Builder.Attribute("MaxLength", s.Value.MaxLength.Value.ToString()));
						}

						if (s.Value.MinLength.HasValue)
						{
							attributes.Add(Builder.Attribute("MinLength", s.Value.MinLength.Value.ToString()));
						}
					}
					
					attributes.Add(Builder.Attribute("JsonIgnore", "Condition = JsonIgnoreCondition.WhenWritingDefault"));

					return Builder.Property(type, s.Key) with
					{
						Summary = ParseComment(s.Value.Description),
						Attributes = attributes,
					};
				}),
		};

		builder.Properties = builder.Properties.Concat([Builder.Property("IDictionary<string, object>", "AdditionalProperties", null, Builder.Attribute("JsonExtensionData"))]);

		return Builder.ToString(builder);
	}

	private static string GetTypeName(OpenApiSchema? schema)
	{
		if (schema is null)
		{
			return String.Empty;
		}

		var type = schema.Type;

		if (type is "integer")
		{
			type = "int";
		}

		if (schema.Items is not null)
		{
			if (schema.UniqueItems == true)
			{
				type = $"HashSet<{GetTypeName(schema.Items)}>";
			}
			else
			{
				type = $"{GetTypeName(schema.Items)}[]";
			}
		}
		else if (schema is { Type: "string", Format: "byte" })
		{
			return "byte[]" + (schema.Nullable ? "?" : "");
		}
		else if (schema.Type is not null && schema.Type != "object")
		{
			if (!String.IsNullOrEmpty(schema.Format))
			{
				type = schema.Format;
			}
		}
		else if (schema.Reference is not null)
		{
			type = schema.Reference.Id;
		}

		return type.ToLower() switch
		{
			"uri"               => "Uri",
			"uuid"              => "Guid",
			"int32"             => "int",
			"int64" or "long"   => "long",
			"float"             => "float",
			"double"            => "double",
			"byte"              => "byte",
			"boolean" or "bool" => "bool",
			"binary"            => "byte[]",
			"date"              => "DateTime",
			"date-time"         => "DateTime",
			"password"          => "string",
			_                   => Builder.ToTypeName(type),
		} + (schema.Nullable ? "?" : "");
	}

	private static IBuilder? ParseParametersCheck(OpenApiParameter parameter)
	{
		var schema = parameter.Schema;
		var parameterName = Builder.ToParameterName(parameter.Name);

		if (schema is null)
		{
			return null;
		}

		if (schema.Items is null && schema.Type is not null && schema.Type != "object")
		{
			if (schema is { Type: "string", Format: "byte" })
			{
				return Builder.Line($"ArgumentNullException.ThrowIfNull({parameterName});");
			}

			if (!String.IsNullOrEmpty(schema.Format))
			{
				switch (schema.Format)
				{
					case "uri":
					case "uuid":
					case "int32":
					case "int64":
					case "float":
					case "double":
					case "byte":
					case "date":
					case "date-time":
						return null;
				}
			}
		}

		return Builder.Line($"ArgumentNullException.ThrowIfNull({parameterName});");
	}

	private static string? ParseParameterComment(OpenApiParameter parameter)
	{
		var result = ParseComment(parameter.Description);

		if (parameter.Example is OpenApiString example)
		{
			result += $"<br/> Example: {example.Value}";
		}

		return result;
	}

	private static string? ParseComment(string? comment)
	{
		if (comment is null)
		{
			return null;
		}

		var document = Markdown.Parse(comment);
		var builder = new IndentedStringBuilder();

		foreach (var item in document)
		{
			switch (item)
			{
				case ParagraphBlock paragraph:
					// builder.AppendLine("<para>");

					// using (builder.Indent())
					// {
					builder.AppendLines(GetText(paragraph));
					// }

					// builder.AppendLine("</para>");
					break;

				case ListBlock list:
				{
					builder.AppendLine("<list type=\"bullet\">");

					using (builder.Indent())
					{
						foreach (var listItem in list)
						{
							builder.AppendLine("<item>");

							using (builder.Indent())
							{
								builder.AppendLines($"<description>{GetTextRaw(listItem).Replace("<br>", String.Empty).TrimStart(list.BulletType).Trim()}</description>");
							}

							builder.AppendLine("</item>");
						}
					}

					builder.AppendLine("</list>");
					break;
				}

				case HeadingBlock header:
					builder.AppendLines($"<b>{GetText(header).TrimStart(header.HeaderChar).Trim()}</b><br/>");
					break;
				case HtmlBlock html:
					// builder.AppendLine("<code>");
					// builder.AppendLines(GetTextRaw(html).Replace("<br>", "<br/>"));
					// builder.AppendLine("</code>");
					break;
				case FencedCodeBlock code:
					var text = GetTextRaw(code).Trim(code.FencedChar).Trim();

					if (text.Contains('\n'))
					{
						builder.AppendLine("<code>");
						builder.AppendLines(text);
						builder.AppendLine("</code>");
					}
					else
					{
						builder.AppendLine($"<c>{text}</c><br/>");
					}

					break;
				default:
					builder.AppendLines(GetText(item));
					break;
			}
		}

		return builder.ToString();

		string GetText(Markdig.Syntax.Block block)
		{
			return GetTextRaw(block).TrimEnd().Replace("\n", "<br/>\n").Replace("<br>", "<br/>\n");
		}

		string GetTextRaw(Markdig.Syntax.Block block)
		{
			return comment.Substring(block.Span.Start, block.Span.Length);
		}
	}

	private static MethodBuilder GetValidation(OpenApiSchema schema)
	{
		var typeName = GetTypeName(schema);

		if (typeName is "object")
		{
			return Builder.Method(String.Empty, []);
		}

		var properties = schema.Properties;

		var method = Builder.Method($"Validate{typeName}", "static void", false, AccessModifier.Private, [Builder.Parameter(typeName, "item")], []);
		var isFirst = true;

		foreach (var item in properties)
		{
			var parameterName = Builder.ToTypeName(item.Key);
			var type = GetTypeName(item.Value);
			var isString = type == "string";

			if (schema.Required.Contains(item.Key))
			{
				AppendValidation($"item.{parameterName} is null",
					$"{{nameof(item.{parameterName})}} is required");
			}

			if (item.Value.MinLength.HasValue && (!isString || item.Value.MinLength > 0) && item.Value.MaxLength.HasValue && (!isString || item.Value.MaxLength > 0))
			{
				AppendValidation($"item.{parameterName} is {{ Length: < {item.Value.MinLength} or > {item.Value.MaxLength} }}",
					$"{{nameof(item.{parameterName})}} was out of range");
			}
			else if (item.Value.MinLength.HasValue && (!isString || item.Value.MinLength > 0))
			{
				AppendValidation($"item.{parameterName} is {{ Length: < {item.Value.MinLength} }}",
					$"the length of {{nameof(item.{parameterName})}} needs to be bigger or equal to {item.Value.MinLength}");
			}
			else if (item.Value.MaxLength.HasValue && (!isString || item.Value.MaxLength > 0))
			{
				AppendValidation($"item.{parameterName} is {{ Length: > {item.Value.MaxLength} }}",
					$"the length of {{nameof(item.{parameterName})}} needs to be smaller or equal to {item.Value.MaxLength}");
			}

			if (isString && !String.IsNullOrWhiteSpace(item.Value.Pattern))
			{
				if (schema.Required.Contains(item.Key))
				{
					AppendValidation($"!Regex.IsMatch(item.{parameterName}, @\"{item.Value.Pattern}\")",
					$"{{nameof(item.{parameterName})}} did not match the pattern");
				}
				else
				{
					AppendValidation($"item.{parameterName} is not null && !Regex.IsMatch(item.{parameterName}, @\"{item.Value.Pattern}\")",
					$"{{nameof(item.{parameterName})}} did not match the pattern");
				}
			}
		}

		return method;

		void AppendValidation(string condition, string message)
		{
			if (isFirst)
			{
				isFirst = false;
			}
			else
			{
				Builder.Append(method, Builder.WhiteLine());
			}

			Builder.Append(method, Builder.If(condition,
				[Builder.Line($"throw new ValidationException($\"{message}\");")]));
		}
	}

	private static bool TryGetBody(OpenApiRequestBody? requestBody, out KeyValuePair<string, OpenApiMediaType> mediaType)
	{
		mediaType = requestBody?.Content?.FirstOrDefault(f => f.Key.Contains("json")) ?? default;
		
		return !String.IsNullOrWhiteSpace(mediaType.Key);
	}
}