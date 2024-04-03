using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace OpenAPIGenerator.Builder;

public class TypeBuilder : BaseTypeBuilder 
{
	public TypeKind TypeKind { get; set; } = TypeKind.Class;

	public IEnumerable<PropertyBuilder> Properties { get; set; }
	public IEnumerable<MethodBuilder> Methods { get; set; }

	public TypeBuilder(string typeName)
	{
		TypeName = typeName;
	}

	public override void Build(IndentedStringBuilder builder)
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
			builder.AppendLine($"namespace {Namespace};");
			builder.AppendLine();
		}

		if (!String.IsNullOrWhiteSpace(Summary))
		{
			builder.AppendLine("/// <summary>");
			builder.AppendLines(Summary!, x => $"/// {x}");
			builder.AppendLine("/// </summary>");
		}

		foreach (var attribute in Attributes ?? [])
		{
			attribute.Build(builder);
			builder.AppendLine();
		}

		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');

		builder.Append(TypeKind switch
		{
			TypeKind.Class     => "class",
			TypeKind.Structure => "struct",
			TypeKind.Interface => "interface",
			_                  => throw new ArgumentOutOfRangeException(nameof(TypeKind), TypeKind, "Invalid type kind"),
		});

		builder.Append(' ');
		builder.Append(ToTypeName(TypeName));

		builder.AppendLine();
		builder.AppendLine("{");

		using (builder.Indent())
		{
			var properties = Properties.ToList();
			var methods = Methods.ToList();

			for (var i = 0; i < properties.Count; i++)
			{
				properties[i].Build(builder);
				builder.AppendLine();

				if (i < properties.Count - 1)
				{
					builder.AppendLine();
				}
			}

			if (properties.Count != 0 && methods.Count != 0)
			{
				builder.AppendLine();
			}

			for (var i = 0; i < methods.Count; i++)
			{
				methods[i].Build(builder);
				builder.AppendLine();

				if (i < methods.Count - 1)
				{
					builder.AppendLine();
				}
			}
		}

		builder.Append("}");
	}
}