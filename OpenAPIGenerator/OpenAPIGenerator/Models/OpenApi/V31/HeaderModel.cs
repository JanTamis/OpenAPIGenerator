using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class HeaderModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("externalDocs")]
    public ExternalDocumentationModel? ExternalDocs { get; set; }
}