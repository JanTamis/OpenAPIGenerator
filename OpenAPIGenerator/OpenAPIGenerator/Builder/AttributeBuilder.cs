using System.Collections.Generic;
using System.Linq;

namespace OpenAPIGenerator.Builder;

public struct AttributeBuilder : IBuilder
{
	public string Name { get; set; }
	public IEnumerable<ParameterBuilder> Parameters { get; set; }
	
	public void Build(IndentedStringBuilder builder)
	{
		var parameters = Parameters.ToList();
		
		builder.Append('[');
		builder.Append(Name);

		if (parameters.Any())
		{
			builder.Append('(');
			parameters[0].Build(builder);
			
			foreach (var parameter in parameters.Skip(1))
			{
				builder.Append(", ");
				parameter.Build(builder);
			}
			
			builder.Append(')');
			builder.Append(']');
		}
	}
}