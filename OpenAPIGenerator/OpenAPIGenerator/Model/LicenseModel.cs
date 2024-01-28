using System;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class LicenseModel
{
	[JsonPropertyName("name")]
	public string Name { get; set; }
	
	[JsonPropertyName("identifier")]
	public string? Identifier { get; set; }
	
	[JsonPropertyName("url")]
	public Uri? Url { get; set; }
}