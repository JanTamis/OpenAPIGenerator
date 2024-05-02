using Markdig;
using Markdig.Syntax;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenAPIGenerator.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Markdig.Extensions.Tables;

namespace OpenAPIGenerator.Helpers;

public static class TypeHelper
{
	private static readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	public static string GetTypeName(OpenApiSchema? schema)
	{
		if (schema is null)
		{
			return String.Empty;
		}

		var type = schema.Type;

		if (type is "integer")
		{
			type = "int";
		}

		if (schema.Items is not null)
		{
			if (schema.UniqueItems == true)
			{
				type = $"HashSet<{GetTypeName(schema.Items)}>";
			}
			else
			{
				type = $"{GetTypeName(schema.Items)}[]";
			}
		}
		else if (schema is { Type: "string", Format: "byte" })
		{
			return "byte[]" + (schema.Nullable ? "?" : "");
		}
		else if (schema.Type is not null && schema.Type != "object")
		{
			if (!String.IsNullOrEmpty(schema.Format))
			{
				type = schema.Format;
			}
		}
		else if (schema.Reference is not null)
		{
			type = schema.Reference.Id;
		}

		return type.ToLower() switch
		{
			"uri"               => "Uri",
			"uuid"              => "Guid",
			"int32"             => "int",
			"int64" or "long"   => "long",
			"float"             => "float",
			"double"            => "double",
			"byte"              => "byte",
			"boolean" or "bool" => "bool",
			"binary"            => "byte[]",
			"date"              => "DateTime",
			"date-time"         => "DateTime",
			"password"          => "string",
			_                   => Builder.ToTypeName(type),
		} + (schema.Nullable ? "?" : "");
	}

	public static string GetTypeName(OpenApiParameter parameter)
	{
		var schema = parameter.Schema ?? parameter.Content.FirstOrDefault().Value.Schema;

		if (schema.Enum.Any())
		{
			return Builder.ToTypeName(parameter.Name);
		}

		return GetTypeName(schema);
	}

	public static string ToType(OpenApiParameter parameter)
	{
		var builder = new EnumBuilder()
		{
			TypeName = Builder.ToTypeName(parameter.Name),
			Members = parameter.Schema.Enum
				.OfType<OpenApiString>()
				.Select(s => new EnumMemberBuilder(s.Value, null, Builder.Attribute("EnumMember", $"Value = \"{s.Value}\"")))
		};

		return Builder.ToString(builder);
	}

	public static string ToType(OpenApiSchema schema, string typeName)
	{
		typeName = Builder.ToTypeName(typeName);

		if (schema.Enum.Any())
		{
			var enumBuilder = new EnumBuilder
			{
				TypeName = typeName,
				Summary = schema.Description,
				Attributes = [Builder.Attribute("JsonConverter", $"typeof(JsonStringEnumConverter<{typeName}>)")],
				Members = schema.Enum
					.OfType<OpenApiString>()
					.Select(s => new EnumMemberBuilder(s.Value, null, Builder.Attribute("EnumMember", $"Value = \"{s.Value}\"")))
			};

			return Builder.ToString(enumBuilder);
		}

		var builder = new TypeBuilder(typeName)
		{
			Modifiers = Enumerators.TypeAttributes.Sealed,
			Summary = ParseComment(schema.Description),
			Properties = schema.Properties
				.Select(s =>
				{
					var isRequired = schema.Required.Contains(s.Key);
					var type = TypeHelper.GetTypeName(s.Value);
					var isString = type == "string";

					if (!isRequired)
					{
						type += "?";
					}
					else
					{
						type = $"required {type}";
					}

					var attributes = new List<AttributeBuilder>()
					{
						Builder.Attribute("JsonPropertyName", $"\"{s.Key}\""),
					};

					if (isString && !String.IsNullOrWhiteSpace(s.Value.Pattern))
					{
						attributes.Add(Builder.Attribute("RegularExpression", $"@\"{s.Value.Pattern}\""));
					}

					if (isString && s.Value.MaxLength.HasValue || s.Value.MinLength.HasValue)
					{
						var attribute = Builder.Attribute("StringLength");

						if (s.Value.MaxLength.HasValue)
						{
							attribute.Parameters = attribute.Parameters.Append(s.Value.MaxLength.Value.ToString());
						}

						if (s.Value.MinLength.HasValue && s.Value.MinLength.Value > 0)
						{
							attribute.Parameters = attribute.Parameters.Append($"MinimumLength = {s.Value.MinLength.Value}");
						}

						attributes.Add(attribute);
					}
					else
					{
						if (s.Value.MaxLength.HasValue)
						{
							attributes.Add(Builder.Attribute("MaxLength", s.Value.MaxLength.Value.ToString()));
						}

						if (s.Value.MinLength.HasValue)
						{
							attributes.Add(Builder.Attribute("MinLength", s.Value.MinLength.Value.ToString()));
						}
					}

					attributes.Add(Builder.Attribute("JsonIgnore", "Condition = JsonIgnoreCondition.WhenWritingDefault"));

					return Builder.Property(type, s.Key) with
					{
						Summary = ParseComment(s.Value.Description),
						Attributes = attributes,
					};
				}),
		};

		builder.Properties = builder.Properties.Concat([Builder.Property("IDictionary<string, object>", "AdditionalProperties", null, Builder.Attribute("JsonExtensionData"))]);

		return Builder.ToString(builder);
	}

