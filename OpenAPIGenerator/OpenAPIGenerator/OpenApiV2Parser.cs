using OpenAPIGenerator.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenAPIGenerator.Builders;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public static class OpenApiV2Parser
{
	public static string Parse(SwaggerModel model, string rootNamespace)
	{
		var typeName = Titleize(model.Info.Title);

		var defaultHeaders = model.SecurityDefinitions
			.Where(w => w.Value.In == ParameterLocation.Header);

		var builder = new CodeStringBuilder(1);

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
						Builder.Block(
							Builder.Line($"BaseAddress = new Uri(\"{model.Schemes[0]}://{model.Host}{model.BasePath}\"),"),
							Builder.Line("DefaultRequestHeaders ="),
							Builder.Block(defaultHeaders
								.Select(s => Builder.Line($"{{\"{s.Key}\", {s.Key}}}")))),
					]
				}
			]
		};

		foreach (var path in model.Paths)
		{
			ParsePath(path.Key, path.Value, builder);
		}

		return Builder.ToString(type);

		return $$"""
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Json;
			using System;
			using System.Threading;
			using System.Threading.Tasks;
			using {{rootNamespace}}.Models;

			namespace {{rootNamespace}};

			/// <summary>
			/// {{model.Info.Description.Trim().Replace("\n", "\n/// ")}}
			/// </summary>
			public class {{typeName}} : IDisposable
			{
				private readonly HttpClient _client;
				
				public {{typeName}}({{String.Join(", ", defaultHeaders.Select(s => $"string {s.Key}"))}})
				{
					_client = new HttpClient()
					{
						BaseAddress = new Uri("{{model.Schemes[0]}}://{{model.Host}}{{model.BasePath}}"),
						DefaultRequestHeaders =
						{
							{{String.Join("\n\t\t\t\t", defaultHeaders.Select(s => $$"""{ "{{s.Key}}", {{s.Key}} }"""))}}
																								}
																							};
																						}{{builder}}
																							
																						public void Dispose()
																						{
																							_client.Dispose();
																						}
																					}
			""";
	}

	public static string ParseObject(string name, SchemaModel schema, string defaultNamespace)
	{
		BaseTypeBuilder type;

		if (schema.Enum?.Any() ?? false)
		{
			type = new EnumBuilder()
			{
				TypeName = Titleize(name),
				Members = schema.Enum?.Select(s => new EnumMemberBuilder(s.ToString())) ?? []
			};
		}
		else
		{
			type = new TypeBuilder(Titleize(name))
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
		var resultType = Titleize(path.Responses
			.Where(w => Int32.TryParse(w.Key, out var code) && code is >= 200 and <= 299)
			.Select(s => GetTypeName(s.Value.Schema))
			.First());

		if (!String.IsNullOrWhiteSpace(resultType))
		{
			resultType = "void";
		}

		var getRequestPath = ParseRequestPath(requestPath, path.Parameters);

		var parameters = path.Parameters
			.Where(w => !String.IsNullOrWhiteSpace(w.Name))
			.OrderByDescending(o => o.Required)
			.Select(ParseParameter)
			.Append(Builder.Parameter("CancellationToken", "token", "default"));

		
		var method = Builder.Method($"{Titleize(path.OperationId)}Async", resultType, true, AccessModifier.Public, parameters, [], path.Summary);

		if (path.Parameters
		    .Any(w => w.In == ParameterLocation.Header))
		{
			method.Content = method.Content
				.Append(Builder.Line($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, $\"{getRequestPath}\");"))
				.Append(Builder.WhiteLine());

			ParseHeaders(path.Parameters, method);
			
			method.Content = method.Content
				.Append(Builder.WhiteLine())
				.Append(Builder.Line("using var response = await _client.SendAsync(request, token);"));
		}
		else
		{
			method.Content = method.Content
				.Append(Builder.Line($"using var response = await _client.{operationName}Async($\"{getRequestPath}\", token);"));
		}

		method.Content = method.Content
			.Append(Builder.WhiteLine());

		ParseResponse(path.Responses, block, !String.IsNullOrWhiteSpace(resultType));
	}

	private static void ParseResponse(IReadOnlyDictionary<string, ResponseModel> responses, Block builder, bool hasReturnType)
	{
		var defaultEnumerable = Enumerable.Empty<string>();

		if (!responses.ContainsKey("default"))
		{
			defaultEnumerable = defaultEnumerable.Append("""
				_ => throw new InvalidOperationException("Unknown status code has been returned."),
				""");
		}

		var maxLength = responses
			.Select(s => ((System.Net.HttpStatusCode) Int32.Parse(s.Key)).ToString().Length)
			.Max();

		var responseCodes = responses
			.Select(s =>
			{
				var code = Int32.Parse(s.Key);
				var result = ((System.Net.HttpStatusCode) code).ToString();
				var type = Titleize(GetTypeName(s.Value.Schema));

				var start = $"HttpStatusCode.{result} {new string(' ', maxLength - result.Length)}=>";

				if (s.Value.Schema is null)
				{
					return $"{start} throw new ApiException(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response),";
				}

				if (code is >= 200 and <= 299)
				{
					return $"{start} await response.Content.ReadFromJsonAsync<{type}>(token),";
				}

				return $"{start} throw new ApiException<{type}>(\"{s.Value.Description.TrimEnd().Replace("\n", @"\n")}\", response, await response.Content.ReadFromJsonAsync<{type}>(token)),";
			})
			.Concat(defaultEnumerable);

		var block = hasReturnType
			? builder.AppendBlock("return response.StatusCode switch", close: "};")
			: builder.AppendBlock("_ = response.StatusCode switch", close: "};");

		foreach (var response in responseCodes)
		{
			block.AppendCode(response);
		}
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

	public static string? Titleize(string? source)
	{
		if (String.IsNullOrEmpty(source))
		{
			return source;
		}

		source = String.Join(String.Empty, source
			.Split(' ', '-')
			.Select(i => Char.ToUpper(i[0]) + i.Substring(1)));

		return Char.ToUpper(source[0]) + source.Substring(1).Replace('.', '_');
	}

	private static string ParseRequestPath(string path, List<ParameterModel> parameters)
	{
		var result = path;

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Path))
		{
			result = result.Replace('{' + parameter.Name + '}', '{' + parameter.Name.Replace('-', '_')) + '}';
		}

		return result;
	}

	private static void ParseHeaders(IEnumerable<ParameterModel> headers, MethodBuilder method)
	{
		foreach (var header in headers
			         .Where(w => w.In == ParameterLocation.Header)
			         .OrderByDescending(o => o.Required))
		{
			var name = header.Name.Replace('-', '_');

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
				method.Content = method.Content
					.Append(Builder.If($"{name} != null",
						Builder.Line($"request.Headers.Add(\"{header.Name}\", {name});")));
			}
			else
			{
				method.Content = method.Content
					.Append(Builder.Line($"request.Headers.Add(\"{header.Name}\", {name});"));
			}
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