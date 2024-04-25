using Microsoft.OpenApi.Models;
using OpenAPIGenerator.Builders;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Helpers;

public static class PathHelper
{
	public static IEnumerable<IBuilder> ParseRequestPath(string path, IList<OpenApiParameter> parameters)
	{
		var hasQueryHoles = false;
		var hasHoles = false;
		var result = path;

		var optionalParameters = parameters.Where(w => !w.Required && w.In == ParameterLocation.Query).ToList();

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

			yield return Builder.Line($"url.AppendQuery(\"{parameter.Name}\", {Builder.ToParameterName(parameter.Name)});");
		}
	}
}
