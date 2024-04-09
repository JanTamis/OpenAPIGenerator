using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenAPIGenerator.Builders;

public abstract class BaseTypeBuilder : IBuilder
{
	public string TypeName { get; set; }
	public string? Namespace { get; set; }
	public string? Summary { get; set; }
	public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

	public IEnumerable<string> Usings { get; set; }

	public IEnumerable<AttributeBuilder> Attributes { get; set; }
	
	public abstract void Build(IndentedStringBuilder builder);

	public static string ToTypeName(string name)
	{
		if (String.Equals(name, "integer", StringComparison.InvariantCultureIgnoreCase) || String.Equals(name, "int", StringComparison.InvariantCultureIgnoreCase))
		{
			return "int";
		}

		if (String.Equals(name, "string", StringComparison.InvariantCultureIgnoreCase))
		{
			return "string";
		}

		var parts = name.Split('_');

		for (var i = 0; i < parts.Length; i++)
		{
			if (!string.IsNullOrEmpty(parts[i]))
			{
				parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
			}
		}

		return Regex.Replace(string.Join("", parts), @"[^a-zA-Z0-9_]", String.Empty);
	}
}