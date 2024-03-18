using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public partial class ServerModel
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("variables")]
    public Dictionary<string, ServerVariableModel>? Variables { get; set; }
}