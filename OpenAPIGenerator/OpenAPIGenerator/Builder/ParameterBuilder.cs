namespace OpenAPIGenerator.Builder;

public class ParameterBuilder : IBuilder
{
	public string TypeName { get; set; }
	public string Name { get; set; }
	
	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(TypeName);
		builder.Append(' ');
		builder.Append(Name);
	}
}