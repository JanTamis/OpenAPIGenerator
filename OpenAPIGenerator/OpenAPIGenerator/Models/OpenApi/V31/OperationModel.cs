using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class OperationModel
{
    [JsonPropertyName("tags")]
    public string[] Tags { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("externalDocs")]
    public ExternalDocumentationModel? ExternalDocs { get; set; }

    [JsonPropertyName("operationId")]
    public string OperationId { get; set; }

    [JsonPropertyName("parameters")]
    public ParameterModel[]? Parameters { get; set; }

    [JsonPropertyName("requestBody")]
    public RequestBodyModel RequestBody { get; set; }

    [JsonPropertyName("responses")]

    public Dictionary<string, ResponseModel> Responses { get; set; }
    // public ResponsesModel Responses { get; set; }

    [JsonPropertyName("callbacks")]
    public Dictionary<string, CallbackModel> Callbacks { get; set; }

    [JsonPropertyName("deprecated")]
    public bool Deprecated { get; set; }

    [JsonPropertyName("security")]
    public Dictionary<string, string[]>[] Security { get; set; }

    [JsonPropertyName("servers")]
    public ServerModel[]? Servers { get; set; }
}