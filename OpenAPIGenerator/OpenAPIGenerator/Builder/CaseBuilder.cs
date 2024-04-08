using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Builders;

public record CaseBuilder : IBuilder, IContent
{
	public string Condition { get; set; }

	public bool HasBreak { get; set; } = true;
	
	public IEnumerable<IBuilder> Content { get; set; }
	
	public void Build(IndentedStringBuilder builder)
	{
		builder.Append("case ");
		builder.Append(Condition);
		builder.AppendLine(":");

		using (builder.Indent())
		{
			var content = Content.ToList();
			
			for (var i = 0; i < content.Count; i++)
			{
				content[i].Build(builder);
				if (i < content.Count - 1)
				{
					builder.AppendLine();
				}
			}

			if (HasBreak)
			{
				builder.AppendLine();
				builder.Append("break;");
			}
		}
	}
}