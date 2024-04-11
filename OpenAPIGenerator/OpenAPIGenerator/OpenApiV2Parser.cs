// using OpenAPIGenerator.Enumerators;
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.Linq;
// using OpenAPIGenerator.Builders;
// using System.Collections;
// using System.Security.Cryptography;
//
// namespace OpenAPIGenerator.Models.OpenApi.V20;
//
// public static class OpenApiV2Parser
// {
// 	public static string Parse(SwaggerModel model, string rootNamespace)
// 	{
// 		var defaultHeaders = model.SecurityDefinitions
// 			.Where(w => w.Value.In == ParameterLocation.Header);
//
// 		var constructorContent = new List<IBuilder>
// 		{
// 			Builder.Line($"BaseAddress = new Uri(\"{model.Schemes[0]}://{model.Host}{model.BasePath}\"),")
// 		};
//
// 		if (defaultHeaders.Any())
// 		{
// 			constructorContent.Add(Builder.Line("DefaultRequestHeaders ="));
// 			constructorContent.Add(Builder.Block(defaultHeaders
// 				.Select(s => Builder.Line($"{{ \"{s.Key}\", {s.Key} }},"))));
// 		}
// 		
// 		var type = new TypeBuilder(model.Info.Title)
// 		{
// 			Usings =
// 			[
// 				"System.Net",
// 				"System.Net.Http",
// 				"System.Net.Http.Json",
// 				"System.Net.Http.Headers",
// 				"System",
// 				"System.Text",
// 				"System.Threading",
// 				"System.Threading.Tasks",
// 				"System.Runtime.CompilerServices",
// 				"System.Collections.Generic",
// 				$"{rootNamespace}.Models",
// 			],
// 			Namespace = rootNamespace,
// 			Summary = model.Info.Description,
// 			Properties = [Builder.Property("HttpClient", "Client", modifier: PropertyModifier.Get, accessModifier: AccessModifier.Private)],
// 			Constructors =
// 			[
// 				new ConstructorBuilder
// 				{
// 					Parameters = defaultHeaders
// 						.Select(s => new ParameterBuilder("string", s.Key)),
// 					Content =
// 					[
// 						Builder.Line("Client = new HttpClient()"),
// 						Builder.Block(constructorContent, ";"),
// 					],
// 				}
// 			]
// 		};
//
// 		foreach (var path in model.Paths)
// 		{
// 			ParsePath(path.Key, path.Value, type);
// 		}
//
// 		return Builder.ToString(type);
// 	}
//
// 	public static string ParseObject(string name, SchemaModel schema, string defaultNamespace)
// 	{
// 		BaseTypeBuilder type;
//
// 		if (schema.Enum?.Any() ?? false)
// 		{
// 			type = new EnumBuilder
// 			{
// 				TypeName = Builder.ToTypeName(name),
// 				Members = schema.Enum?.Select(s => new EnumMemberBuilder(s.ToString())) ?? []
// 			};
// 		}
// 		else
// 		{
// 			type = new TypeBuilder(Builder.ToTypeName(name))
// 			{
// 				Properties = schema.Properties?
// 					.Select(s => Builder.Property(GetTypeName(s.Value), s.Key, s.Value.Description, Builder.Attribute("JsonPropertyName", $"\"{s.Key}\""))) ?? [],
// 			};
// 		}
//
// 		type.Namespace = $"{defaultNamespace}.Models";
// 		type.Summary = schema.Description;
// 		type.Usings =
// 		[
// 			"System",
// 			"System.Text.Json.Serialization",
// 		];
//
// 		return Builder.ToString(type);
// 	}
//
// 	private static void ParsePath(string requestPath, PathModel path, TypeBuilder type)
// 	{
// 		foreach (var item in path.GetOperations())
// 		{
// 			if (item.Value is not null)
// 			{
// 				ParseOperation(requestPath, item.Value, item.Key, type);
// 			}
// 		}
// 	}
//
// 	private static void ParseOperation(string requestPath, OperationModel path, string operationName, TypeBuilder type)
// 	{
// 		var resultType = Builder.ToTypeName(path.Responses
// 			.Where(w => Int32.TryParse(w.Key, out var code) && code is >= 200 and <= 299)
// 			.Select(s => $"{GetTypeName(s.Value.Schema)}?")
// 			.First());
//
// 		var getRequestPath = ParseRequestPath(requestPath, path.Parameters);
//
// 		var parameters = path.Parameters
// 			.Where(w => !String.IsNullOrWhiteSpace(w.Name))
// 			.OrderByDescending(o => o.Required)
// 			.Select(ParseParameter)
// 			.Append(Builder.Parameter("CancellationToken", "token", "default"));
//
// 		var method = Builder.Method($"{Builder.ToTypeName(path.OperationId)}Async", $"{resultType}?", true, AccessModifier.Public, parameters, [], path.Summary);
// 		var hasQuery = path.Parameters.Any(a => a.In == ParameterLocation.Query && !a.Required);
// 		var hasPath = path.Parameters.Any(a => a.In == ParameterLocation.Path);
// 		var hasHeader = path.Parameters.Any(w => w.In == ParameterLocation.Header) || path.Consumes?.Any() == true || path.Produces?.Any() == true;
// 		var hasForm = path.Parameters.Any(a => a.In == ParameterLocation.FormData);
//
// 		var requiredParameters = path.Parameters
// 			.Where(w => !String.IsNullOrWhiteSpace(w.Name) && w.Required && w.Type == ParameterTypes.String)
// 			.Select(s => Builder.ToParameterName(s.Name));
//
// 		foreach (var item in requiredParameters)
// 		{
// 			Builder.Append(method, Builder.Line($"ArgumentNullException.ThrowIfNull({item});"));
// 		}
//
// 		if (requiredParameters.Any())
// 		{
// 			Builder.Append(method, Builder.WhiteLine());
// 		}
//
// 		if (hasQuery)
// 		{
// 			Builder.Append(method, Builder.Line($"var urlBuilder = new StringBuilder($\"{getRequestPath}\");"));
// 			Builder.Append(method, Builder.WhiteLine());
// 			ParseQuery(path.Parameters, method);
// 		}
//
// 		if (hasHeader || hasForm)
// 		{
// 			if (hasQuery)
// 			{
// 				Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, urlBuilder.ToString());"));
// 			}
// 			else
// 			{
// 				if (hasPath)
// 				{
// 					Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, $\"{getRequestPath}\");"));
// 				}
// 				else
// 				{
// 					Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, \"{getRequestPath}\");"));
// 				}
// 			}
//
// 			if (hasForm)
// 			{
// 				Builder.Append(method, Builder.WhiteLine());
// 				Builder.Append(method, Builder.Line("request.Content = new FormUrlEncodedContent(GetFormContent());"));
// 			}
//
// 			Builder.Append(method, Builder.WhiteLine());
//
// 			foreach (var consume in path.Consumes ?? [])
// 			{
// 				Builder.Append(method, Builder.Line($"request.Headers.Expect.Add(NameValueWithParametersHeaderValue.Parse(\"{consume}\"));"));
// 			}
//
// 			foreach (var consume in path.Produces ?? [])
// 			{
// 				Builder.Append(method, Builder.Line($"request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(\"{consume}\"));"));
// 			}
// 			
// 			ParseHeaders(path.Parameters, method);
//
// 			Builder.Append(method, Builder.WhiteLine());
// 			Builder.Append(method, Builder.Line("using var response = await Client.SendAsync(request, token);"));
// 		}
// 		else
// 		{
// 			if (hasQuery)
// 			{
// 				Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async(urlBuilder.ToString(), token);"));
// 			}
// 			else
// 			{
// 				if (hasPath)
// 				{
// 					Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async($\"{getRequestPath}\", token);"));
// 				}
// 				else
// 				{
// 					Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async(\"{getRequestPath}\", token);"));
// 				}
// 			}
// 		}
//
// 		Builder.Append(method, Builder.WhiteLine());
//
// 		ParseResponse(path.Responses, method, !String.IsNullOrWhiteSpace(resultType));
//
// 		if (hasForm)
// 		{
// 			Builder.Append(method, Builder.WhiteLine());
// 			Builder.Append(method, ParseForm(path.Parameters));
// 		}
//
// 		type.Methods = type.Methods.Append(method);
// 	}
//
// 	private static void ParseResponse(IReadOnlyDictionary<string, ResponseModel> responses, MethodBuilder method, bool hasReturnType)
// 	{
// 		if (hasReturnType)
// 		{
// 			var length = responses.Max(m =>
// 			{
// 				var code = Int32.Parse(m.Key);
// 				var result = ((System.Net.HttpStatusCode) code).ToString();
//
// 				return result.Length;
// 			});
// 			
// 			Builder.Append(method, Builder.Line($"return response.StatusCode switch"));
// 			Builder.Append(method, Builder.Block(responses
// 				.Select(s =>
// 				{
// 					var code = Int32.Parse(s.Key);
// 					var result = ((System.Net.HttpStatusCode) code).ToString();
// 					var type = Builder.ToTypeName(GetTypeName(s.Value.Schema));
// 					var padding = new String(' ', length - result.Length);
//
// 					var caseText = s.Value.Description.TrimEnd().Replace("\n", @"\n");
//
// 					if (s.Value.Schema is null)
// 					{
// 						return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException(\"{caseText}\", response),");
// 					}
// 					
// 					if (code is >= 200 and <= 299)
// 					{
// 						return Builder.Line($"HttpStatusCode.{result}{padding} => await response.Content.ReadFromJsonAsync<{type}>(token),");
// 					}
//
// 					return Builder.Line($"HttpStatusCode.{result}{padding} => throw new ApiException<{type}>(\"{caseText}\", response, await response.Content.ReadFromJsonAsync<{type}>(token)),");
// 				})
// 				.Append(Builder.Line($"_{new String(' ', length + "HttpStatusCode".Length)} => throw new InvalidOperationException(\"Unknown status code has been returned.\"),")), ";"));
// 		}
// 		else
// 		{
// 			var responseCodes = responses
// 				.Select(s =>
// 				{
// 					var code = Int32.Parse(s.Key);
// 					var result = ((System.Net.HttpStatusCode) code).ToString();
// 					var type = Builder.ToTypeName(GetTypeName(s.Value.Schema));
//
// 					var start = $"HttpStatusCode.{result}";
//
// 					if (s.Value.Schema is null)
// 					{
// 						return Builder.Case(start, Builder.Line($"throw new ApiException(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response);")) with
// 						{
// 							HasBreak = false,
// 						};
// 					}
//
// 					if (code is >= 200 and <= 299)
// 					{
// 						return Builder.Case(start, Builder.Line($"return await response.Content.ReadFromJsonAsync<{type}>(token);")) with
// 						{
// 							HasBreak = false,
// 						};
// 					}
//
// 					return Builder.Case(start, Builder.Line($"throw new ApiException<{type}>(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response, await response.Content.ReadFromJsonAsync<{type}>(token));")) with
// 					{
// 						HasBreak = false
// 					};
// 				});
//
// 			Builder.Append(method, Builder.Switch("response.StatusCode", responseCodes, Builder.Case(String.Empty, Builder.Line("throw new InvalidOperationException(\"Unknown status code has been returned.\");")) with { HasBreak = false }));
// 		}
// 	}
//
// 	private static ParameterBuilder ParseParameter(ParameterModel parameter)
// 	{
// 		var name = parameter.Name;
//
// 		var type = parameter.Type switch
// 		{
// 			ParameterTypes.Integer  => "int",
// 			ParameterTypes.Long     => "long",
// 			ParameterTypes.Float    => "float",
// 			ParameterTypes.Double   => "double",
// 			ParameterTypes.String   => "string",
// 			ParameterTypes.Byte     => "byte",
// 			ParameterTypes.Binary   => "ReadOnlySpan<byte>",
// 			ParameterTypes.Boolean  => "bool",
// 			ParameterTypes.Date     => "DateOnly",
// 			ParameterTypes.DateTime => "DateTime",
// 			ParameterTypes.Password => "string",
// 			ParameterTypes.File     => "Stream",
// 			_                       => "string",
// 		};
//
// 		if (parameter is { Type: ParameterTypes.String, Format: "uuid" })
// 		{
// 			type = "Guid";
// 		}
// 		else if (parameter is { Type: ParameterTypes.String, Format: "byte" })
// 		{
// 			type = "byte[]";
// 		}
//
// 		if (!parameter.Required)
// 		{
// 			return Builder.Parameter($"{type}?", name, "null", documentation: parameter.Description);
// 		}
//
// 		return Builder.Parameter(type, name, documentation: parameter.Description);
// 	}
//
// 	private static string ParseRequestPath(string path, List<ParameterModel> parameters)
// 	{
// 		var result = path;
//
// 		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Path))
// 		{
// 			result = result.Replace('{' + parameter.Name + '}', "{Uri.EscapeDataString(" + Builder.ToParameterName(parameter.Name) + ")}");
// 		}
//
// 		var isFirst = true;
//
// 		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Query && w.Required))
// 		{
// 			if (isFirst)
// 			{
// 				result += '?';
// 				isFirst = false;
// 			}
//
// 			var name = Builder.ToParameterName(parameter.Name);
//
// 			result += parameter.Name + "={Uri.EscapeDataString(" + name + ")}&";
// 		}
//
// 		return result;
// 	}
//
// 	private static void ParseHeaders(IEnumerable<ParameterModel> headers, MethodBuilder method)
// 	{
// 		foreach (var header in headers
// 			         .Where(w => w.In == ParameterLocation.Header)
// 			         .OrderByDescending(o => o.Required))
// 		{
// 			var name = Builder.ToParameterName(header.Name);
//
// 			if (header.Type == ParameterTypes.Binary || header is { Type: ParameterTypes.String, Format: "byte" })
// 			{
// 				name = $"Convert.ToBase64String({name})";
// 			}
// 			else if (header.Type == ParameterTypes.Boolean)
// 			{
// 				name = $"{name} ? \"true\" : \"false\"";
// 			}
// 			else if (header.Type != ParameterTypes.String || !String.IsNullOrEmpty(header.Format))
// 			{
// 				name = $"{name}.ToString()";
// 			}
//
// 			var headerText = $"request.Headers.Add(\"{header.Name}\", {name});";
//
// 			if (header.Name == "authorization")
// 			{
// 				headerText = $"request.Headers.Authorization = new AuthenticationHeaderValue(\"Basic\", {name});";
// 			}
//
// 			if (!header.Required)
// 			{
// 				Builder.Append(method, Builder.WhiteLine());
// 				Builder.Append(method, Builder.If($"{name} != null",
// 				[
// 					Builder.Line(headerText),
// 				]));
// 			}
// 			else
// 			{
// 				Builder.Append(method, Builder.Line(headerText));
// 			}
// 		}
// 	}
//
// 	private static void ParseQuery(IEnumerable<ParameterModel> query, MethodBuilder method)
// 	{
// 		query = query.Where(w => w.In == ParameterLocation.Query && !w.Required);
//
// 		foreach (var header in query)
// 		{
// 			var name = Builder.ToParameterName(header.Name);
// 			IContent block = method;
//
// 			if (!header.Required)
// 			{
// 				var ifBlock = Builder.If($"{name} != null");
// 				Builder.Append(method, ifBlock);
// 				block = ifBlock;
// 			}
//
// 			Builder.Append(block, Builder.Line($"urlBuilder.Append($\"{Uri.EscapeDataString(header.Name)}=" + "{Uri.EscapeDataString(" + name + ")}&\");"));
// 			Builder.Append(method, Builder.WhiteLine());
// 		}
// 	}
//
// 	private static MethodBuilder ParseForm(IEnumerable<ParameterModel> query)
// 	{
// 		var method = Builder.Method("GetFormContent") with
// 		{
// 			ReturnType = "IEnumerable<KeyValuePair<string, string>>",
// 			AccessModifier = AccessModifier.None,
// 		};
//
// 		var data = query
// 			.Where(w => w.In == ParameterLocation.FormData)
// 			.GroupBy(g => g.Required)
// 			.OrderByDescending(o => o.Key);
// 		
// 		foreach (var parameters in data)
// 		{
// 			if (parameters.Key)
// 			{
// 				foreach (var item in parameters)
// 				{
// 					Builder.Append(method, Builder.Line($"yield return KeyValuePair.Create(\"{item.Name}\", {Builder.ToParameterName(item.Name)});"));
//
// 				}
// 			}
// 			else
// 			{
// 				foreach (var item in parameters)
// 				{
// 					var parameterName = Builder.ToParameterName(item.Name);
// 					
// 					Builder.Append(method, Builder.WhiteLine());
// 					Builder.Append(method, Builder.If($"{parameterName} != null",
// 					[
// 						Builder.Line($"yield return KeyValuePair.Create(\"{item.Name}\", {parameterName});"),
// 					]));
// 				}
// 			}
// 		}
//
// 		return method;
// 	}
//
// 	private static string GetTypeName(SchemaModel? schema)
// 	{
// 		if (schema is null)
// 		{
// 			return String.Empty;
// 		}
//
// 		if (schema.Type == "array")
// 		{
// 			return $"{GetTypeName(schema.Items)}[]";
// 		}
//
// 		return (schema.Ref ?? schema.Type)
// 			.Split('/')
// 			.Last();
// 	}
// }