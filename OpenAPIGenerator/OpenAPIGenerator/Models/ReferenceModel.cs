using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ReferenceModel
{ 
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
	
	[JsonPropertyName("summary")]
	public string Summary { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
}