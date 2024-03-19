using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class InfoModel
{
	[JsonPropertyName("title")]
	public string Title { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("termsOfService")]
	public string TermsOfService { get; set; }
	
	[JsonPropertyName("contact")]
	public ContactModel Contact { get; set; }
	
	[JsonPropertyName("license")]
	public LicenseModel License { get; set; }
	
	[JsonPropertyName("version")]
	public string Version { get; set; }
}