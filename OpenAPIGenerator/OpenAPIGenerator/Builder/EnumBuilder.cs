using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Builders;

public class EnumBuilder : BaseTypeBuilder
{
	public IEnumerable<EnumMemberBuilder>? Members { get; set; }
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

		builder.Append("enum ");
		builder.Append(ToTypeName(TypeName));

		builder.AppendLine();

		new BlockBuilder(Members).Build(builder);
	}
}