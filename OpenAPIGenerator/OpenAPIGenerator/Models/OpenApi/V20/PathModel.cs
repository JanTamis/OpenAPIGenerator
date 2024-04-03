using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class PathModel
{
	[JsonPropertyName("$ref")]
	public string Ref { get; set; }
	
	[JsonPropertyName("get")]
	public OperationModel? Get { get; set; } 
	
	[JsonPropertyName("put")]
	public OperationModel? Put { get; set; }
	
	[JsonPropertyName("post")]
	public OperationModel? Post { get; set; }
	
	[JsonPropertyName("delete")]
	public OperationModel? Delete { get; set; }
	
	[JsonPropertyName("options")]
	public OperationModel? Options { get; set; }
	
	[JsonPropertyName("head")]
	public OperationModel? Head { get; set; }
	
	[JsonPropertyName("patch")]
	public OperationModel? Patch { get; set; }
	
	[JsonPropertyName("parameters")]
	public List<ParameterModel> Parameters { get; set; }

	public IEnumerable<KeyValuePair<string, OperationModel?>> GetOperations()
	{
		yield return new (nameof(Get), Get);
		yield return new (nameof(Post), Post);
		yield return new (nameof(Put), Put);
		yield return new (nameof(Delete), Post);
		yield return new (nameof(Options), Options);
		yield return new (nameof(Head), Head);
		yield return new (nameof(Patch), Patch);
	}
}