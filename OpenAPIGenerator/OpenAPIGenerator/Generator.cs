using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Schema;
using OpenAPIGenerator.Model;

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

	void Generate(SourceProductionContext context, (Compilation compilation, ImmutableArray<(string, string)> files) compilationAndFiles)
	{
		var path = compilationAndFiles.compilation.SyntaxTrees
			.Select(s => Path.GetDirectoryName(s.FilePath))
			.FirstOrDefault() ?? String.Empty;

		var rootNamespace = compilationAndFiles.compilation.AssemblyName ?? path.Split(Path.DirectorySeparatorChar).Last();

		foreach (var file in compilationAndFiles.files)
		{
			var model = JsonSerializer.Deserialize<OpenApiModel>(file.Item2, JsonSerializerOptions.Default);

			context.AddSource(model.Info.Title, OpenApiParser.Parse(model, rootNamespace));
		}
	}
}