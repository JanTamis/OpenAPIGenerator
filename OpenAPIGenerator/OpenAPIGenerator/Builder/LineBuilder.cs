namespace OpenAPIGenerator.Builders;

public class LineBuilder(string code) : IBuilder
{
	public string Code { get; } = code;

	public void Build(IndentedStringBuilder builder)
	{
		builder.Append(Code);
	}
}