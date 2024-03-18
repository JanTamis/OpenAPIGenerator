using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class SecuritySchemeModel
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("in")]
    public string In { get; set; }

    [JsonPropertyName("scheme")]
    public string Scheme { get; set; }

    [JsonPropertyName("bearerFormat")]
    public string BearerFormat { get; set; }

    [JsonPropertyName("flows")]
    public OAuthFlowsModel Flows { get; set; }

    [JsonPropertyName("openIdConnectUrl")]
    public string OpenIdConnectUrl { get; set; }
}