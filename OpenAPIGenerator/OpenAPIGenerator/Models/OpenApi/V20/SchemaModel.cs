using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class SchemaModel
{
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
	
	[JsonPropertyName("format")]
	public string Format { get; set; }
	
	[JsonPropertyName("title")]
	public string Title { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("default")]
	public object Default { get; set; }
	
	[JsonPropertyName("multipleOf")]
	public double? MultipleOf { get; set; }
	
	[JsonPropertyName("maximum")]
	public double? Maximum { get; set; }
	
	[JsonPropertyName("exclusiveMaximum")]
	public bool? ExclusiveMaximum { get; set; }
	
	[JsonPropertyName("minimum")]
	public double? Minimum { get; set; }
	
	[JsonPropertyName("exclusiveMinimum")]
	public bool? ExclusiveMinimum { get; set; }
	
	[JsonPropertyName("maxLength")]
	public int? MaxLength { get; set; }
	
	[JsonPropertyName("minLength")]
	public int? MinLength { get; set; }
	
	[JsonPropertyName("pattern")]
	public string Pattern { get; set; }
	
	[JsonPropertyName("maxItems")]
	public int? MaxItems { get; set; }
	
	[JsonPropertyName("minItems")]
	public int? MinItems { get; set; }
	
	[JsonPropertyName("uniqueItems")]
	public bool? UniqueItems { get; set; }
	
	[JsonPropertyName("maxProperties")]
	public int? MaxProperties { get; set; }
	
	[JsonPropertyName("minProperties")]
	public int? MinProperties { get; set; }
	
	[JsonPropertyName("required")]
	public List<string> Required { get; set; }
	
	[JsonPropertyName("enum")]
	public List<object> Enum { get; set; }
	
	[JsonPropertyName("type")]
	public string Type { get; set; }
	
	[JsonPropertyName("items")]
	public SchemaModel Items { get; set; }
	
	[JsonPropertyName("allOf")]
	public List<SchemaModel> AllOf { get; set; }
	
	[JsonPropertyName("properties")]
	public Dictionary<string, SchemaModel> Properties { get; set; }
	
	[JsonPropertyName("additionalProperties")]
	public SchemaModel AdditionalProperties { get; set; }
	
	[JsonPropertyName("discriminator")]
	public string Discriminator { get; set; }
	
	[JsonPropertyName("readOnly")]
	public bool? ReadOnly { get; set; }
	
	[JsonPropertyName("xml")]
	public XmlModel Xml { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocsModel ExternalDocs { get; set; }
	
	[JsonPropertyName("example")]
	public object Example { get; set; }
}