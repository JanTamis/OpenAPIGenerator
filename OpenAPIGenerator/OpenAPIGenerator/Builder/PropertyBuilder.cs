using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public record PropertyBuilder : IBuilder
{
	public IEnumerable<AttributeBuilder> Attributes { get; set; }
	public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;
	public PropertyModifier PropertyModifier { get; set; } = PropertyModifier.Get | PropertyModifier.Set;

	public string Name { get; set; }
	public string TypeName { get; set; }
	public string? DefaultValue { get; set; }
	public string? Summary { get; set; }
	
	public IEnumerable<IBuilder> GetContent { get; set; }
	public IEnumerable<IBuilder> SetContent { get; set; }

	public PropertyBuilder(string typeName, string name)
	{
		Name = Builder.ToTypeName(name);
		TypeName = typeName;
	}

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

		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');
		builder.Append(TypeName);
		builder.Append(' ');
		builder.Append(Builder.ToTypeName(Name));
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
}