using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class ResponsesModel
{
    [JsonPropertyName("default")]
    public ResponseModel Default { get; set; }

    [JsonPropertyName("responses")]
    public Dictionary<string?, ResponseModel?> Responses { get; set; }
}