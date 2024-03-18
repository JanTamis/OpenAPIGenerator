using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class DescriminatorModel
{
	[JsonPropertyName("propertyName")]	
	public string PropertyName { get; set; }
	
	[JsonPropertyName("mapping")]
	public Dictionary<string, string>? Mapping { get; set; }
}