namespace OpenAPIGenerator.Builders;

public class WhiteLineBuilder : IBuilder
{
	public void Build(IndentedStringBuilder builder)
	{
		builder.AppendLine();
	}
}