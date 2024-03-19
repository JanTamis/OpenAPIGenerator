using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
		if (schema.Enum?.Any() ?? false)
		{
			return $$"""
				using System;

				namespace {{defaultNamespace}}.Models;

				public enum {{name}}
				{
					{{String.Join("\n\t", schema.Enum.Select(s => $"{Titleize(s.ToString())},"))}}
				}
				""";
		}


		var properties = schema.Properties?.Select<KeyValuePair<string, SchemaModel>, string>(s =>
		{
			var type = s.Value.Type ?? s.Value.Ref.Split('/').Last();

			if (s.Value.Type == "array")
			{
				type = s.Value.Items.Type ?? s.Value.Items.Ref.Split('/').Last() + "[]";
			}

			if (String.IsNullOrWhiteSpace(s.Value.Description))
			{
				return $"public {Titleize(type)} {Titleize(s.Key)} {{ get; set; }}";
			}

			return $$"""
			/// <summary> {{s.Value.Description.Replace("\n", "\n\t/// ")}} </summary>
			public {{Titleize(type)}} {{Titleize(s.Key)}} { get; set; }
			""";
		});

		return $$"""
			using System;
			
			namespace {{defaultNamespace}}.Models;
			
			public sealed class {{name}}
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

			var parameters = path.Get.Parameters
				.Select(s => $"/// <param name=\"{s.Name.Replace('-', '_')}\">{s.Description.Replace("\n", "\n/// ")}</param>");

			var headers = path.Get.Parameters
				.Where(w => w.In == ParameterLocation.Header)
				.Select(s =>
				{
					var name = s.Name.Replace('-', '_');

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
				{{String.Join("\n", parameters)}}
				public async Task<{{resultType}}?> {{Titleize(path.Get.OperationId)}}Async({{String.Concat((path.Get.Parameters).Select(ParseParameter))}}CancellationToken token = default)
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
				{{String.Join("\n", parameters)}}
				public async Task<{{resultType}}?> {{Titleize(path.Get.OperationId)}}Async({{String.Concat((path.Get.Parameters).Select(ParseParameter))}}CancellationToken token = default)
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

		return $$"""
			return response.StatusCode switch
			{
				{{String.Join("\n\t", responses
					.Select(s =>
					{
						var code = 0;
						var result = String.Empty;

						if (s.Value.Schema is null && Int32.TryParse(s.Key, out code))
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
					.Concat(defaultEnumerable))}}
			};
			""";
	}

	private static string ParseParameter(ParameterModel parameter)
	{
		var name = parameter.Name;
		var type = parameter.Type; //  ?? parameter.Ref.Split('/').Last();

		if (String.IsNullOrWhiteSpace(name))
		{
			return String.Empty;
		}

		return $"{type} {name.Replace('-', '_')}, ";
	}

	private static string ParseCode(string code)
	{
		if (String.Equals("default", code, StringComparison.OrdinalIgnoreCase))
		{
			return "_ =>";
		}

		if (Int32.TryParse(code, out var result))
		{
			return $"HttpStatusCode.{(System.Net.HttpStatusCode)result} =>";
		}

		var start = String.Empty;
		var end = String.Empty;

		for (var i = 0; i < code.Length; i++)
		{
			if (Char.IsNumber(code[i]))
			{
				start += code[i];
				end += code[i];
			}
			else
			{
				start += '0';
				end += '9';
			}
		}

		return $"case >= {start} and <= {end}:";
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