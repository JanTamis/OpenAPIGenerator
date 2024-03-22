using System.Text;

namespace OpenAPIGenerator.Builder
{
	public interface ICode
	{
		void Append(StringBuilder builder, int indent, bool isFirst);
	}
}