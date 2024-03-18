using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class SchemaModel
{
	[JsonPropertyName("descimonator")]
	public DescriminatorModel? Descriminator { get; set; }
	
	[JsonPropertyName("xml")]
	public XmlModel? Xml { get; set; }
	
	[JsonPropertyName("type")]
	public string? Type { get; set; }
	
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocumentationModel? ExternalDocs { get; set; }
}