using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class PathModel
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

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

    [JsonPropertyName("trace")]
    public OperationModel? Trace { get; set; }

    [JsonPropertyName("servers")]
    public ServerModel[]? Servers { get; set; }

    [JsonPropertyName("parameters")]
    public Parameter[]? Parameters { get; set; }

    [JsonIgnore]
    public IEnumerable<OperationModel> Operations
    {
        get
        {
            if (Get is not null)
                yield return Get;

            if (Put is not null)
                yield return Put;

            if (Post is not null)
                yield return Post;

            if (Delete is not null)
                yield return Delete;

            if (Options is not null)
                yield return Options;

            if (Head is not null)
                yield return Head;

            if (Patch is not null)
                yield return Patch;

            if (Trace is not null)
                yield return Trace;
        }
    }
}