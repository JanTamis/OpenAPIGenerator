using System;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class ContactModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public Uri? Url { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}