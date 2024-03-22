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
				}
						
				{{String.Join("\n\n", model.Paths
					.SelectMany(i => ParsePath(i.Key, i.Value, model))).Replace("\n", "\n\t")}}
					
				public void Dispose()
				{
					_client.Dispose();
				}
			}
			""";
	}

	public static string ParseObject(string name, SchemaModel schema, string defaultNamespace)
	{
		var builder = new CodeStringBuilder();

		builder
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


		}

		var properties = schema.Properties?.Select(s =>
		{
			var type = s.Value.Type ?? s.Value.Ref.Split('/').Last();

			if (s.Value.Type == "array")
			{
				type = s.Value.Items.Type ?? s.Value.Items.Ref.Split('/').Last() + "[]";
			}

			type = Titleize(type.TrimStart('_'));

			if (String.IsNullOrWhiteSpace(s.Value.Description))
			{
				return $$"""
					[JsonPropertyName("{{s.Key}}")]
					public {{Titleize(type)}} {{Titleize(s.Key)}} { get; set; }
					""";
			}

			return $$"""
				/// <summary> {{s.Value.Description.Replace("\n", "\n\t/// ")}} </summary>
				[JsonPropertyName("{{s.Key}}")]
				public {{Titleize(type)}} {{Titleize(s.Key)}} { get; set; }
				""";
		});

		return builder.ToString();

		return $$"""
			using System;
			using System.Text.Json.Serialization;

			namespace {{defaultNamespace}}.Models;

			/// <summary> 
			/// {{schema.Description?.Trim()?.Replace("\n", "\n/// ")}} 
			/// </summary>
			public sealed class {{Titleize(name)}}
			{
				{{String.Join("\n\n\t", (properties ?? []).Select(s => s.Replace("\n", "\n\t")))}}
			}
			""";
	}

	private static IEnumerable<string> ParsePath(string requestPath, PathModel path, SwaggerModel root)
	{
		if (path.Get is not null)
		{
			var resultType = Titleize(path.Get.Responses
				.Where(w => Int32.TryParse(w.Key, out var code) && code is >= 200 and <= 299)
				.Select(s => (s.Value.Schema.Ref ?? s.Value.Schema.Type).Split('/').Last())
				.First());

			var getRequestPath = ParseRequestPath(requestPath, path.Get.Parameters);

			var parametersComments = path.Get.Parameters
				.Select(s => $"/// <param name=\"{s.Name.Replace('-', '_')}\">{s.Description.Replace("\n", "\n/// ")}</param>");

			var parameters = String.Concat(path.Get.Parameters
				.OrderByDescending(o => o.Required)
				.Select(ParseParameter));

			var headers = path.Get.Parameters
				.Where(w => w.In == ParameterLocation.Header)
				.OrderByDescending(o => o.Required)
				.Select(s =>
				{
					var name = s.Name.Replace('-', '_');

					if (s.Type != ParameterTypes.String || !String.IsNullOrEmpty(s.Format))
					{
						name = $"{name}.ToString()";
					}
					else if (s.Type == ParameterTypes.Binary)
					{
						name = $"Convert.ToBase64String({name});";
					}

					if (!s.Required)
					{
						return $$"""
							if ({{name}} != null)
								{
									request.Headers.Add("{{s.Name}}", {{name}});
								}
							""";
					}

					return $"request.Headers.Add(\"{s.Name}\", {name});";
				});

			if (headers.Any())
			{
				yield return $$"""
					/// <summary>
					/// {{path.Get.Summary.Replace("\n", "\n/// ")}}
					/// </summary>
					{{String.Join("\n", parametersComments)}}
					public async Task<{{resultType}}?> {{Titleize(path.Get.OperationId)}}Async({{parameters}}CancellationToken token = default)
					{
						using var request = new HttpRequestMessage(HttpMethod.Get, $"{{getRequestPath}}");
						
						{{String.Join("\n\t", headers)}}
						
						using var response = await _client.SendAsync(request, token);
						
						{{ParseResponse(path.Get.Responses).Replace("\n", "\n\t")}}
					}
					""";
			}
			else
			{
				yield return $$"""
					/// <summary>
					/// {{path.Get.Summary.Replace("\n", "\n/// ")}}
					/// </summary>
					{{String.Join("\n", parametersComments)}}
					public async Task<{{resultType}}?> {{Titleize(path.Get.OperationId)}}Async({{parameters}}CancellationToken token = default)
					{
						using var response = await _client.GetAsync($"{{getRequestPath}}", token);
						
						{{ParseResponse(path.Get.Responses).Replace("\n", "\n\t")}}
					}
					""";
			}
		}
	}

	private static string ParseResponse(IReadOnlyDictionary<string, ResponseModel> responses)
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
				var result = String.Empty;

				if (s.Value.Schema is null && Int32.TryParse(s.Key, out int code))
				{
					result = ((System.Net.HttpStatusCode)code).ToString();

					return $$"""
						HttpStatusCode.{{result}} {{new string(' ', maxLength - result.Length)}}=> throw new ApiException("{{s.Value.Description.TrimEnd().Replace("\n", @"\n")}}", response),
						""";
				}

				var type = Titleize((s.Value.Schema.Ref ?? s.Value.Schema.Type).Split('/').Last());

				if (Int32.TryParse(s.Key, out code) && code is >= 200 and <= 299)
				{
					result = ((System.Net.HttpStatusCode)code).ToString();

					return $$"""
						HttpStatusCode.{{result}} {{new string(' ', maxLength - result.Length)}}=> await response.Content.ReadFromJsonAsync<{{type}}>(token),
						""";
				}

				result = ((System.Net.HttpStatusCode)code).ToString();

				return $$"""
					HttpStatusCode.{{result}} {{new string(' ', maxLength - result.Length)}}=> throw new ApiException<{{type}}>("{{s.Value.Description.TrimEnd().Replace("\n", @"\n")}}", response, await response.Content.ReadFromJsonAsync<{{type}}>(token)),
					""";
			})
			.Concat(defaultEnumerable);

		var builder = new CodeStringBuilder();
		var block = builder.AppendBlock("return response.StatusCode switch", close: "};");

		foreach (var response in responseCodes)
		{
			block.AddCode(response);
		}

		return builder.ToString();
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

		if (parameter.Type == ParameterTypes.String && parameter.Format == "uuid")
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

		return Char.ToUpper(source[0]) + source.Substring(1);
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
}