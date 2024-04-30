using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Builders;

public class IfBuilder(string condition, IEnumerable<IBuilder> content) : IBuilder, IContent
{
	public string Condition { get; set; } = condition;

	public IEnumerable<IBuilder> Content { get; set; } = content;

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append("if (");
		builder.Append(Condition);
		builder.AppendLine(")");

		// 	if (content.Count() == 1 && Content.FirstOrDefault() is LineBuilder line)
		// 	{
		// 		using (builder.Indent())
		// 		{
		// 			line.Build(builder);
		// 		}
		// 	}
		// 	else
		// 	{
		// 		Builder.Block(Content)
		// 		.Build(builder);
		// 	}		
		// }

		Builder.Block(Content)
			.Build(builder);
	}
}