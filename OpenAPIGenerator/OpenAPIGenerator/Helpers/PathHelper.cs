using Microsoft.OpenApi.Models;
using OpenAPIGenerator.Builders;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Expressions;

namespace OpenAPIGenerator.Helpers;

public static class PathHelper
{
	public static IEnumerable<IBuilder> ParseRequestPath(string path, IList<OpenApiParameter> parameters)
	{
		var hasQueryHoles = false;
		var hasHoles = false;
		var result = path;

		var optionalParameters = parameters
			.Where(w => !w.Required && w.In == ParameterLocation.Query)
			.OrderBy(o => o.Schema?.Enum.Any() == true)
			.ToList();

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Path))
		{
			result = result.Replace('{' + parameter.Name + '}', "{" + Builder.ToParameterName(parameter.Name) + "}");
			hasHoles = true;
		}

		var isFirst = true;

		foreach (var parameter in parameters.Where(w => w.In == ParameterLocation.Query && w.Required))
		{
			if (isFirst)
			{
				result += '?';
				isFirst = false;
			}

			var name = Builder.ToParameterName(parameter.Name);

			result += $"{parameter.Name}={{{name}}}&";
			hasQueryHoles = true;
		}

		if (!hasQueryHoles)
		{
			result += "?";
		}

		if (!optionalParameters.Any())
		{
			result = result.TrimEnd('&', '?');
		}

		if (hasQueryHoles || hasHoles || optionalParameters.Any())
		{
			yield return Builder.Line($"var url = new UrlBuilder($\"{result.TrimEnd('?')}\");");
		}
		else
		{
			yield return Builder.Line($"var url = \"{result.TrimEnd('?', '&')}\";");
		}

		if (optionalParameters.Any())
		{
			yield return Builder.WhiteLine();
		}

		for (var i = 0; i < optionalParameters.Count; i++)
		{
			var parameter = optionalParameters[i];

			if (parameter.Schema?.Enum.Any() == true)
			{
				var enums = parameter.Schema.Enum
					.OfType<OpenApiString>()
					.Select(s => (s.Value, Builder.ToTypeName(s.Value)))
					.ToList();
					
				var typeName = Builder.ToTypeName(parameter.Name);
				var maxLength = enums.Max(m => m.Item2.Length);
				
				if (parameter.Required)
				{
					yield return Builder.Line($"url.AppendQuery(\"{parameter.Name}\", {Builder.ToParameterName(parameter.Name)} switch");

					var block = Builder.Block(");");

					foreach (var item in enums)
					{
						Builder.Append(block, Builder.Line($"{typeName}.{item.Item2}{new string(' ', maxLength - item.Item2.Length)} => \"{item.Value}\","));
					}

					Builder.Append(block, Builder.Line($"_{new string(' ', maxLength + typeName.Length)} => null"));

					yield return Builder.Line(");");
				}
				else
				{
					var ifStatement = Builder.If($"{Builder.ToParameterName(parameter.Name)}.HasValue");
					
					Builder.Append(ifStatement, Builder.Line($"url.AppendQuery(\"{parameter.Name}\", {Builder.ToParameterName(parameter.Name)}.Value switch"));

					var block = Builder.Block(");");

					foreach (var item in enums)
					{
						Builder.Append(block, Builder.Line($"{typeName}.{item.Item2}{new string(' ', maxLength - item.Item2.Length)} => \"{item.Value}\","));
					}

					Builder.Append(block, Builder.Line($"_{new string(' ', maxLength + typeName.Length)} => null"));

					Builder.Append(ifStatement, block);

					yield return Builder.WhiteLine();
					yield return ifStatement;
				}
				
				continue;
			}

			yield return Builder.Line($"url.AppendQuery(\"{parameter.Name}\", {Builder.ToParameterName(parameter.Name)});");
		}
	}
}
