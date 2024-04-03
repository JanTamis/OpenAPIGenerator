using System;
using System.Collections.Generic;

namespace OpenAPIGenerator.Builder;

public class MethodBuilder(string methodName) : IBuilder
{
	public string MethodName { get; set; } = methodName;
	public string? Summary { get; set; }
	public string ReturnType { get; set; } = "void";
	
	public bool IsAsync { get; set; }
	
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

		if (IsAsync)
		{
			if (ReturnType == "void")
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

		builder.Append(')');
		builder.AppendLine("{");
		
		using (builder.Indent())
		{
			foreach (var content in Content)
			{
				content.Build(builder);
				builder.AppendLine();
			}
		}

		builder.Append("}");
	}
}