using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class EnumMemberBuilder(string name, params AttributeBuilder[] attributes) : IBuilder
{
	public string Name { get; set; } = Builder.ToTypeName(name);

	public IEnumerable<AttributeBuilder> Attributes { get; set; } = attributes;

	public void Build(IndentedStringBuilder builder)
	{
		foreach (var attribute in Attributes)
		{
			attribute.Build(builder);
			builder.AppendLine();
		}
		
		builder.Append(Name);
		builder.Append(',');
	}
}