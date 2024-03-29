using OpenAPIGenerator.Builder;
using OpenAPIGenerator.Enumerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public static class OpenApiV2Parser
{
	public static string Parse(SwaggerModel model, string rootNamespace)
	{
		var typeName = Titleize(model.Info.Title);

		var defaultHeaders = model.SecurityDefinitions
			.Where(w => w.Value.In == ParameterLocation.Header);
		
		var builder = new CodeStringBuilder(1);

		foreach (var path in model.Paths)
		{
			ParsePath(path.Key, path.Value, builder);
		}

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
		var builder = new CodeStringBuilder()
			.AppendCode("using System;")
			.AppendCode("using System.Text.Json.Serialization;")
			.AppendCode("")
			.AppendCode($"namespace {defaultNamespace}.Models;")
			.AppendCode("")
			.AppendCode("/// <summary>")
			.AppendCode($"/// {schema.Description?.Trim()?.Replace("\n", "\n/// ")}")
			.AppendCode("/// </summary>");

		if (schema.Enum?.Any() ?? false)
		{
			builder.AppendBlock($"public enum {name}")
				.AppendCode(schema.Enum.Select(s => $"{Titleize(s.ToString())},"));
		}
		else
		{
			var block = builder.AppendBlock($"public sealed class {Titleize(name)}");

			foreach (var property in schema.Properties ?? [])
			{
				var type = GetTypeName(property.Value);

				type = Titleize(type.TrimStart('_'));

				if (String.IsNullOrWhiteSpace(property.Value.Description))
				{
					block
						.AppendCode($"[JsonPropertyName(\"{property.Key}\")]")
						.AppendCode($"public {type} {Titleize(property.Key)} {{ get; set; }}");
				}
				else
				{
					block
						.AppendCode($"/// <summary> {property.Value.Description.Replace("\n", "\n\t/// ")} </summary>")
						.AppendCode($"[JsonPropertyName(\"{property.Key}\")]")
						.AppendCode($"public {Titleize(type)} {Titleize(property.Key)} {{ get; set; }}");
				}
			}
		}

		return builder.ToString();
	}

	private static void ParsePath(string requestPath, PathModel path, CodeStringBuilder builder)
	{
		if (path.Get is not null)
		{
			ParseOperation(requestPath, path.Get, "Get", builder);
		}

		if (path.Post is not null)
		{
			ParseOperation(requestPath, path.Post, "Post", builder);
		}

		if (path.Put is not null)
		{
			ParseOperation(requestPath, path.Put, "Put", builder);
		}

		if (path.Delete is not null)
		{
			ParseOperation(requestPath, path.Delete, "Delete", builder);
		}

		if (path.Options is not null)
		{
			ParseOperation(requestPath, path.Options, "Options", builder);
		}

		if (path.Head is not null)
		{
			ParseOperation(requestPath, path.Head, "Head", builder);
		}

		if (path.Patch is not null)
		{
			ParseOperation(requestPath, path.Patch, "Patch", builder);
		}
	}
	private static void ParseOperation(string requestPath, OperationModel path, string operationName, CodeStringBuilder builder)
	{
		var resultType = Titleize(path.Responses
			.Where(w => Int32.TryParse(w.Key, out var code) && code is >= 200 and <= 299)
			.Select(s => GetTypeName(s.Value.Schema))
			.First());

		var getRequestPath = ParseRequestPath(requestPath, path.Parameters);

		var parameters = String.Concat(path.Parameters
			.OrderByDescending(o => o.Required)
			.Select(ParseParameter));
		

		builder
			.AppendCode("")
			.AppendCode("/// <summary>")
			.AppendCode(CodeStringBuilder
				.GetLines(path.Summary)
				.Select(s => $"/// {s}"));

		foreach (var parameter in path.Parameters)
		{
			builder.AppendCode($"/// <param name=\"{parameter.Name.Replace('-', '_')}\">{parameter.Description.Replace("\n", "\n/// ")}</param>");
		}
		
		var returnType = String.IsNullOrWhiteSpace(resultType)
			? "Task"
			: $"Task<{resultType}?>";

		var block = builder.AppendBlock($"public async {returnType} {Titleize(path.OperationId)}Async({parameters}CancellationToken token = default)");

		if (path.Parameters
		    .Any(w => w.In == ParameterLocation.Header))
		{
			block.AppendCode($"using var request = new HttpRequestMessage(HttpMethod.{operationName}, $\"{getRequestPath}\");");
				
			ParseHeaders(path.Parameters, block);

			block.AppendCode("using var response = await _client.SendAsync(request, token);");
				
		}
		else
		{
			block.AppendCode($"using var response = await _client.{operationName}Async($\"{getRequestPath}\", token);");
		}

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
			.Select(s => ((System.Net.HttpStatusCode)Int32.Parse(s.Key)).ToString().Length)
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

	private static string ParseParameter(ParameterModel parameter)
	{
		var name = parameter.Name;

		if (String.IsNullOrWhiteSpace(name))
		{
			return String.Empty;
		}

		var type = parameter.Type switch
		{
			ParameterTypes.Integer => "int",
			ParameterTypes.Long => "long",
			ParameterTypes.Float => "float",
			ParameterTypes.Double => "double",
			ParameterTypes.String => "string",
			ParameterTypes.Byte => "byte",
			ParameterTypes.Binary => "ReadOnlySpan<byte>",
			ParameterTypes.Boolean => "bool",
			ParameterTypes.Date => "DateOnly",
			ParameterTypes.DateTime => "DateTime",
			ParameterTypes.Password => "string",
			ParameterTypes.File => "Stream",
			_ => "string",
		};

		if (parameter is { Type: ParameterTypes.String, Format: "uuid" })
		{
			type = "Guid";
		}

		if (!parameter.Required)
		{
			return $"{type}? {name.Replace('-', '_')} = null, ";
		}

		return $"{type} {name.Replace('-', '_')}, ";
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

	private static void ParseHeaders(IEnumerable<ParameterModel> headers, Block builder)
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
				builder
					.AppendBlock($"if ({name} != null)")
					.AppendCode($"request.Headers.Add(\"{header.Name}\", {name});");
			}
			else
			{
				builder.AppendCode($"request.Headers.Add(\"{header.Name}\", {name});");
			}
		}

		builder.AppendCode("");
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