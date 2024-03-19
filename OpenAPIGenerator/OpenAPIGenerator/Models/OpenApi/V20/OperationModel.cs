using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class OperationModel
{
	[JsonPropertyName("tags")]
	public List<string> Tags { get; set; }
	
	[JsonPropertyName("summary")]
	public string Summary { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("externalDocs")]
	public ExternalDocsModel ExternalDocs { get; set; }
	
	[JsonPropertyName("operationId")]
	public string OperationId { get; set; }
	
	[JsonPropertyName("consumes")]
	public List<string> Consumes { get; set; }
	
	[JsonPropertyName("produces")]
	public List<string> Produces { get; set; }
	
	[JsonPropertyName("parameters")]
	public List<ParameterModel> Parameters { get; set; }
	
	[JsonPropertyName("responses")]
	public Dictionary<string, ResponseModel> Responses { get; set; }
	
	[JsonPropertyName("schemes")]
	public List<string> Schemes { get; set; }
	
	[JsonPropertyName("deprecated")]
	public bool Deprecated { get; set; }
	
	[JsonPropertyName("security")]
	public List<Dictionary<string, List<string>>> Security { get; set; }
}