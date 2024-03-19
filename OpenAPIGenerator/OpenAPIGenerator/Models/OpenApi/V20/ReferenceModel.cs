using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ReferenceModel
{
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
}