using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class EncodingModel
{
	[JsonPropertyName("contentType")]
	public string ContentType { get; set; }
	
	[JsonPropertyName("headers")]
	public Dictionary<string, HeaderModel> Headers { get; set; }
	
	[JsonPropertyName("style")]
	public string Style { get; set; }
	
	[JsonPropertyName("explode")]
	public bool Explode { get; set; }
	
	[JsonPropertyName("allowReserved")]
	public bool AllowReserved { get; set; }
}