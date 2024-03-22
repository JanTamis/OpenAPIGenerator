using System;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenApiV2 = OpenAPIGenerator.Models.OpenApi.V20;

namespace OpenAPIGenerator;

[Generator]
public class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider
			.Where(x => String.Equals(Path.GetExtension(x.Path), ".json"))
			.Select((s, token) => (s.Path, s.GetText(token).ToString()));

		var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());

		context.RegisterSourceOutput(compilationAndFiles, Generate);
	}

	public void Generate(SourceProductionContext context, (Compilation compilation, ImmutableArray<(string, string)> files) compilationAndFiles)
	{
		var path = compilationAndFiles.compilation.SyntaxTrees
			.Select(s => Path.GetDirectoryName(s.FilePath))
			.FirstOrDefault() ?? String.Empty;

		var rootNamespace = compilationAndFiles.compilation.AssemblyName ?? path.Split(Path.DirectorySeparatorChar).Last();

		context.AddSource("ApiException", $$""""
			using System;
			using System.Collections.Generic;
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Headers;
			
			namespace {{rootNamespace}};
			
			public class ApiException : Exception
			{
				public HttpStatusCode StatusCode { get; private set; }
			
				public HttpResponseHeaders Headers { get; private set; }
				
				public ApiException(string message, HttpResponseMessage response)
						: this(message, response.StatusCode, response.Headers, null)
				{
				
				}
			
				public ApiException(string message, HttpStatusCode statusCode, HttpResponseHeaders headers, Exception innerException)
						: base($"""
					{message}
					
					Status: {statusCode}
					""", innerException)
				{
					StatusCode = statusCode;
					Headers = headers;
				}
			
				public override string ToString()
				{
					return base.ToString();
				}
			}
			"""");

		context.AddSource("ApiException`T", $$""""
			using System;
			using System.Collections.Generic;
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Headers;
			
			namespace {{rootNamespace}};

			public sealed class ApiException<TResult> : ApiException
			{
				public TResult? Result { get; private set; }
				
				public ApiException(string message, HttpResponseMessage response, TResult? result)
					: this(message, response.StatusCode, response.Headers, result, null)
			{
			
			}
			
				public ApiException(string message, HttpStatusCode statusCode, HttpResponseHeaders headers, TResult? result, Exception innerException)
						: base(message, statusCode, headers, innerException)
				{
					Result = result;
				}
			}
			"""");

		foreach (var file in compilationAndFiles.files)
		{
			var model = JsonSerializer.Deserialize<OpenApiV2.SwaggerModel>(file.Item2, JsonSerializerOptions.Default);

			foreach (var item in model.Definitions)
			{
				var name = OpenApiV2.OpenApiV2Parser.Titleize(item.Key);
				context.AddSource($"Models/{OpenApiV2.OpenApiV2Parser.Titleize(name.TrimStart('_'))}", OpenApiV2.OpenApiV2Parser.ParseObject(name.TrimStart('_'), item.Value, rootNamespace));
			}

			// context.AddSource("UnprocessableEntity", "public class UnprocessableEntity {}");
			context.AddSource(model.Info.Title.Replace(' ', '_'), OpenApiV2.OpenApiV2Parser.Parse(model, rootNamespace));
		}
	}
}