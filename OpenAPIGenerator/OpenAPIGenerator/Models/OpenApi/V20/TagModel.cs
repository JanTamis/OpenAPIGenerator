using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class TagModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocsModel ExternalDocs { get; set; }
}