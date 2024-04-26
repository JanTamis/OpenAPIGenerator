using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAPIGenerator;

public partial class Program
{
	[GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}\+\d{4}$")]
	public partial Regex GetRegex();

	public static void Main(string[] args)
	{
		
	}
}