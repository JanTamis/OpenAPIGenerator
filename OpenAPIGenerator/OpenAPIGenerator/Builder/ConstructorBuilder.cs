using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public class ConstructorBuilder : IBuilder, IContent
{
	public string TypeName { get; set; }
	public string? Summary { get; set; }

	public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

	public IEnumerable<AttributeBuilder>? Attributes { get; set; }
	public IEnumerable<ParameterBuilder> Parameters { get; set; }
	
	public IEnumerable<IBuilder> Content { get; set; }

	public void Build(IndentedStringBuilder builder)
	{
		if (!String.IsNullOrWhiteSpace(Summary))
		{
			builder.AppendLine("/// <summary>");
			builder.AppendLines(Summary!, x => $"/// {x}");
			builder.AppendLine("/// </summary>");
		}

		foreach (var parameter in Parameters)
		{
			if (!String.IsNullOrWhiteSpace(parameter.Documentation))
			{
				builder.Append($"/// <param name=\"{parameter.Name}\">");
				builder.AppendLines(parameter.Documentation, x => $"/// {x}", false);
				builder.AppendLine("</param>");
			}
		}

		builder.Append(AccessModifier.ToString().ToLower());
		builder.Append(' ');
		builder.Append(TypeName);
		builder.Append('(');

		var isFirst = true;

		foreach (var parameter in Parameters)
		{
			if (!isFirst)
			{
				builder.Append(", ");
			}
			else
			{
				isFirst = false;
			}

			parameter.Build(builder);
		}

		builder.AppendLine(")");
		
		new BlockBuilder(Content).Build(builder);
	}
}