using System.Collections.Generic;

namespace OpenAPIGenerator.Builder;

public class IfBuilder(string condition, params IBuilder[] content) : IBuilder
{
	public string Condition { get; set; } = condition;

	public IEnumerable<IBuilder> Content { get; set; } = content;

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append("if (").Append(Condition).AppendLine(")");
		builder.AppendLine("{");

		using (builder.Indent())
		{
			foreach (var item in Content)
			{
				item.Build(builder);
				builder.AppendLine();
			}
		}

		builder.Append("}");
	}
}