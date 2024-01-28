using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Model;

public class CallbackModel
{
	[JsonPropertyName("callback")]
	public Dictionary<string, PathModel> Callback { get; set; }
}