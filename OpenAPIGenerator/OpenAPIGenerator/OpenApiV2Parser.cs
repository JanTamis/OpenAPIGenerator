using OpenAPIGenerator.Enumerators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenAPIGenerator.Builders;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public static class OpenApiV2Parser
{
	public static string Parse(SwaggerModel model, string rootNamespace)
	{
		var defaultHeaders = model.SecurityDefinitions
			.Where(w => w.Value.In == ParameterLocation.Header);

		var constructorContent = new List<IBuilder>
		{
			Builder.Line($"BaseAddress = new Uri(\"{model.Schemes[0]}://{model.Host}{model.BasePath}\"),")
		};

		if (defaultHeaders.Any())
		{
			constructorContent.Add(Builder.Line("DefaultRequestHeaders ="));
			constructorContent.Add(Builder.Block(defaultHeaders
				.Select(s => Builder.Line($"{{ \"{s.Key}\", {s.Key} }},"))));
		}
		
		var type = new TypeBuilder(model.Info.Title)
		{
			Usings =
			[
				"System.Net",
				"System.Net.Http",
				"System.Net.Http.Json",
				"System",
				"System.Threading",
				"System.Threading.Tasks",
				$"{rootNamespace}.Models",
			],
			Namespace = rootNamespace,
			Summary = model.Info.Description,
			Properties = [Builder.Property("HttpClient", "Client", modifier: PropertyModifier.Get)],
			Constructors =
			[
				new ConstructorBuilder
				{
					Parameters = defaultHeaders
						.Select(s => new ParameterBuilder("string", s.Key)),
					Content =
					[
						Builder.Line("Client = new HttpClient()"),
						Builder.Block(constructorContent, ";"),
					],
				}
			]
		};

		foreach (var path in model.Paths)
		{
			ParsePath(path.Key, path.Value, type);
		}

		return Builder.ToString(type);
	}

	public static string ParseObject(string name, SchemaModel schema, string defaultNamespace)
	{
		BaseTypeBuilder type;

		if (schema.Enum?.Any() ?? false)
		{
			type = new EnumBuilder
			{
				TypeName = Builder.ToTypeName(name),
				Members = schema.Enum?.Select(s => new EnumMemberBuilder(s.ToString())) ?? []
			};
		}
		else
		{
			type = new TypeBuilder(Builder.ToTypeName(name))
			{
				Properties = schema.Properties?
					.Select(s => Builder.Property(GetTypeName(s.Value), s.Key, s.Value.Description, Builder.Attribute("JsonPropertyName", $"\"{s.Key}\""))) ?? [],
			};
		}

		type.Namespace = $"{defaultNamespace}.Models";
		type.Summary = schema.Description;
		type.Usings =
		[
			"System",
			"System.Text.Json.Serialization",
		];

		return Builder.ToString(type);
	}

	private static void ParsePath(string requestPath, PathModel path, TypeBuilder type)
	{
		foreach (var item in path.GetOperations())
		{
			if (item.Value is not null)
			{
				ParseOperation(requestPath, item.Value, item.Key, type);
			}
		}
	}

	private static void ParseOperation(string requestPath, OperationModel path, string operationName, TypeBuilder type)
	{
		var resultType = Builder.ToTypeName(path.Responses
			.Where(w => Int32.TryParse(w.Key, out var code) && code is >= 200 and <= 299)
			.Select(s => $"{GetTypeName(s.Value.Schema)}?")
			.First());

		var getRequestPath = ParseRequestPath(requestPath, path.Parameters);

		var parameters = path.Parameters
			.Where(w => !String.IsNullOrWhiteSpace(w.Name))
			.OrderByDescending(o => o.Required)
			.Select(ParseParameter)
			.Append(Builder.Parameter("CancellationToken", "token", "default"));

		var method = Builder.Method($"{Builder.ToTypeName(path.OperationId)}Async", $"{resultType}?", true, AccessModifier.Public, parameters, [], path.Summary);
		var hasQuery = path.Parameters.Any(a => a.In == ParameterLocation.Query && !a.Required);
		var hasPath = path.Parameters.Any(a => a.In == ParameterLocation.Path);

		if (hasQuery)
		{
			Builder.Append(method, Builder.Line($"DefaultInterpolatedStringHandler url = $\"{getRequestPath}\";"));
			Builder.Append(method, Builder.WhiteLine());
			ParseQuery(path.Parameters, method);
		}

		if (path.Parameters
		    .Any(w => w.In == ParameterLocation.Header))
		{
			if (hasQuery)
			{
				Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, url.ToStringAndClear());"));
			}
			else
			{
				if (hasPath)
				{
					Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, $\"{getRequestPath}\");"));
				}
				else
				{
					Builder.Append(method, Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, \"{getRequestPath}\");"));
				}
			}

			Builder.Append(method, Builder.WhiteLine());

			ParseHeaders(path.Parameters, method);

			Builder.Append(method, Builder.WhiteLine());
			Builder.Append(method, Builder.Line("using var response = await Client.SendAsync(request, token);"));
		}
		else
		{
			if (hasQuery)
			{
				Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async(url.ToStringAndClear(), token);"));
			}
			else
			{
				if (hasPath)
				{
					Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async($\"{getRequestPath}\", token);"));
				}
				else
				{
					Builder.Append(method, Builder.Line($"using var response = await Client.{operationName}Async(\"{getRequestPath}\", token);"));
				}
			}
		}

		Builder.Append(method, Builder.WhiteLine());

		ParseResponse(path.Responses, method, !String.IsNullOrWhiteSpace(resultType));

		type.Methods = type.Methods.Append(method);
	}

	private static void ParseResponse(IReadOnlyDictionary<string, ResponseModel> responses, MethodBuilder method, bool hasReturnType)
	{
		var responseCodes = responses
			.Select(s =>
			{
				var code = Int32.Parse(s.Key);
				var result = ((System.Net.HttpStatusCode) code).ToString();
				var type = Builder.ToTypeName(GetTypeName(s.Value.Schema));

				var start = $"HttpStatusCode.{result}";

				if (s.Value.Schema is null)
				{
					return Builder.Case(start, Builder.Line($"throw new ApiException(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response);"));
				}

				if (code is >= 200 and <= 299)
				{
					return Builder.Case(start, Builder.Line($"return await response.Content.ReadFromJsonAsync<{type}>(token);"));
				}

				return Builder.Case(start, Builder.Line($"throw new ApiException<{type}>(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response, await response.Content.ReadFromJsonAsync<{type}>(token));"));
			});

		Builder.Append(method, Builder.Switch("response.StatusCode", responseCodes, Builder.Line("throw new InvalidOperationException(\"Unknown status code has been returned.\");")));
	}

	private static ParameterBuilder ParseParameter(ParameterModel parameter)
	{
		var name = parameter.Name;

		var type = parameter.Type switch
		{
			ParameterTypes.Integer  => "int",
			ParameterTypes.Long     => "long",
			ParameterTypes.Float    => "float",
			ParameterTypes.Double   => "double",
			ParameterTypes.String   => "string",
			ParameterTypes.Byte     => "byte",
			ParameterTypes.Binary   => "ReadOnlySpan<byte>",
			ParameterTypes.Boolean  => "bool",
			ParameterTypes.Date     => "DateOnly",
			ParameterTypes.DateTime => "DateTime",
			ParameterTypes.Password => "string",
			ParameterTypes.File     => "Stream",
			_                       => "string",
		};

		if (parameter is { Type: ParameterTypes.String, Format: "uuid" })
		{
			type = "Guid";
		}

		if (!parameter.Required)
		{
			return Builder.Parameter($"{type}?", name, "null", documentation: parameter.Description);
		}

		return Builder.Parameter(type, name, documentation: parameter.Description);
	}

	private static string ParseRequestPath(string path, List<ParameterModel> parameters)
	{
		var result = path;

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Path))
		{
			result = result.Replace('{' + parameter.Name + '}', '{' + Builder.ToParameterName(parameter.Name) + '}');
		}

		var isFirst = true;

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Query && w.Required))
		{
			var name = Builder.ToParameterName(parameter.Name);

			result += isFirst
				? '?'
				: '&';

			result += parameter.Name + "={" + name + '}';

			isFirst = false;
		}


		return result;
	}

	private static void ParseHeaders(IEnumerable<ParameterModel> headers, MethodBuilder method)
	{
		foreach (var header in headers
			         .Where(w => w.In == ParameterLocation.Header)
			         .OrderByDescending(o => o.Required))
		{
			var name = Builder.ToParameterName(header.Name);

			if (header.Type != ParameterTypes.String || !String.IsNullOrEmpty(header.Format))
			{
				name = $"{name}.ToString()";
			}
			else if (header.Type == ParameterTypes.Binary)
			{
				name = $"Convert.ToBase64String({name});";
			}

			if (!header.Required)
			{
				Builder.Append(method, Builder.WhiteLine());
				Builder.Append(method, Builder.If($"{name} != null",
				[
					Builder.Line($"request.Headers.Add(\"{header.Name}\", {name});")
				]));
			}
			else
			{
				Builder.Append(method, Builder.Line($"request.Headers.Add(\"{header.Name}\", {name});"));
			}
		}
	}

	private static void ParseQuery(IEnumerable<ParameterModel> query, MethodBuilder method)
	{
		var isFirst = false;

		foreach (var header in query
			         .Where(w => w.In == ParameterLocation.Query && !w.Required))
		{
			var name = Builder.ToParameterName(header.Name);
			IContent block = method;

			if (!header.Required)
			{
				var ifBlock = Builder.If($"{name} != null");
				Builder.Append(method, ifBlock);
				block = ifBlock;
			}

			if (isFirst)
			{
				Builder.Append(block, Builder.Line($"url.AppendLiteral(\"{header.Name}=\");"));
			}
			else
			{
				Builder.Append(block, Builder.Line($"url.AppendLiteral(\"&{header.Name}=\");"));
			}

			Builder.Append(block, Builder.Line($"url.AppendFormatted({name});"));

			isFirst = false;
			Builder.Append(method, Builder.WhiteLine());
		}
	}

	private static string GetTypeName(SchemaModel? schema)
	{
		if (schema is null)
		{
			return String.Empty;
		}

		if (schema.Type == "array")
		{
			return $"{GetTypeName(schema.Items)}[]";
		}

		return (schema.Ref ?? schema.Type)
			.Split('/')
			.Last();
	}
}