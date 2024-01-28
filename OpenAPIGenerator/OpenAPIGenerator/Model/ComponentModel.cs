using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class ComponentsModel
{
	[JsonPropertyName("schemas")]
	public Dictionary<string, SchemaModel> Schemas { get; set; }
	
	[JsonPropertyName("responses")]
	public Dictionary<string, ResponseModel> Responses { get; set; }
	
	[JsonPropertyName("parameters")]
	public Dictionary<string, ParameterModel> Parameters { get; set; }
	
	[JsonPropertyName("examples")]
	public Dictionary<string, ExampleModel> Examples { get; set; }
	
	[JsonPropertyName("requestBodies")]
	public Dictionary<string, RequestBodyModel> RequestBodies { get; set; }
	
	[JsonPropertyName("headers")]
	public Dictionary<string, HeaderModel> Headers { get; set; }
	
	[JsonPropertyName("securitySchemes")]
	public Dictionary<string, SecuritySchemeModel> SecuritySchemes { get; set; }
	
	[JsonPropertyName("links")]
	public Dictionary<string, LinkModel> Links { get; set; }
	
	[JsonPropertyName("callbacks")]
	public Dictionary<string, CallbackModel> Callbacks { get; set; }
	
	[JsonPropertyName("pathItems")]
	public Dictionary<string, PathModel> PathItems { get; set; }
}