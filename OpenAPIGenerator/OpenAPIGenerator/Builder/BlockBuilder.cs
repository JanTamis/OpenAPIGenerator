using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class BlockBuilder(IEnumerable<IBuilder> content) : IBuilder, IContent
{
	public string Suffix { get; set; } = String.Empty;
	public IEnumerable<IBuilder> Content { get; set; } = content;

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
		builder.Append(Suffix);
	}
}