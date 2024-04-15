using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class EnumMemberBuilder(string name, string? summary, params AttributeBuilder[] attributes) : IBuilder
{
	public string Name { get; set; } = Builder.ToTypeName(name);
	public string? Summary { get; set; } = summary;

	public IEnumerable<AttributeBuilder> Attributes { get; set; } = attributes;

	public void Build(IndentedStringBuilder builder)
	{
		if (!String.IsNullOrWhiteSpace(Summary))
		{
			builder.AppendLine("/// <summary>");
			builder.AppendLines(Summary!, x => $"/// {x}");
			builder.AppendLine("/// </summary>");
		}
		
		foreach (var attribute in Attributes)
		{
			attribute.Build(builder);
			builder.AppendLine();
		}
		
		builder.Append(Name);
		builder.Append(',');
	}
}