using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RegexParser;
using RegexParser.Nodes;
using RegexParser.Nodes.CharacterClass;
using RegexParser.Nodes.QuantifierNodes;

namespace OpenAPIGenerator;

public partial class Program
{
	[GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}\+\d{4}$")]
	public partial Regex GetRegex();

	public static void Main(string[] args)
	{
		var parser = new Parser(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}\+\d{4}$");
		var result = parser.Parse();

		Console.WriteLine(RegexParser.Parse(result));
	}
}

static class RegexParser
{
	public static string Parse(RegexTree tree)
	{
		return String.Join("\n", tree.Root.ChildNodes
			.SelectMany<RegexNode, string>(Parse));
	}

	private static IEnumerable<string> Parse(CharacterNode node)
	{
		yield return $"\u25cb Match '{node.Character}'.";
	}

	private static IEnumerable<string> Parse(EscapeCharacterNode node)
	{
		yield return $"\u25cb Match '{node.Character}'.";
	}

	private static IEnumerable<string> Parse(CharacterClassNode node)
	{
		var set = node.CharacterSet;

		foreach (var range in set.ChildNodes.OfType<CharacterClassRangeNode>())
		{
			yield return $"Char.IsBetween(value[index++], '{range.Start}', '{range.End}')";
		}
	}

	private static IEnumerable<string> Parse(QuantifierNNode node)
	{		
		return Enumerable.Range(0, node.N)
			.SelectMany(_ => node.ChildNodes
				.SelectMany(Parse));
	}

	private static IEnumerable<string> Parse(CharacterClassShorthandNode node)
	{
		if (node.Shorthand == 'd')
		{
			yield return "Char.IsAsciiDigit(value[index++])";
		}
	}
	
	private static IEnumerable<string> Parse(QuantifierStarNode node)
	{
		yield return $$"""
				while ((uint)index < (uint)value.Length && {{string.Join(" && ", node.ChildNodes.SelectMany(Parse))}})
				{
					index++;
				}
				""";
	}

	private static IEnumerable<string> Parse(RegexNode node)
	{
		return node switch
		{
			CharacterNode characterNode                             => Parse(characterNode),
			EscapeCharacterNode escapeCharacterNode                 => Parse(escapeCharacterNode),
			QuantifierNNode quantifierNNode                         => Parse(quantifierNNode),
			CharacterClassShorthandNode characterClassShorthandNode => Parse(characterClassShorthandNode),
			CharacterClassNode characterClassNode                   => Parse(characterClassNode),
			QuantifierStarNode quantifierStarNode                   => Parse(quantifierStarNode),	
			_                                                       => Enumerable.Empty<string>(),
		};
	}
}