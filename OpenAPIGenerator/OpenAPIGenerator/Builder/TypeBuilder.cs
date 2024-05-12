using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CodeAnalysis;
using Microsoft.OpenApi.Extensions;
using OpenAPIGenerator.Enumerators;

namespace OpenAPIGenerator.Builders;

public class TypeBuilder : BaseTypeBuilder
{
	public TypeKind TypeKind { get; set; } = TypeKind.Class;

	public TypeAttributes Modifiers { get; set; } = TypeAttributes.None;

	public IEnumerable<PropertyBuilder> Properties { get; set; } = [];
	public IEnumerable<ConstructorBuilder> Constructors { get; set; } = [];
	public IEnumerable<MethodBuilder> Methods { get; set; } = [];

	public TypeBuilder(string typeName)
	{
		TypeName = Builder.ToTypeName(typeName);
	}

	public override void Build(IndentedStringBuilder builder)
	{
		foreach (var @using in Usings.Distinct().OrderBy(o => o))
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

		if (Modifiers.HasFlag(TypeAttributes.Static))
		{
			builder.Append("static ");
		}

		if (Modifiers.HasFlag(TypeAttributes.Sealed))
		{
			builder.Append("sealed ");
		}

		if (Modifiers.HasFlag(TypeAttributes.Partial))
		{
			builder.Append("partial ");
		}

		builder.Append(TypeKind switch
		{
			TypeKind.Class => "class",
			TypeKind.Structure => "struct",
			TypeKind.Interface => "interface",
			_ => throw new ArgumentOutOfRangeException(nameof(TypeKind), TypeKind, "Invalid type kind"),
		});

		builder.Append(' ');
		builder.Append(TypeName);

		builder.AppendLine();
		builder.AppendLine("{");

		using (builder.Indent())
		{
			var properties = Properties.ToList();
			var methods = Methods.ToList();
			var constructors = Constructors.ToList();

			for (var i = 0; i < properties.Count; i++)
			{
				properties[i].Build(builder);
				builder.AppendLine();

				if (i < properties.Count - 1)
				{
					builder.AppendLine();
				}
			}

			if (properties.Count != 0 && constructors.Count != 0)
			{
				builder.AppendLine();
			}

			for (var i = 0; i < constructors.Count; i++)
			{
				constructors[i].TypeName = TypeName;

				constructors[i].Build(builder);
				builder.AppendLine();

				if (i < constructors.Count - 1)
				{
					builder.AppendLine();
				}
			}

			if (properties.Count != 0 && methods.Count != 0 && constructors.Count != 0)
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