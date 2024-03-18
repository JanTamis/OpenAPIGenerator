using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ExampleModel
{
	[JsonPropertyName("summary")]
	public string Summary { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("value")]
	public object Value { get; set; }
	
	[JsonPropertyName("externalValue")]
	public string ExternalValue { get; set; }
}