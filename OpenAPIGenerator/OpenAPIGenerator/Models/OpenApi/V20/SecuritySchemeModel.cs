using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class SecuritySchemeModel
{
	[JsonPropertyName("type")]
	public string Type { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("in")]
	public string In { get; set; }
	
	[JsonPropertyName("flow")]
	public string Flow { get; set; }
	
	[JsonPropertyName("authorizationUrl")]
	public string AuthorizationUrl { get; set; }
	
	[JsonPropertyName("tokenUrl")]
	public string TokenUrl { get; set; }
	
	[JsonPropertyName("scopes")]
	public Dictionary<string, string> Scopes { get; set; }
}