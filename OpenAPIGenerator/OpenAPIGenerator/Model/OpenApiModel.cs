using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

// [JsonSerializable(typeof(OpenApiModel))]
public partial class OpenApiModel // : JsonSerializerContext
{
	[JsonPropertyName("openapi")]
	public Version OpenApiVersion { get; set; }

	[JsonPropertyName("info")]
	public InfoModel Info { get; set; }

	[JsonPropertyName("jsonSchemaDialect")]
	public string? JsonSchemaDialect { get; set; }

	[JsonPropertyName("servers")]
	public ServerModel[]? Servers { get; set; }

	[JsonPropertyName("paths")]
	public Dictionary<string, PathModel> Paths { get; set; }
	
	[JsonPropertyName("webhooks")]
	public Dictionary<string, PathModel> Webhooks { get; set; }
	
	[JsonPropertyName("components")]
	public ComponentsModel? Components { get; set; }
	
	[JsonPropertyName("security")]
	public Dictionary<string, string[]>[] Security { get; set; }
	
	[JsonPropertyName("tags")]
	public TagModel[]? Tags { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocumentationModel? ExternalDocs { get; set; }
}