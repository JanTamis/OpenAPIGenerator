using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ParameterModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("in")]
	public string In { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
	
	[JsonPropertyName("required")]
	public bool Required { get; set; }
	
	[JsonPropertyName("deprecated")]
	public bool Deprecated { get; set; }
	
	[JsonPropertyName("allowEmptyValue")]
	public bool AllowEmptyValue { get; set; }
	
	[JsonPropertyName("style")]
	public string Style { get; set; }
	
	[JsonPropertyName("explode")]
	public bool Explode { get; set; }
	
	[JsonPropertyName("allowReserved")]
	public bool AllowReserved { get; set; }
	
	[JsonPropertyName("schema")]
	public SchemaModel Schema { get; set; }
	
	[JsonPropertyName("example")]
	public object Example { get; set; }
	
	[JsonPropertyName("examples")]
	public Dictionary<string, ExampleModel> Examples { get; set; }
	
	[JsonPropertyName("content")]
	public Dictionary<string, MediaTypeModel> Content { get; set; }
}