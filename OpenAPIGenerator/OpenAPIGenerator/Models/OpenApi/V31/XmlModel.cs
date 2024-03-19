using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class XmlModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("namespace")]
	public string Namespace { get; set; }
	
	[JsonPropertyName("prefix")]
	public string Prefix { get; set; }
	
	[JsonPropertyName("attribute")]
	public bool Attribute { get; set; }
	
	[JsonPropertyName("wrapped")]
	public bool Wrapped { get; set; }
}