using System;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.OpenApi.Readers;
using OpenAPIGenerator.Builders;

namespace OpenAPIGenerator;

[Generator]
public class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var files = context.AdditionalTextsProvider
			.Where(x => Path.GetExtension(x.Path).ToLower() is ".json" or ".yaml")
			.Select((s, token) => (s.Path, s.GetText()?.ToString()));

		var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());

		context.RegisterSourceOutput(compilationAndFiles, Generate);
	}

	public void Generate(SourceProductionContext context, (Compilation compilation, ImmutableArray<(string path, string? content)> files) compilationAndFiles)
	{
		if (!compilationAndFiles.files.Any())
		{
			return;
		}

		var path = compilationAndFiles.compilation.SyntaxTrees
			.Select(s => Path.GetDirectoryName(s.FilePath))
			.FirstOrDefault() ?? String.Empty;

		var rootNamespace = compilationAndFiles.compilation.AssemblyName ?? path.Split(Path.DirectorySeparatorChar).Last();

		context.AddSource("ApiException", $$""""
			using System;
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Headers;

			namespace {{rootNamespace}};

			public class ApiException : Exception
			{
				public HttpStatusCode StatusCode { get; private set; }
			
				public HttpResponseHeaders Headers { get; private set; }
				
				public ApiException(string message, HttpResponseMessage response)
						: this(message, response.StatusCode, response.Headers, null)
				{
				
				}
			
				public ApiException(string message, HttpStatusCode statusCode, HttpResponseHeaders headers, Exception? innerException)
						: base($"""
					{message}
					
					Status: {statusCode}
					""", innerException)
				{
					StatusCode = statusCode;
					Headers = headers;
				}
			
				public override string ToString()
				{
					return base.ToString();
				}
			}
			"""");

		context.AddSource("ApiException`T", $$""""
			using System;
			using System.Net;
			using System.Net.Http;
			using System.Net.Http.Headers;

			namespace {{rootNamespace}};

			public sealed class ApiException<TResult> : ApiException
			{
				public TResult? Result { get; }
				
				public ApiException(string message, HttpResponseMessage response, TResult? result)
					: this(message, response.StatusCode, response.Headers, result, null)
				{
				
				}
			
				public ApiException(string message, HttpStatusCode statusCode, HttpResponseHeaders headers, TResult? result, Exception? innerException)
						: base(message, statusCode, headers, innerException)
				{
					Result = result;
				}
			}
			"""");

		context.AddSource("UrlBuilder", $$"""
			using System;
			using System.Buffers;
			using System.Runtime.CompilerServices;

			namespace {{rootNamespace}};

			public struct UrlBuilder
			{
				private ArrayPool<char> _pool;
				private char[] _buffer;
				private int _index;
			
				private bool hasQuery = false;
			
				public UrlBuilder()
				{
					_pool = ArrayPool<char>.Shared;
					_buffer = _pool.Rent(128);
					_index = 0;
				}
			
				public UrlBuilder(UrlBuilderHandler builder)
				{
					_pool = builder._pool;
					_buffer = builder._buffer;
					_index = builder._index;
					this.hasQuery = _buffer.AsSpan(0, _index).Contains('?');
				}
			
				public void AppendQuery(string key, string? value)
				{
					if (value is not null)
					{
						value = Uri.EscapeDataString(value);
			
						if (!hasQuery)
						{
							Append('?');
							hasQuery = true;
						}
			
						AppendLiteral(key);
						Append('=');
						AppendLiteral(value);
						Append('&');
					}
				}
			
				public void AppendQuery<T>(string key, T value) where T : ISpanFormattable
				{
					if (!hasQuery)
					{
						Append('?');
						hasQuery = true;
					}
			
					AppendLiteral(key);
					Append('=');
					AppendFormatted(value);
					Append('&');
				}
			
				public void AppendQuery<T>(string key, T? value) where T : struct, ISpanFormattable
				{
					if (value.HasValue)
					{
						AppendQuery(key, value.Value);
					}
				}
			
				public string ToStringAndClear()
				{
					var result = new string(_buffer, 0, _index);
			
					_pool.Return(_buffer);
			
					return result;
				}
			
				private void AppendLiteral(string? s)
				{
					if (s is not null)
					{
						s = Uri.EscapeDataString(s);
			
						while (!s.TryCopyTo(GetBuffer()))
						{
							Grow();
						}
			
						_index += s.Length;
					}
				}
			
				private void AppendFormatted<T>(T item) where T : ISpanFormattable
				{
					var charsWritten = 0;
			
					while (!item.TryFormat(GetBuffer(), out charsWritten, ReadOnlySpan<char>.Empty, null))
					{
						Grow();
					}
			
					_index += charsWritten;
				}
			
				private void AppendFormatted<T>(T? item) where T : struct, ISpanFormattable
				{
					if (item.HasValue)
					{
						AppendFormatted(item.Value);
					}
				}
			
				public void Append(char item)
				{
					if (_index >= _buffer.Length)
					{
						Grow();
					}
			
					_buffer[_index++] = item;
				}
			
				private void Grow()
				{
					var buffer = _buffer;
					_buffer = _pool.Rent(buffer.Length * 2);
					buffer.CopyTo(_buffer, 0);
			
					_pool.Return(buffer);
				}
			
				private Span<char> GetBuffer()
				{
					if (_index >= _buffer.Length)
					{
						return Span<char>.Empty;
					}
			
					return _buffer.AsSpan(_index);
				}
			
				[InterpolatedStringHandler]
				public ref struct UrlBuilderHandler
				{
					public readonly ArrayPool<char> _pool;
					public char[] _buffer;
					public int _index;
			
					public UrlBuilderHandler(int literalLength, int formattedCount)
					{
						_pool = ArrayPool<char>.Shared;
						_buffer = _pool.Rent(literalLength + formattedCount * 11);
						_index = 0;
					}
			
					public void AppendLiteral(string? s)
					{
						if (s is not null)
						{
							s = Uri.EscapeDataString(s);
			
							while (!s.TryCopyTo(GetBuffer()))
							{
								Grow();
							}
			
							_index += s.Length;
						}
					}
			
					public void AppendFormatted<T>(T item) where T : ISpanFormattable
					{
						var charsWritten = 0;
			
						while (!item.TryFormat(GetBuffer(), out charsWritten, ReadOnlySpan<char>.Empty, null))
						{
							Grow();
						}
			
						_index += charsWritten;
					}
					
					public void AppendFormatted(string item)
					{
						AppendLiteral(item);
					}
			
					public void AppendFormatted<T>(T? item) where T : struct, ISpanFormattable
					{
						if (item.HasValue)
						{
							AppendFormatted(item.Value);
						}
					}
			
					public void Append(char item)
					{
						if (_index >= _buffer.Length)
						{
							Grow();
						}
			
						_buffer[_index++] = item;
					}
			
					private void Grow()
					{
						var buffer = _buffer;
						_buffer = _pool.Rent(buffer.Length * 2);
						buffer.CopyTo(_buffer, 0);
			
						_pool.Return(buffer);
					}
			
					private Span<char> GetBuffer()
					{
						if (_index >= _buffer.Length)
						{
							return Span<char>.Empty;
						}
			
						return _buffer.AsSpan(_index);
					}
				}
			}
			""");

		var reader = new OpenApiTextReaderReader();

		foreach (var file in compilationAndFiles.files)
		{
			using var stream = new StringReader(file.content);
			var model = reader.Read(stream, out var diagnostics);

			foreach (var diagnostic in diagnostics.Errors)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"OpenAPIGenerator",
						diagnostic.ToString(),
						diagnostic.ToString(),
						"OpenAPIGenerator",
						DiagnosticSeverity.Error,
						true
					),
					Location.Create(file.path, default, default)
				));
			}

			foreach (var diagnostic in diagnostics.Warnings)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					new DiagnosticDescriptor(
						"OpenAPIGenerator",
						diagnostic.ToString(),
						diagnostic.ToString(),
						"OpenAPIGenerator",
						DiagnosticSeverity.Warning,
						true
					),
					Location.Create(file.path, default, default)
				));
			}

			context.AddSource(Builder.ToTypeName(model.Info.Title), OpenApiParser.Parse(model, rootNamespace));
		}
	}
}