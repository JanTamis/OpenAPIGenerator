using OpenAPIGenerator.Sample;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenAPIGenerator;

public partial class Program
{
	public static async Task Main(string[] args)
	{
		var samenwerkingApi = new SamenwerkenAPI();

		var result = await samenwerkingApi.GetAppHealthAsync();

		Console.WriteLine(result);
	}
}