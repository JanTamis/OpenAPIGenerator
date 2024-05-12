using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public record MethodBuilder : IBuilder, IContent
{
	public string MethodName { get; set; }
	public string? Summary { get; set; }
	public string ReturnType { get; set; } = "void";

	public bool IsAsync { get; set; }

	public bool IsPartial { get; set; }

public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

	public IEnumerable<AttributeBuilder>? Attributes { get; set; }
	
	public IEnumerable<ParameterBuilder> Parameters { get; set; }
	public IEnumerable<IBuilder> Content { get; set; }

	public MethodBuilder(string methodName)
	{
		MethodName = methodName;
	}

	public void Build(IndentedStringBuilder builder)
	{
		if (!String.IsNullOrWhiteSpace(Summary))
		{
			builder.AppendLine("/// <summary>");
			builder.AppendLines(Summary!, x => $"/// {x}");
			builder.AppendLine("/// </summary>");
		}

		foreach (var parameter in Parameters ?? [])
		{
			if (!String.IsNullOrWhiteSpace(parameter.Documentation))
			{
				builder.Append($"/// <param name=\"{parameter.Name}\">");
				builder.AppendLines(parameter.Documentation!, x => $"/// {x}", false, skipFinalNewline: true);
				builder.AppendLine("</param>");
			}
		}

		if (AccessModifier != AccessModifier.None)
		{
			builder.Append(AccessModifier.ToString().ToLower());
			builder.Append(' ');
		}

		if (IsPartial)
		{
			builder.Append("partial ");
		}

		if (IsAsync)
		{
			if (ReturnType is "void" or "Task" or "?" || String.IsNullOrWhiteSpace(ReturnType))
			{
				builder.Append("async Task");
			}
			else
			{
				builder.Append("async Task<").Append(ReturnType).Append('>');
			}
		}
		else
		{
			builder.Append(ReturnType);
		}

		builder.Append(' ');
		builder.Append(MethodName);
		builder.Append('(');

		var isFirst = true;
		
		foreach (var parameter in Parameters ?? [])
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

		if (IsPartial)
		{
			builder.Append(");");
			return;
		}

		builder.AppendLine(")");

		Builder.Block(Content).Build(builder);
	}
}