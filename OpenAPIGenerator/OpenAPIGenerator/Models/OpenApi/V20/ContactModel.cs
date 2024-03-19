using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ContactModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("url")]
	public string Url { get; set; }
	
	[JsonPropertyName("email")]
	public string Email { get; set; }
}