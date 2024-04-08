using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenAPIGenerator.Builders;

public class SwitchBuilder : IBuilder
{
	public string Expression { get; set; }
	
	public IEnumerable<CaseBuilder> Cases { get; set; }
	
	public IEnumerable<IBuilder> Default { get; set; }
	
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

			if (Default.Any())
			{
				builder.AppendLine("default:");

				using (builder.Indent())
				{
					foreach (var item in Default)
					{
						item.Build(builder);
						builder.AppendLine();
					}

					builder.AppendLine("break;");
				}
			}
		}
		
		builder.Append('}');
	}
}