using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public struct PropertyBuilder : IBuilder
{
	public IEnumerable<AttributeBuilder> Attributes { get; set; }
	public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
	public PropertyModifier PropertyModifier { get; set; } = PropertyModifier.Get | PropertyModifier.Set;

	public string Name { get; set; }
	public string TypeName { get; set; }
	public string? DefaultValue { get; set; }
	public string? Summary { get; set; }

	public PropertyBuilder(string typeName, string name)
	{
		Name = name;
		TypeName = typeName;
	}

	public void Build(IndentedStringBuilder builder)
	{
		foreach (var attribute in Attributes)
		{
			attribute.Build(builder);
			builder.AppendLine();
		}

		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');
		builder.Append(BaseTypeBuilder.ToTypeName(TypeName));
		builder.Append(' ');
		builder.Append(ConvertToCamelCase(Name));
		builder.Append(' ');

		builder.Append("{ ");

		if (PropertyModifier.HasFlag(PropertyModifier.Get))
		{
			builder.Append("get; ");
		}

		if (PropertyModifier.HasFlag(PropertyModifier.Set))
		{
			builder.Append("set; ");
		}

		if (PropertyModifier.HasFlag(PropertyModifier.Init))
		{
			builder.Append("init; ");
		}

		builder.Append('}');

		if (!String.IsNullOrWhiteSpace(DefaultValue))
		{
			builder.Append(" = ");
			builder.Append(DefaultValue!);
			builder.Append(';');
		}
	}

	private static string ConvertToCamelCase(string name)
	{
		if (String.Equals(name, "integer", StringComparison.InvariantCultureIgnoreCase) || String.Equals(name, "int", StringComparison.InvariantCultureIgnoreCase))
		{
			return "int";
		}

		if (String.Equals(name, "string", StringComparison.InvariantCultureIgnoreCase))
		{
			return "string";
		}

		var parts = name.Split('_');

		for (var i = 0; i < parts.Length; i++)
		{
			if (!String.IsNullOrEmpty(parts[i]))
			{
				parts[i] = Char.ToUpper(parts[i][0]) + parts[i].Substring(1);
			}
		}

		return String.Join("", parts);
	}
}