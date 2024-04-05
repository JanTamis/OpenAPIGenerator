using System;

namespace OpenAPIGenerator.Builders;

public class ParameterBuilder(string typeName, string name) : IBuilder
{
	public string TypeName { get; set; } = typeName;
	public string Name { get; set; } = name;
	public string? DefaultValue { get; set; }
	public string? Documentation { get; set; }

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(TypeName);
		builder.Append(' ');
		builder.Append(Name);

		if (!String.IsNullOrEmpty(DefaultValue))
		{
			builder.Append(" = ");
			builder.Append(DefaultValue!);
		}
	}
}