using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ExternalDocumentationModel
{
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("url")]
	public string Url { get; set; }
}