using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace OpenAPIGenerator.Builder;

public class CodeStringBuilder(int defaultIndent = 0)
{
	private readonly List<ICode> _codes = new List<ICode>();
	private readonly int defaultIndent = Math.Max(defaultIndent, 0);

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

	public override string ToString()
	{
		var builder = new StringBuilder();

		for (var i = 0; i < _codes.Count; i++)
		{
			_codes[i].Append(builder, defaultIndent, i == 0);
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

	public Block AddCode(string code)
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
			AddCode(item);
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

	public void Append(StringBuilder builder, int indent, bool isFirst)
	{
		if (!String.IsNullOrWhiteSpace(Header))
		{
			builder.AppendLine();
			builder.Append('\t', Math.Max(0, indent - 1));
			builder.Append(Header);
		}

		builder.AppendLine();
		builder.Append('\t', Math.Max(0, indent - 1));
		builder.Append(Open);

		for (var i = 0; i < _codes.Count; i++)
		{
			if (_codes[i] is Block block)
			{
				if (i != 0)
				{
					builder.AppendLine();
				}
				block.Append(builder, indent + 1, false);
			}
			else if (_codes[i] is Line line)
			{
				if (i != 0 && _codes[i - 1] is Block)
				{
					builder.AppendLine();
				}
				line.Append(builder, indent + 1, false);
			}
		}

		builder.AppendLine();
		builder.Append('\t', Math.Max(0, indent - 1));
		builder.Append(Close);
	}
}

public class Line : ICode
{
	public string Code { get; set; }

	public void Append(StringBuilder builder, int indent, bool isFirst)
	{
		if (!isFirst)
		{
			builder.AppendLine();
		}
		
		builder.Append('\t', indent);
		builder.Append(Code);
	}
}