using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public static class OpenApiV2Parser
{
	public static string Parse(SwaggerModel model, string rootNamespace)
	{
		var typeName = Titleize(model.Info.Title);

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
				
				public {{typeName}}()
				{
					_client = new HttpClient()
					{
						BaseAddress = new Uri("{{model.Schemes[0]}}://{{model.Host}}{{model.BasePath}}"),
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


		var properties = schema.Properties?.SelectMany<KeyValuePair<string, SchemaModel>, string>(s =>
		{
			var type = s.Value.Type ?? s.Value.Ref.Split('/').Last();
			
			if (s.Value.Type == "array")
			{
				type = s.Value.Items.Type ?? s.Value.Items.Ref.Split('/').Last() + "[]";
			}

			if (String.IsNullOrWhiteSpace(s.Value.Description))
			{
				return
				[
					$"public {Titleize(type)} {Titleize(s.Key)} {{ get; set; }}\n\n"
				];
			}

			return
			[
				$"/// <summary> {s.Value.Description.Replace("\n", "\n\t/// ")} </summary>\n",
				$"public {Titleize(type)} {Titleize(s.Key)} {{ get; set; }}\n\n"
			];
		});
		
		return $$"""
			using System;
			
			namespace {{defaultNamespace}}.Models;
			
			public sealed class {{name}}
			{
				{{String.Join("\t", properties ?? [])}}
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

			yield return $$"""
				/// <summary>
				/// {{path.Get.Summary.Replace("\n", "\n/// ")}}
				/// </summary>
				{{String.Join("\n", parameters)}}
				public async Task<{{resultType}}> {{Titleize(path.Get.OperationId)}}Async({{String.Concat((path.Get.Parameters).Select(ParseParameter))}}CancellationToken token = default)
				{
					using var request = new HttpRequestMessage(HttpMethod.Get, $"{{getRequestPath}}");
					
					{{String.Join("\n\t", path.Get.Parameters.Where(w => w.In == ParameterLocation.Header).Select(s => $"request.Headers.Add(\"{s.Name}\", {s.Name.Replace('-', '_')});"))}}
					
					using var response = await _client.SendAsync(request, token);
					
					{{ParseResponse(path.Get.Responses).Replace("\n", "\n\t")}}
				}
				""";
		}
	}

	private static string ParseResponse(IReadOnlyDictionary<string, ResponseModel> responses)
	{
		var defaultEnumerable = Enumerable.Empty<string>();

		if (!responses.ContainsKey("default"))
		{
			defaultEnumerable = defaultEnumerable.Append("""
				default:
					{
						return default;
					}
				""");
		}

		return $$"""
			switch (response.StatusCode)
			{
				{{String.Join("\n\t", responses
					.Where(w => w.Value.Schema is not null)
					.Select(s => (s.Key, (s.Value)))
					.Select(s =>
					{
						var type = Titleize((s.Item2.Schema.Ref ?? s.Item2.Schema.Type).Split('/').Last());

						if (Int32.TryParse(s.Key, out var code) && code is >= 200 and <= 299)
						{
							return $$"""
								case HttpStatusCode.{{(System.Net.HttpStatusCode) code}}:
									{
										return await response.Content.ReadFromJsonAsync<{{type}}>(token).ConfigureAwait(false);
									}
								""";
						}

						return $$"""
							{{ParseCode(s.Item1)}}
								{
									var result = await response.Content.ReadFromJsonAsync<{{type}}>(token).ConfigureAwait(false);
									
									throw new ApiException<{{type}}>("{{s.Item2.Description.Replace("\n", @"\n")}}", response, result);
								}
							""";
					})
					.Concat(defaultEnumerable))
				}}
			}
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
			return "default:";
		}

		if (Int32.TryParse(code, out var result))
		{
			return $"case HttpStatusCode.{(System.Net.HttpStatusCode) result}:";
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