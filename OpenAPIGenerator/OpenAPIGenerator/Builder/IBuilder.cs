using System.Text;

namespace OpenAPIGenerator.Builders;

public interface IBuilder
{
	void Build(IndentedStringBuilder builder);
}