using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public static class OpenApiV3Parser
{
	public static string Parse(OpenApiModel model, string rootNamespace)
	{
		var typeName = Titleize(model.Info.Title);

		return $$"""
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Json;
			using System;
			using System.Threading;
			using System.Threading.Tasks;

			namespace {{rootNamespace}};

			/// <summary>
			/// 
			/// </summary>
			public class {{typeName}} : IDisposable
			{
				private readonly HttpClient _client;
				
				public {{typeName}}()
				{
					_client = new HttpClient()
					{
						BaseAddress = new Uri("{{model.Servers.First().Url}}"),
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

	private static IEnumerable<string> ParsePath(string requestPath, PathModel path, OpenApiModel root)
	{
		if (path.Get is not null)
		{
			yield return $$"""
				/// <summary>
				/// {{path.Get.Summary.Replace("\n", "\n/// ")}}
				/// </summary>
				public async Task<object> {{Titleize(path.Get.OperationId)}}Async({{String.Concat((path.Get.Parameters ?? Enumerable.Empty<ParameterModel>()).Select(ParseParameter))}}CancellationToken token = default)
				{
					using var response = await _client.GetAsync($"{{requestPath}}", token);
					
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
			switch ((int)response.StatusCode)
			{
				{{String.Join("\n\t", responses
						.Select(s => (s.Key, (s.Value.Ref ?? s.Value.Content.Values.First().Schema.Type ?? s.Value.Content.Values.First().Schema.Ref).Split('/').Last()))
						.Select(s =>
							$$"""
							{{ParseCode(s.Item1)}}
								{
									return await response.Content.ReadFromJsonAsync<{{s.Item2}}>(token);
								}
							""")
					.Concat(defaultEnumerable))
				}}
			}
			""";
	}

	private static string ParseParameter(ParameterModel parameter)
	{
		var name = parameter.Name;
		var type = parameter.Schema?.Type ?? parameter.Ref.Split('/').Last();

		if (String.IsNullOrWhiteSpace(name))
		{
			return String.Empty;
		}

		return $"{Titleize(type)} {name}, ";
	}

	private static string ParseCode(string code)
	{
		if (String.Equals("default", code, StringComparison.OrdinalIgnoreCase))
		{
			return "default:";
		}

		if (Int32.TryParse(code, out var result))
		{
			return $"case (int)HttpStatusCode.{(System.Net.HttpStatusCode) result}:";
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

	private static string? Titleize(string? source)
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
}