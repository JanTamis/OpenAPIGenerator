using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using OpenAPIGenerator.Builders;
using OpenAPIGenerator.Enumerators;
using OpenAPIGenerator.Extensions;
using OpenAPIGenerator.Helpers;

namespace OpenAPIGenerator;

public static class OpenApiParser
{
	public static string Parse(OpenApiDocument document, string rootNamespace)
	{
		var typeList = GetTypes(document);

		var typeDictionary = typeList.ToDictionary(t => t.Key, t => t.Value, new OpenApiSchemaComparer());

		var types = typeList
			.OrderBy(o => o.Value)
			.Select(s => TypeHelper.ToType(s.Key, s.Value, typeDictionary));

		return $"""
			{Builder.ToString(CreateClient(document, rootNamespace))}

			#region Schema's

			{String.Join("\n\n", types.Select(Builder.ToString))}

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
			Modifiers = TypeAttributes.Partial,
			Usings =
			[
				"System",
				"System.Linq",
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
			Summary = TypeHelper.ParseComment(document.Info.Description),
			Properties = [Builder.Property("HttpClient", "Client", modifier: PropertyModifier.Get, accessModifier: AccessModifier.Private)],
			Constructors =
			[
				new ConstructorBuilder
				{
					Parameters = defaultHeaders.Select(s => new ParameterBuilder("string", s.Id) { Documentation = TypeHelper.ParseComment(s.Description) }),
					Content =
					[
						Builder.Line("Client = new HttpClient()"),
						Builder.Block(constructorContent, ";"),
					],
				}
			],
			Methods = ParsePaths(document.Paths).Append(Builder.Method("Dispose", Builder.Line("Client.Dispose();")))
				.Prepend(Builder.Method("ProcessResponse", "void", false, AccessModifier.None, [new ParameterBuilder("HttpClient", "client"), new ParameterBuilder("HttpResponseMessage", "response")], []) with { IsPartial = true })
				.Prepend(Builder.Method("PrepareRequest", "void", false, AccessModifier.None, [new ParameterBuilder("HttpClient", "client"), new ParameterBuilder("HttpRequestMessage", "request")], []) with { IsPartial = true }),
		};

		type.Methods = type.Methods.Append(Builder.Method("ParseResponse<T>", "virtual Task<T?>", false, AccessModifier.Public,
			[Builder.Parameter("HttpResponseMessage", "response"), Builder.Parameter("CancellationToken", "token")],
			[Builder.Line("return response.Content.ReadFromJsonAsync<T>(token);")]));

		type.Methods = type.Methods
			.Concat(document.Paths.Values
				.SelectMany(s => s.Operations)
				.Select(s => s.Value.RequestBody)
				.Where(w => TryGetBody(w, out _))
				.SelectMany(s => s?.Content?.Values?.Select(x => x.Schema) ?? [])
				.Distinct()
				.Select(GetValidation)
				.Where(w => w.Content.Any()));

		return type;
	}

	private static IEnumerable<MethodBuilder> ParsePaths(OpenApiPaths paths)
	{
		return paths
			.SelectMany(s => s.Value.Operations
				.Select(x => ParseOperation(s.Key, x.Key, x.Value)));
	}

	private static MethodBuilder ParseOperation(string path, OperationType type, OpenApiOperation operation)
	{
		var method = Builder.Method($"{Builder.ToTypeName(operation.OperationId)}Async") with
		{
			Summary = TypeHelper.ParseComment(operation.Description),
			IsAsync = true,
			ReturnType = operation.Responses
				.Where(a => Int32.TryParse(a.Key, out var code) && code is >= 200 and <= 299)
				.Select(s => Builder.ToTypeName(TypeHelper.GetTypeName(s.Value.Content.Values.FirstOrDefault()?.Schema)))
				.First() + '?',
			Content = PathHelper.ParseRequestPath(path, operation.Parameters),
		};

		var parameters = new List<ParameterBuilder>();
		var wasAdded = operation.RequestBody is null;
		var hasBody = TryGetBody(operation.RequestBody, out var mediaType);

		var (hasHoles, hasQueryHoles) = PathHelper.GetPathHoles(operation.Parameters);

		foreach (var parameterGroup in operation.Parameters.GroupBy(g => g.Required))
		{
			foreach (var parameter in parameterGroup)
			{
				parameters.Add(Builder.Parameter(TypeHelper.GetTypeName(parameter) + (parameter.Required ? String.Empty : "?"), parameter.Name, parameter.Required ? null : "null", documentation: TypeHelper.ParseParameterComment(parameter)));
			}

			if (!wasAdded && operation.RequestBody!.Required == parameterGroup.Key && hasBody)
			{
				var typeName = TypeHelper.GetTypeName(mediaType.Value.Schema);

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
			var typeName = TypeHelper.GetTypeName(mediaType.Value.Schema);

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
			var typeName = TypeHelper.GetTypeName(mediaTypes.Value.Schema);

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
			if (!hasHoles && !hasQueryHoles)
			{
				Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{type}, \"{path}\");"));
			}
			else
			{
				Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{type}, url);"));
			}
		}

		Builder.Append(method, ParseHeaders(operation.Parameters.Where(w => w.In == ParameterLocation.Header).ToList()));

		if (hasBody)
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

			Builder.Append(method, Builder.WhiteLine());
		}
		else if (operation.Parameters.Any(a => a.In is ParameterLocation.Header))
		{
			Builder.Append(method, Builder.WhiteLine());
		}

		//ParseResponse(operation.Responses, method, false);

		var hasReturnType = operation.Responses.Any(a => Int32.TryParse(a.Key, out var code) && code is >= 200 and <= 299 && a.Value.Content.Values.FirstOrDefault()?.Schema is not null);

		Builder.Append(method, Builder.Line("PrepareRequest(Client, request);"));
		Builder.Append(method, Builder.WhiteLine());

		Builder.Append(method, Builder.Line("using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);"));
		Builder.Append(method, Builder.Line("ProcessResponse(Client, response);"));
		Builder.Append(method, Builder.WhiteLine());

		if (operation.Responses.Any(a => Int32.TryParse(a.Key, out var code) && code is < 200 or > 299))
		{
			Builder.Append(method, Builder.Line("var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);"));
			Builder.Append(method, Builder.WhiteLine());
			Builder.Append(method, Builder.Line("foreach (var header in response.Content.Headers)"));
			Builder.Append(method, Builder.Block(Builder.Line("headers.Add(header.Key, header.Value);")));

			Builder.Append(method, Builder.WhiteLine());
		}

		Builder.Append(method, ParseResponse(operation.Responses, hasReturnType));

		return method;
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
				var type = Builder.ToTypeName(TypeHelper.GetTypeName(s.Value.Content.Values.FirstOrDefault()?.Schema));
				var padding = new String(' ', length - result.Length);

				var caseText = s.Value.Description.TrimEnd().Replace("\n", @"\n");

				if (String.IsNullOrWhiteSpace(type))
				{
					if (hasReturnType)
					{
						return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException(\"{caseText}\", response, headers),");
					}

					return Builder.Line($"HttpStatusCode.{result}{padding} => new ApiException(\"{caseText}\", response, headers),");
				}

				if (code is >= 200 and <= 299)
				{
					if (type is "byte[]")
					{
						return Builder.Line($"HttpStatusCode.{result}{padding} => await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false),");
					}

					return Builder.Line($"HttpStatusCode.{result}{padding} => await ParseResponse<{type}>(response, token).ConfigureAwait(false),");
				}

				if (hasReturnType)
				{
					return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException<{type}>(\"{caseText}\", response, headers, await ParseResponse<{type}>(response, token).ConfigureAwait(false)),");
				}

				return Builder.Line($"HttpStatusCode.{result}{padding} => new ApiException<{type}>(\"{caseText}\", response, headers, await ParseResponse<{type}>(response, token).ConfigureAwait(false)),");
			})
			.Append(Builder.Line($"_{new String(' ', length + "HttpStatusCode".Length)} => {(hasReturnType ? "throw " : String.Empty)}new InvalidOperationException(\"Unknown status code has been returned.\"),")), ";");
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

	private static MethodBuilder GetValidation(OpenApiSchema schema)
	{
		var typeName = TypeHelper.GetTypeName(schema);

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
			var type = TypeHelper.GetTypeName(item.Value);
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

	private static IEnumerable<KeyValuePair<string, OpenApiSchema>> GetPropertySchemas(OpenApiSchema schema, string propertyName)
	{
		yield return new KeyValuePair<string, OpenApiSchema>(propertyName, schema);

		if (schema.Properties is null)
		{
			yield break;
		}

		foreach (var item in schema.Properties)
		{
			if (item.Value.Type == "object")
			{
				foreach (var subItem in GetPropertySchemas(item.Value, item.Key))
				{
					yield return subItem;
				}
			}
		}
	}

	private static IEnumerable<KeyValuePair<OpenApiSchema, string>> GetBodies(OpenApiDocument document)
	{
		return document.Paths.Values
			.SelectMany(s => s.Operations)
			.Where(w => TryGetBody(w.Value.RequestBody, out _))
			.SelectMany(s => s.Value?.RequestBody?.Content?.Values?.Select(x => x.Schema) ?? [])
			.Where(w => w is not null && document.Components.Schemas.All(a => a.Value != w))
			.Select(s => new KeyValuePair<OpenApiSchema, string>(s, TypeHelper.GetTypeName(s)));
	}

	private static IEnumerable<KeyValuePair<OpenApiSchema, string>> GetSchemas(OpenApiDocument document)
	{
		return document.Components.Schemas
			.Where(w => w.Value.Type is "object" or null)
			.SelectMany(s => GetPropertySchemas(s.Value, s.Key))
			.Select(s => new KeyValuePair<OpenApiSchema, string>(s.Value, Builder.ToTypeName(s.Key)));
	}

	private static IEnumerable<KeyValuePair<OpenApiSchema, string>> GetOperations(OpenApiDocument document)
	{
		return document.Paths.Values
			.SelectMany(s => s.Operations)
			.SelectMany(s => s.Value.Parameters)
			.Where(w => w.Schema is not null && w.Schema.Enum.Any() && w.Schema.Reference is null)
			.Select(s => new KeyValuePair<OpenApiSchema, string>(s.Schema, TypeHelper.GetTypeName(s)));
	}

	private static List<KeyValuePair<OpenApiSchema, string>> GetTypes(OpenApiDocument document)
	{
		return GetBodies(document)
			.Concat(GetSchemas(document))
			.Concat(GetOperations(document))
			.GroupBy(g => g.Value)
			.SelectMany(s => s
				.Index((item, index) =>
				{
					if (index != 0)
					{
						return new KeyValuePair<OpenApiSchema, string>(item.Key, item.Value + (index + 1));
					}

					return item;
				}))
			.DistinctBy(d => d.Key)
			.ToList();
	}
}