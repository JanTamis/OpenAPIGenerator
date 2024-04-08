using System.Collections.Generic;

namespace OpenAPIGenerator.Builders;

public interface IContent
{
	IEnumerable<IBuilder> Content { get; set; }
}