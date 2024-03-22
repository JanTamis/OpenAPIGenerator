using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace OpenAPIGenerator.Builder;

public class CodeStringBuilder(int defaultIndent = 0)
{
	private readonly List<ICode> _codes = new List<ICode>();

	public CodeStringBuilder AppendCode(string code)
	{
		_codes.Add(new Line
		{
			Code = code,
		});

		return this;
	}

	public Block AppendBlock(string? header = null, string open = "{", string close = "}")
	{
		var block = new Block
		{
			Header = header,
			Open = open,
			Close = close,
		};

		_codes.Add(block);
		return block;
	}

	public void AddBlocks(IEnumerable<string> blocks)
	{
		_codes.AddRange(blocks.Select(s =>
		{
			var block = new Block();
			block.AppendCode(s);

			return block;
		}));
	}

	public override string ToString()
	{
		var builder = new StringBuilder();

		for (var i = 0; i < _codes.Count; i++)
		{
			_codes[i].Append(builder, defaultIndent, i != 0 ? _codes[i - 1] : null);
		}

		return builder.ToString();
	}
}

public class Block : ICode
{
	private readonly List<ICode> _codes = new List<ICode>();

	public string? Header { get; set; }
	public string Open { get; set; }
	public string Close { get; set; }

	public Block AppendCode(string code)
	{
		_codes.Add(new Line
		{
			Code = code,
		});

		return this;
	}

	public Block AppendCode(IEnumerable<string> code)
	{
		foreach (var item in code)
		{
			AppendCode(item);
		}

		return this;
	}

	public Block AddBlock(string? header = null, string open = "{", string close = "}")
	{
		var block = new Block
		{
			Header = header,
			Open = open,
			Close = close,
		};

		_codes.Add(block);
		return block;
	}

	public void AddBlocks(IEnumerable<string?> blocks)
	{
		_codes.AddRange(blocks
			.Where(w => !String.IsNullOrEmpty(w))
			.Select(s =>
			{
				var block = new Block();
				block.AppendCode(s);

				return block;
			}));
	}

	public void Append(StringBuilder builder, int indent, ICode? previous)
	{
		if (previous != null)
		{
			builder.AppendLine();
		}

		builder.AppendIndented(Header, indent);
		builder.AppendIndented(Open, indent);

		for (var i = 0; i < _codes.Count; i++)
		{
			if (_codes[i] is Line)
			{
				if (i != 0 && _codes[i - 1] is Block)
				{
					builder.AppendLine();
				}
			}
			else if (i != 0)
			{
				builder.AppendLine();
			}

			if (String.IsNullOrEmpty(Open) && String.IsNullOrEmpty(Close))
			{
				_codes[i].Append(builder, indent, i != 0 ? _codes[i - 1] : this);
			}
			else
			{
				_codes[i].Append(builder, indent + 1, i != 0 ? _codes[i - 1] : this);
			}			
		}

		builder.AppendIndented(Close, indent);
	}
}

public class Line : ICode
{
	public string Code { get; set; }

	public void Append(StringBuilder builder, int indent, ICode? previous)
	{
		if (previous != null || previous is Block)
		{
			builder.AppendLine();
		}

		builder.Append('\t', indent);
		builder.Append(Code.Replace("\n", "\n" + new string('\t', indent)));
	}
}

public static class StringBuilderExtensions
{
	public static StringBuilder AppendIndented(this StringBuilder builder, string? text, int indentation)
	{
		if (!String.IsNullOrEmpty(text))
		{
			builder.AppendLine();
			builder.Append('\t', Math.Max(0, indentation));
			builder.Append(text);
		}

		return builder;
	}
}