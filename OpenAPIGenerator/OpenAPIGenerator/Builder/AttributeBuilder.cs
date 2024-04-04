using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Builders;

public readonly struct AttributeBuilder(string name, params string[] parameters) : IBuilder
{
	public string Name { get; } = name;
	public IEnumerable<string> Parameters { get; } = parameters;

	public void Build(IndentedStringBuilder builder)
	{
		var parameters = Parameters.ToList();
		
		builder.Append('[');
		builder.Append(Name);

		if (parameters.Count > 0)
		{
			builder.Append('(');
			builder.Append(parameters[0]);
			
			for (var i = 1; i < parameters.Count; i++)
			{
				builder.Append(", ");
				builder.Append(parameters[i]);
			}
			
			builder.Append(')');
		}

		builder.Append(']');
	}
}