using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class IfBuilder(string condition, IEnumerable<IBuilder> content) : IBuilder
{
	public string Condition { get; set; } = condition;

	public IEnumerable<IBuilder> Content { get; set; } = content;

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append("if (");
		builder.Append(Condition);
		builder.AppendLine(")");

		Builder.Block(Content)
			.Build(builder);
	}
}