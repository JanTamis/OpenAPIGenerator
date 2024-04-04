using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class BlockBuilder(IEnumerable<IBuilder> content) : IBuilder
{
	public IEnumerable<IBuilder> Content { get; } = content;

	public void Build(IndentedStringBuilder builder)
	{
		builder.AppendLine("{");

		using (builder.Indent())
		{
			foreach (var item in Content)
			{
				item.Build(builder);
				builder.AppendLine();
			}
		}
		
		builder.Append('}');
	}
}