	public static string? ParseParameterComment(OpenApiParameter parameter)
	{
		var result = ParseComment(parameter.Description);

		if (parameter.Example is OpenApiString example)
		{
			result += $"<br/> Example: {example.Value}";
		}

		return result;
	}

	public static string? ParseComment(string? comment)
	{
		if (comment is null)
		{
			return null;
		}

		var document = Markdown.Parse(comment, pipeline);
		var builder = new IndentedStringBuilder();

		foreach (var item in document)
		{
			switch (item)
			{
				case ParagraphBlock paragraph:
					// builder.AppendLine("<para>");

					// using (builder.Indent())
					// {
					builder.AppendLines(GetText(paragraph));
					// }

					// builder.AppendLine("</para>");
					break;

				case ListBlock list:
				{
					builder.AppendLine("<list type=\"bullet\">");

					using (builder.Indent())
					{
						foreach (var listItem in list)
						{
							builder.AppendLine("<item>");

							using (builder.Indent())
							{
								builder.AppendLines($"<description>{GetTextRaw(listItem).Replace("<br>", String.Empty).TrimStart(list.BulletType).Trim()}</description>");
							}

							builder.AppendLine("</item>");
						}
					}

					builder.AppendLine("</list>");
					break;
				}

				case HeadingBlock header:
					builder.AppendLines($"<b>{GetText(header).TrimStart(header.HeaderChar).Trim()}</b><br/>");
					break;
				case HtmlBlock html:
					// builder.AppendLine("<code>");
					// builder.AppendLines(GetTextRaw(html).Replace("<br>", "<br/>"));
					// builder.AppendLine("</code>");
					break;
				case FencedCodeBlock code:
					var text = GetTextRaw(code).Trim(code.FencedChar).Trim();

					if (text.Contains('\n'))
					{
						builder.AppendLine("<code>");
						builder.AppendLines(text);
						builder.AppendLine("</code>");
					}
					else
					{
						builder.AppendLine($"<c>{text}</c><br/>");
					}

					break;

				case Table table:
					builder.AppendLine("<list type=\"table\">");

					using (builder.Indent())
					{
						foreach (var row in table.OfType<TableRow>())
						{
							using (builder.Indent())
							{
								foreach (var cell in row.OfType<TableCell>())
								{
									builder.AppendLine(row.IsHeader ? "<listheader>" : "<item>");

									if (!row.IsHeader)
									{
										builder.AppendLines($"<description>{ParseComment(GetTextRaw(cell)).Trim()}</description>");
									}
									else
									{
										builder.AppendLines($"<term>{ParseComment(GetTextRaw(cell)).Trim()}</term>");
									}

									builder.AppendLine(row.IsHeader ? "</listheader>" : "</item>");
								}
							}
						}
					}

					builder.Append("</list>");
					break;
				default:
					builder.AppendLines(GetText(item));
					break;
			}
		}

		return builder.ToString();

		string GetText(Block block)
		{
			return GetTextRaw(block).TrimEnd().Replace("\n", "<br/>\n").Replace("<br>", "<br/>\n");
		}

		string GetTextRaw(Block block)
		{
			return comment.Substring(block.Span.Start, block.Span.Length);
		}
	}
}