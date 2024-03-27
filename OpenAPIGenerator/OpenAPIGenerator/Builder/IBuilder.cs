using System.Text;

namespace OpenAPIGenerator.Builder;

public interface IBuilder
{
	void Build(IndentedStringBuilder builder);
}