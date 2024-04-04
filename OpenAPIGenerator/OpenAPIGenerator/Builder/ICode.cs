using System.Text;

namespace OpenAPIGenerator.Builders
{
	public interface ICode
	{
		void Append(StringBuilder builder, int indent, ICode? previous);
	}
}