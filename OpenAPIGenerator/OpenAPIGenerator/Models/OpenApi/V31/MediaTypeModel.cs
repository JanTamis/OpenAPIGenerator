using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Models.OpenApi.V31;

public class MediaTypeModel
{
    [JsonPropertyName("schema")]
    public SchemaModel Schema { get; set; }

    [JsonPropertyName("example")]
    public object Example { get; set; }

    [JsonPropertyName("examples")]
    public Dictionary<string, ExampleModel> Examples { get; set; }

    [JsonPropertyName("encoding")]
    public Dictionary<string, EncodingModel> Encoding { get; set; }
}