using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class OAuthFlowModel
{
    [JsonPropertyName("authorizationUrl")]
    public string AuthorizationUrl { get; set; }

    [JsonPropertyName("tokenUrl")]
    public string TokenUrl { get; set; }

    [JsonPropertyName("refreshUrl")]
    public string RefreshUrl { get; set; }

    [JsonPropertyName("scopes")]
    public Dictionary<string, string> Scopes { get; set; }
}