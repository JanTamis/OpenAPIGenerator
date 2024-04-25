using System;

namespace OpenAPIGenerator.Enumerators;

[Flags]
public enum TypeAttributes
{
	None= 0,
	Static = 1,
	Partial = 2,
	Sealed = 4,
}
