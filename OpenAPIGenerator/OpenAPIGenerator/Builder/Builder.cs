using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenAPIGenerator.Builders;

public static class Builder
{
	public static LineBuilder Line(string code) => new LineBuilder(code);

	public static WhiteLineBuilder WhiteLine() => new WhiteLineBuilder();

	public static BlockBuilder Block(string suffix, params IBuilder[] content) => new BlockBuilder(content) { Suffix = suffix };
	public static BlockBuilder Block(params IBuilder[] content) => new BlockBuilder(content);
	public static BlockBuilder Block(IEnumerable<IBuilder> content, string suffix = "") => new BlockBuilder(content) { Suffix = suffix };

	public static IfBuilder If(string condition, params IBuilder[] content) => new IfBuilder(condition, content);
	public static IfBuilder If(string condition, IEnumerable<IBuilder> content) => new IfBuilder(condition, content);

	public static PropertyBuilder Property(string typeName, string name, string? summary, params AttributeBuilder[] attributes) => new PropertyBuilder(typeName, name)
	{
		Summary = summary,
		Attributes = attributes,
	};

	public static PropertyBuilder Property(string typeName, string name, string? defaultValue = null, string? summary = null, AccessModifier accessModifier = AccessModifier.Public, PropertyModifier modifier = PropertyModifier.Get | PropertyModifier.Set) => new PropertyBuilder(typeName, name)
	{
		AccessModifier = accessModifier,
		PropertyModifier = modifier,
		Summary = summary,
		DefaultValue = defaultValue,
		Attributes = [],
	};

	public static AttributeBuilder Attribute(string name, params string[] parameters) => new AttributeBuilder(name, parameters);

	public static ParameterBuilder Parameter(string typeName, string name, string? defaultValue = null, string? documentation = null) => new ParameterBuilder(typeName, Builder.ToParameterName(name))
	{
		DefaultValue = defaultValue,
		Documentation = documentation,
	};

	public static MethodBuilder Method(string methodName, params IBuilder[] content) => new MethodBuilder(methodName)
	{
		Content = content,
	};

	public static MethodBuilder Method(string methodName, IEnumerable<IBuilder> content) => new MethodBuilder(methodName)
	{
		Content = content,
	};

	public static MethodBuilder Method(string methodName, string returnType, bool isAsync, AccessModifier accessModifier, IEnumerable<ParameterBuilder> parameters, IEnumerable<IBuilder> content, string? summary = null, IEnumerable<AttributeBuilder>? attributes = null) => new MethodBuilder(methodName)
	{
		ReturnType = returnType,
		IsAsync = isAsync,
		AccessModifier = accessModifier,
		Parameters = parameters,
		Content = content,
		Summary = summary,
		Attributes = attributes,
	};
	
	public static SwitchBuilder Switch(string expression, params CaseBuilder[] cases) => new SwitchBuilder
	{
		Expression = expression,
		Cases = cases,
	};

	public static SwitchBuilder Switch(string expression, IEnumerable<CaseBuilder> cases, CaseBuilder @default) => new SwitchBuilder
	{
		Expression = expression,
		Cases = cases,
		Default = @default
	};

	public static CaseBuilder Case(string condition, params IBuilder[] content) => new CaseBuilder
	{
		Condition = condition,
		Content = content,
	};

	public static CaseBuilder Case(string condition, IEnumerable<IBuilder> content) => new CaseBuilder
	{
		Condition = condition,
		Content = content,
	};

	public static void Append<T>(T source, IBuilder item) where T : IContent
	{
		source.Content = source.Content.Append(item);
	}

	public static void Append<T>(T source, IEnumerable<IBuilder> items) where T : IContent
	{
		source.Content = source.Content.Concat(items);
	}

	public static void Append<T>(T source, params IBuilder[] items) where T : IContent
	{
		Append(source, items.AsEnumerable());
	}

	public static string ToTypeName(string name)
	{
		if (String.Equals(name, "integer", StringComparison.InvariantCultureIgnoreCase) || String.Equals(name, "int", StringComparison.InvariantCultureIgnoreCase))
		{
			return "int";
		}

		if (name.StartsWith("string", StringComparison.InvariantCultureIgnoreCase))
		{
			return "string";
		}

		if (name.StartsWith("byte", StringComparison.InvariantCultureIgnoreCase))
		{
			return name;
		}

		if (String.Equals(name, "object", StringComparison.InvariantCultureIgnoreCase))
		{
			return "object";
		}

		var parts = name.Split('_', '.', '-');

		for (var i = 0; i < parts.Length; i++)
		{
			if (!string.IsNullOrEmpty(parts[i]))
			{
				if (parts[i].All(Char.IsUpper))
				{
					parts[i] = parts[i][0] + parts[i].Substring(1).ToLower();
				}
				else
				{
					parts[i] = Char.ToUpper(parts[i][0]) + parts[i].Substring(1);
				}
			}
		}

		return Regex.Replace(String.Join("", parts), @"[^a-zA-Z0-9_\<\>\[\]]", String.Empty);
	}

	public static string ToParameterName(string name)
	{
		name = ToTypeName(name);

		return Char.ToLower(name[0]) + name.Substring(1);
	}

	public static string ToString<T>(T item) where T : IBuilder
	{
		var builder = new IndentedStringBuilder();
		item.Build(builder);

		return builder.ToString();
	}
}