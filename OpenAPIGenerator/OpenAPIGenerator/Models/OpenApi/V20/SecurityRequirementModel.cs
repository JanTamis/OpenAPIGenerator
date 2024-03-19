using System.Collections.Generic;

namespace OpenAPIGenerator.Models.OpenApi.V20;

public class SecurityRequirementModel
{
	public Dictionary<string, string[]> Security { get; set; }
}