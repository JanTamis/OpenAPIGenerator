using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public static class Builder
{
	public static LineBuilder Line(string code) => new LineBuilder(code);
	
	public static WhiteLineBuilder WhiteLine() => new WhiteLineBuilder();
	
	public static BlockBuilder Block(params IBuilder[] content) => new BlockBuilder(content);
	public static BlockBuilder Block(IEnumerable<IBuilder> content) => new BlockBuilder(content);
	
	public static IfBuilder If(string condition, params IBuilder[] content) => new IfBuilder(condition, content);
	public static IfBuilder If(string condition, IEnumerable<IBuilder> content) => new IfBuilder(condition, content);

	public static PropertyBuilder Property(string typeName, string name, string? summary, params AttributeBuilder[] attributes) => new PropertyBuilder(typeName, name)
	{
		Summary = summary,
		Attributes = attributes,
	};

	public static PropertyBuilder Property(string typeName, string name, string? defaultValue = null, string? summary = null, AccessModifier accessModifier = AccessModifier.Public, PropertyModifier modifier = PropertyModifier.Get | PropertyModifier.Set ) => new PropertyBuilder(typeName, name)
	{
		AccessModifier = accessModifier,
		PropertyModifier = modifier,
		Summary = summary,
		DefaultValue = defaultValue,
		Attributes = [],
	};

	public static AttributeBuilder Attribute(string name, params string[] parameters) => new AttributeBuilder(name, parameters);

	public static string ToString<T>(T item) where T : IBuilder
	{
		var builder = new IndentedStringBuilder();
		item.Build(builder);

		return builder.ToString();
	}
}