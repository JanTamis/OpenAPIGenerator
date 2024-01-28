using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class RequestBodyModel
{
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("content")]
	public Dictionary<string, MediaTypeModel> Content { get; set; }
	
	[JsonPropertyName("required")]
	public bool Required { get; set; }
}