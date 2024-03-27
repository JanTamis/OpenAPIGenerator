using System;
using System.Text;

namespace OpenAPIGenerator.Builder;

public struct PropertyBuilder : IBuilder
{
	public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
	public PropertyModifier PropertyModifier { get; set; } = PropertyModifier.Get | PropertyModifier.Set;
	
	public string Name { get; set; }
	public string TypeName { get; set; }
	public string? DefaultValue { get; set; }
	public string? Summary { get; set; }

	public PropertyBuilder(string name, string typeName)
	{
		Name = name;
		TypeName = typeName;
	}
	
	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');
		builder.Append(TypeName);
		builder.Append(' ');
		builder.Append(Name);
		builder.Append(' ');

		builder.Append(" { ");

		if (PropertyModifier.HasFlag(PropertyModifier.Get))
		{
			builder.Append("get; ");
		}

		if (PropertyModifier.HasFlag(PropertyModifier.Set))
		{
			builder.Append("get; ");
		}

		if (PropertyModifier.HasFlag(PropertyModifier.Init))
		{
			builder.Append("init; ");
		}

		builder.Append('}');

		if (!String.IsNullOrWhiteSpace(DefaultValue))
		{
			builder.Append(" = ");
			builder.Append(DefaultValue);
			builder.Append(';');
		}
	}
}