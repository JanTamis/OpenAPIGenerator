using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class TagModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocumentationModel? ExternalDocs { get; set; }
}