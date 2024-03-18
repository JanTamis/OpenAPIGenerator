using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ResponseModel
{
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("$ref")]
	public string? Ref { get; set; }
	
	[JsonPropertyName("headers")]
	public Dictionary<string, HeaderModel> Headers { get; set; }
	
	[JsonPropertyName("content")]
	public Dictionary<string, MediaTypeModel> Content { get; set; }
	
	[JsonPropertyName("links")]
	public Dictionary<string, LinkModel> Links { get; set; }
}