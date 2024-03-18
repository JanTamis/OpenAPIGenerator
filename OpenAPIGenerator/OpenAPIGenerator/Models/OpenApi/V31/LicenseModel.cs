using System;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class LicenseModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("url")]
    public Uri? Url { get; set; }
}