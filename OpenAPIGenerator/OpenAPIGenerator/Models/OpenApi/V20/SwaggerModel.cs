using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class SwaggerModel
{
	[JsonPropertyName("swagger")]
	public string Swagger { get; set; }
	
	[JsonPropertyName("info")]
	public InfoModel Info { get; set; }
	
	[JsonPropertyName("host")]
	public string Host { get; set; }
	
	[JsonPropertyName("basePath")]
	public string BasePath { get; set; }
	
	[JsonPropertyName("schemes")]
	public string[] Schemes { get; set; }
	
	[JsonPropertyName("consumes")]
	public string[] Consumes { get; set; }
	
	[JsonPropertyName("produces")]
	public string[] Produces { get; set; }
	
	[JsonPropertyName("paths")]
	public Dictionary<string, PathModel> Paths { get; set; }
	
	[JsonPropertyName("definitions")]
	public Dictionary<string, SchemaModel> Definitions { get; set; }
	
	[JsonPropertyName("parameters")]
	public Dictionary<string, ParameterModel> Parameters { get; set; }
	
	[JsonPropertyName("responses")]
	public Dictionary<string, ResponseModel> Responses { get; set; }
	
	[JsonPropertyName("securityDefinitions")]
	public Dictionary<string, SecuritySchemeModel> SecurityDefinitions { get; set; }
	
	[JsonPropertyName("security")]
	public SecurityRequirementModel[] Security { get; set; }
	
	[JsonPropertyName("tags")]
	public TagModel[] Tags { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocsModel ExternalDocs { get; set; }
}