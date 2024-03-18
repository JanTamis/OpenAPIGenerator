using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class CallbackModel
{
    [JsonPropertyName("callback")]
    public Dictionary<string, PathModel> Callback { get; set; }
}