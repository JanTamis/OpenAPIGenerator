using System.Collections.Generic;
using OpenAPIGenerator.Builders;

namespace OpenAPIGenerator.Builders;

public static class Builder
{
	public static LineBuilder Line(string code) => new LineBuilder(code);
	
	public static WhiteLineBuilder WhiteLine() => new WhiteLineBuilder();
	
	public static BlockBuilder Block(params IBuilder[] content) => new BlockBuilder(content);
	public static BlockBuilder Block(IEnumerable<IBuilder> content) => new BlockBuilder(content);
	
	public static IfBuilder If(string condition, params IBuilder[] content) => new IfBuilder(condition, content);
	public static IfBuilder If(string condition, IEnumerable<IBuilder> content) => new IfBuilder(condition, content);

	public static string ToString<T>(T item) where T : IBuilder
	{
		var builder = new IndentedStringBuilder();
		item.Build(builder);

		return builder.ToString();
	}
}