using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenAPIGenerator.Builders;

public class SwitchBuilder : IBuilder
{
	public string Expression { get; set; }
	
	public IEnumerable<CaseBuilder> Cases { get; set; }
	
	public CaseBuilder? Default { get; set; }
	
	public void Build(IndentedStringBuilder builder)
	{
		builder.Append("switch (");
		builder.Append(Expression);
		builder.AppendLine(")");
		builder.AppendLine("{");

		using (builder.Indent())
		{
			foreach (var @case in Cases)
			{
				@case.Build(builder);
				builder.AppendLine();
			}

			if (Default is not null)
			{
				builder.AppendLine("default:");

				using (builder.Indent())
				{
					var content = Default.Content.ToList();

					for (var i = 0; i < content.Count; i++)
					{
						content[i].Build(builder);
						if (i < content.Count - 1)
						{
							builder.AppendLine();
						}
					}

					if (Default.HasBreak)
					{
						builder.AppendLine("break;");
					}
					else
					{
						builder.AppendLine();
					}
				}
			}
		}
		
		builder.Append('}');
	}
}