using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ExternalDocsModel
{
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("url")]
	public string Url { get; set; }
}