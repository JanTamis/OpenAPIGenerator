using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public abstract partial class ServerVariableModel
{
    [JsonPropertyName("enum")]
    public string[] Enum { get; set; }

    [JsonPropertyName("default")]
    public string Default { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}