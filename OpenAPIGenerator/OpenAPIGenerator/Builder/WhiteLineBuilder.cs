namespace OpenAPIGenerator.Builders;

public struct WhiteLineBuilder : IBuilder
{
	public void Build(IndentedStringBuilder builder)
	{
		builder.AppendLine();
	}
}