using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class LicenseModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("url")]
	public string Url { get; set; }
}