using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class OAuthFlowsModel
{
	[JsonPropertyName("implicit")]
	public OAuthFlowModel Implicit { get; set; }
	
	[JsonPropertyName("password")]
	public OAuthFlowModel Password { get; set; }
	
	[JsonPropertyName("clientCredentials")]
	public OAuthFlowModel ClientCredentials { get; set; }
	
	[JsonPropertyName("authorizationCode")]
	public OAuthFlowModel AuthorizationCode { get; set; }
}