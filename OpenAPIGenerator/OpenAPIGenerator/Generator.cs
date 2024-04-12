using System;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using OpenAPIGenerator.Builders;
using Microsoft.OpenApi.Readers.Interface;
using System.Runtime;

namespace OpenAPIGenerator;

[Generator]
public class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider
			.Where(x => String.Equals(Path.GetExtension(x.Path), ".json"))
			.Select((s, token) => (s.Path, s.GetText()?.ToString()));

		var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());

		context.RegisterSourceOutput(compilationAndFiles, Generate);
	}

	public void Generate(SourceProductionContext context, (Compilation compilation, ImmutableArray<(string path, string? content)> files) compilationAndFiles)
	{
		var path = compilationAndFiles.compilation.SyntaxTrees
			.Select(s => Path.GetDirectoryName(s.FilePath))
			.FirstOrDefault() ?? String.Empty;

		var rootNamespace = compilationAndFiles.compilation.AssemblyName ?? path.Split(Path.DirectorySeparatorChar).Last();

		context.AddSource("ApiException", $$""""
			using System;
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
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Headers;

			namespace {{rootNamespace}};

			public sealed class ApiException<TResult> : ApiException
			{
				public TResult? Result { get; }
				
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

		var reader = new OpenApiTextReaderReader();

		foreach (var file in compilationAndFiles.files)
		{
			using var stream = new StringReader(file.content);
			var model = reader.Read(stream, out var diagnostics);

			foreach (var diagnostic in diagnostics.Errors)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"OpenAPIGenerator",
						diagnostic.ToString(),
						diagnostic.ToString(),
						"OpenAPIGenerator",
						DiagnosticSeverity.Error,
						true
					),
					Location.Create(file.path, default, default)
				));
			}

			foreach (var diagnostic in diagnostics.Warnings)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"OpenAPIGenerator",
						diagnostic.ToString(),
						diagnostic.ToString(),
						"OpenAPIGenerator",
						DiagnosticSeverity.Warning,
						true
					),
					Location.Create(file.path, default, default)
				));
			}

			foreach (var item in model.Components.Schemas)
			{
				// context.AddSource($"Models/{BaseTypeBuilder.ToTypeName(name)}", OpenApiV2.OpenApiV2Parser.ParseObject(name.TrimStart('_'), item.Value, rootNamespace));
				context.AddSource(Builder.ToTypeName(item.Key), ToType(item.Value, Builder.ToTypeName(item.Key), rootNamespace));
			}

			// // context.AddSource("UnprocessableEntity", "public class UnprocessableEntity {}");
			// context.AddSource(model.Info.Title.Replace(' ', '_'), OpenApiV2.OpenApiV2Parser.Parse(model, rootNamespace));
		}
	}

	private string ToType(OpenApiSchema schema, string typeName, string rootNamespace)
	{
		var builder = new TypeBuilder(typeName)
		{
			Usings = ["System", "System.Text.Json.Serialization"],
			Namespace = rootNamespace,
			Summary = schema.Description,
			Properties = schema.Properties
				.Select(s =>
				{
					var type = GetTypeName(s.Value);
					//var type = s.Value.Type ?? Builder.ToTypeName(s.Value.Reference.Id);
					return Builder.Property(type, s.Key) with
					{
						Summary = s.Value.Description,
						Attributes = [Builder.Attribute("JsonPropertyName", $"\"{s.Key}\"")],
					};
				}),
		};

		return Builder.ToString(builder);
	}

	private string GetTypeName(OpenApiSchema schema)
	{
		var type = schema.Type;
		
		if (schema.Items is not null)
		{
			type = GetTypeName(schema.Items) + "[]";
		}
		else if (schema.Reference is not null)
		{
			type = schema.Reference.Id;
		}

		return Builder.ToTypeName(type + (schema.Nullable ? "?" : ""));
	}
}