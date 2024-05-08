﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Testura.Code.Compilations;

/// <summary>
/// Provides functionality to compile in memory or to file.
/// </summary>
public class Compiler : ICompiler
{
    private readonly string[] _referencedAssemblies;
    private readonly IEnumerable<string> _defaultNamespaces;

    /// <summary>
    /// Initializes a new instance of the <see cref="Compiler"/> class.
    /// </summary>
    /// <param name="referencedAssemblies">Path to all required external assemblies.</param>
    public Compiler(string[]? referencedAssemblies = null)
    {
        _referencedAssemblies = referencedAssemblies ?? Array.Empty<string>();
        _defaultNamespaces = new[]
        {
            "System",
            "System.IO",
            "System.Net",
            "System.Linq",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Collections.Generic"
        };
    }

    /// <summary>
    /// Compile code from a file without creating a dll.
    /// </summary>
    /// <param name="pathsToCsFiles">Paths to cs files.</param>
    /// <returns>The result from the compilation.</returns>
    public async Task<CompileResult> CompileFilesInMemoryAsync(params string[] pathsToCsFiles)
    {
        return await CompileFilesAsync(null, pathsToCsFiles);
    }

    /// <summary>
    /// Compile code from a string source without creating a dll.
    /// </summary>
    /// <param name="source">Source strings to compile.</param>
    /// <returns>The result from the compilation.</returns>
    public async Task<CompileResult> CompileSourceInMemoryAsync(params string[] source)
    {
        return await CompileSourceAsync(null, source);
    }

    /// <summary>
    /// Compile code from a file into a dll.
    /// </summary>
    /// <param name="outputPath">Where we should save the dlls.</param>
    /// <param name="pathsToCsFiles">Path to the cs files.</param>
    /// <returns>The result from the compilation.</returns>
    public async Task<CompileResult> CompileFilesAsync(string outputPath, params string[] pathsToCsFiles)
    {
        if (pathsToCsFiles.Length == 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(pathsToCsFiles));
        }

        var source = new string[pathsToCsFiles.Length];
        for (int n = 0; n < pathsToCsFiles.Length; n++)
        {
            source[n] = File.ReadAllText(pathsToCsFiles[n]);
        }

        return await CompileSourceAsync(outputPath, source);
    }

    /// <summary>
    /// Compile code from a string source into a dll.
    /// </summary>
    /// <param name="outputPath">Where we should save the dlls.</param>
    /// <param name="source">Source string to compile.</param>
    /// <returns>The result of the compilation.</returns>
    public async Task<CompileResult> CompileSourceAsync(string outputPath, params string[] source)
    {
        if (source.Length == 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(source));
        }

        return await Task.Run(() =>
        {
            var parsedSyntaxTrees = new SyntaxTree[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                parsedSyntaxTrees[i] = Parse(
                    source[i],
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            }

            var defaultCompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithUsings(_defaultNamespaces);

            var compilation = CSharpCompilation.Create(
                string.IsNullOrEmpty(outputPath) ? "temporary" : Path.GetFileName(outputPath),
                parsedSyntaxTrees,
                ConvertReferenceToMetadataReference(),
                defaultCompilationOptions);

            EmitResult result;

            if (string.IsNullOrEmpty(outputPath))
            {
                using (var memoryStream = new MemoryStream())
                {
                    result = compilation.Emit(memoryStream);
                }
            }
            else
            {
                result = compilation.Emit(outputPath);
            }

            var outputRows = ConvertDiagnosticsToOutputRows(result.Diagnostics);
            return new CompileResult(outputPath, result.Success, outputRows);
        });
    }

    private IList<OutputRow> ConvertDiagnosticsToOutputRows(IEnumerable<Diagnostic> diagnostics)
    {
        var outputRows = new List<OutputRow>();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity < DiagnosticSeverity.Error)
            {
                continue;
            }

            outputRows.Add(new OutputRow { Description = diagnostic.GetMessage(), Severity = diagnostic.Severity.ToString(), ClassName = diagnostic.Id });
        }

        return outputRows;
    }

    private IEnumerable<MetadataReference> ConvertReferenceToMetadataReference()
    {
        var metadata = new List<MetadataReference>();
        foreach (var reference in _referencedAssemblies)
        {
            metadata.Add(MetadataReference.CreateFromFile(reference));
        }

        // Instead of the old system for assuming the given DLLs such as System.dll and System.Core.dll
        // are always present (which is not true under newer .NET versions), we instead have a list of
        // types we get the assembly paths of and use those as references.
        // The list here should cover the basics from the namespaces listed up above.
        var expectedTypes = new Type[]
        {
            typeof(object),
            typeof(File),
            typeof(System.Net.EndPoint),
            typeof(Enumerable),
            typeof(Encoding),
            typeof(System.Text.RegularExpressions.Regex),
            typeof(List<>)
        };
        var assemblies = expectedTypes
            .Select(type => type.Assembly.Location)
            .Distinct()
            .Select(assembly => MetadataReference.CreateFromFile(assembly));
        metadata.AddRange(assemblies);
        return metadata;
    }

    private SyntaxTree Parse(string text, CSharpParseOptions options = null)
    {
        var stringText = SourceText.From(text, Encoding.UTF8);
        return SyntaxFactory.ParseSyntaxTree(stringText, options);
    }
}
