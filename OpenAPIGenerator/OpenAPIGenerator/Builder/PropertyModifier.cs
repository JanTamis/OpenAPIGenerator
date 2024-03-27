using System;

namespace OpenAPIGenerator.Builder;

[Flags]
public enum PropertyModifier
{
	Get,
	Set,
	Init
}