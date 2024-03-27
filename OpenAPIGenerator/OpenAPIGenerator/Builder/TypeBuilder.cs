using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace OpenAPIGenerator.Builder;

public struct TypeBuilder : IBuilder
{
	public string TypeName { get; set; }
	public string? Namespace { get; set; }
	public string? Summary { get; set; }

	public IEnumerable<string> Usings { get; set; }

	public AccessModifier AccessModifier { get; set; }
	public TypeKind TypeKind { get; set; }

	public IEnumerable<PropertyBuilder> Properties { get; set; }

	public TypeBuilder(string typeName)
	{
		TypeName = typeName;
	}

	public void Build(IndentedStringBuilder builder)
	{
		foreach (var @using in Usings)
		{
			builder.Append("using ");
			builder.Append(@using);
			builder.Append(';');
			builder.AppendLine();
		}

		if (Usings.Any())
		{
			builder.AppendLine();
		}
		

		if (!String.IsNullOrWhiteSpace(Namespace))
		{
			builder.AppendLine($"namespace {Namespace}");
			builder.AppendLine();
		}

		if (!String.IsNullOrWhiteSpace(Summary))
		{
			builder.AppendLine("/// <summary>");
			builder.AppendLines(Summary, x => $"/// {x}");
			builder.AppendLine("/// </summary>");
		}

		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');

		builder.Append(TypeKind switch
		{
			TypeKind.Class     => "class",
			TypeKind.Structure => "struct",
			TypeKind.Interface => "interface",
			_                  => "",
		});

		builder.Append(' ');
		builder.Append(TypeName);

		builder.AppendLine();
		builder.AppendLine("{");

		using (builder.Indent())
		{
			var properties = Properties.ToList();

			for (var i = 0; i < properties.Count; i++)
			{
				properties[i].Build(builder);
				builder.AppendLine();

				if (i < properties.Count - 1)
				{
					builder.AppendLine();
				}
			}
		}

		builder.AppendLine("}");
	}
}