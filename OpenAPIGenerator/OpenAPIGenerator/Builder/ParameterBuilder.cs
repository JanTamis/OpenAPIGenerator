namespace OpenAPIGenerator.Builders;

public readonly struct ParameterBuilder(string typeName, string name) : IBuilder
{
	public string TypeName { get; } = typeName;
	public string Name { get; } = name;
	public string Documentation { get; }

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(TypeName);
		builder.Append(' ');
		builder.Append(Name);
	}
}