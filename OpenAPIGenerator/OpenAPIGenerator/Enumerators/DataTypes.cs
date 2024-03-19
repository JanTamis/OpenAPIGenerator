using System.Text.Json.Serialization;

namespace OpenAPIGenerator.Enumerators;

public enum DataTypes
{
	[JsonPropertyName("string")]
	String,
	
	[JsonPropertyName("number")]
	Number,
	
	[JsonPropertyName("integer")]
	Integer,
	
	[JsonPropertyName("boolean")]
	Boolean,
	
	[JsonPropertyName("array")]
	Array,
	
	[JsonPropertyName("object")]
	Object,
}