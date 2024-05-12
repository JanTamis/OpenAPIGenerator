using System;
using System.Collections.Generic;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace OpenAPIGenerator.Helpers;

public class OpenApiSchemaComparer : IEqualityComparer<OpenApiSchema>
{
	public bool Equals(OpenApiSchema x, OpenApiSchema y)
	{
		if (x == y)
		{
			return true;
		}
		
		var jsonX = x?.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
		var jsonY = y?.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

		return jsonX == jsonY;
	}
	
	public int GetHashCode(OpenApiSchema obj)
	{
		var json = obj.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

		return json.GetHashCode();
	}
}