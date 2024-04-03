namespace OpenAPIGenerator.Builder;

public readonly struct LineBuilder(string code) : IBuilder
{
	public string Code { get; } = code;

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(Code);
	}
}