using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class ResponsesModel
{
	[JsonPropertyName("default")]
	public ResponseModel Default { get; set; }
	
	public Dictionary<string, ResponseModel> Responses { get; set; }
}