using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class ExternalDocumentationModel
{
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}