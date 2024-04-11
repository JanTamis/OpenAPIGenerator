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

	public IEnumerable<string> Usings { get; set; } = [];

	public IEnumerable<AttributeBuilder> Attributes { get; set; }
	
	public abstract void Build(IndentedStringBuilder builder);
}