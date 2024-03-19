using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ParameterModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("in")]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public ParameterLocation In { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("required")]
	public bool Required { get; set; }
	
	[JsonPropertyName("type")]
	public string Type { get; set; }
	
	[JsonPropertyName("format")]
	public string Format { get; set; }
	
	[JsonPropertyName("allowEmptyValue")]
	public bool AllowEmptyValue { get; set; }
	
	[JsonPropertyName("items")]
	public ItemsModel Items { get; set; }
	
	[JsonPropertyName("collectionFormat")]
	public string CollectionFormat { get; set; }
	
	[JsonPropertyName("default")]
	public string Default { get; set; }
	
	[JsonPropertyName("maximum")]
	public int Maximum { get; set; }
	
	[JsonPropertyName("exclusiveMaximum")]
	public bool ExclusiveMaximum { get; set; }
	
	[JsonPropertyName("minimum")]
	public int Minimum { get; set; }
	
	[JsonPropertyName("exclusiveMinimum")]
	public bool ExclusiveMinimum { get; set; }
	
	[JsonPropertyName("maxLength")]
	public int MaxLength { get; set; }
	
	[JsonPropertyName("minLength")]
	public int MinLength { get; set; }
	
	[JsonPropertyName("pattern")]
	public string Pattern { get; set; }
	
	[JsonPropertyName("maxItems")]
	public int MaxItems { get; set; }
	
	[JsonPropertyName("minItems")]
	public int MinItems { get; set; }
	
	[JsonPropertyName("uniqueItems")]
	public bool UniqueItems { get; set; }
	
	[JsonPropertyName("enum")]
	public List<string> Enum { get; set; }
	
	[JsonPropertyName("multipleOf")]
	public int MultipleOf { get; set; }
}