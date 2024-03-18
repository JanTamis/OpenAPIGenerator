using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class LinkModel
{
	[JsonPropertyName("operationRef")]
	public string OperationRef { get; set; }
	
	[JsonPropertyName("operationId")]
	public string OperationId { get; set; }
	
	[JsonPropertyName("parameters")]
	public Dictionary<string, string> Parameters { get; set; }
	
	[JsonPropertyName("requestBody")]
	public string RequestBody { get; set; }
	
	[JsonPropertyName("description")]
	public string Description { get; set; }
	
	[JsonPropertyName("server")]
	public ServerModel Server { get; set; }
}