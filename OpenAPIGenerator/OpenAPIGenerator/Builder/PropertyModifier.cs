using System;

namespace OpenAPIGenerator.Builders;

[Flags]
public enum PropertyModifier
{
	Get,
	Set,
	Init
}