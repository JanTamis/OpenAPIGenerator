using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ResponseModel
{
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("headers")]
	public Dictionary<string, HeaderModel> Headers { get; set; }
	
	[JsonPropertyName("schema")]
	public SchemaModel Schema { get; set; }
	
	[JsonPropertyName("examples")]
	public Dictionary<string, ExampleModel> Examples { get; set; }
}