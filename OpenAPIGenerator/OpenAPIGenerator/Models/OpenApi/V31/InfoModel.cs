using System;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class InfoModel
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("termOfService")]
    public string? TermOfService { get; set; }

    [JsonPropertyName("contact")]
    public ContactModel? Contact { get; set; }

    [JsonPropertyName("license")]
    public LicenseModel? License { get; set; }

    [JsonPropertyName("version")]
    public Version Version { get; set; }
}