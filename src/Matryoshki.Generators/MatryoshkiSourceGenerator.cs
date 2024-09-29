using System.Collections.Immutable;
using System.Text;
using Matryoshki.Generators.Builders;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Pipelines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Matryoshki.Generators;

[Generator(LanguageNames.CSharp)]
public class MatryoshkiSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor SealedTypeRule = new(
        id: "MatryoshkiSourceGeneratorSealedType",
        title: "MT2001: Decoration of sealed type",
        messageFormat: "Sealed type cannot be decorated",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NonInterfaceTypeRule = new(
        id: "MatryoshkiSourceGeneratorNonInterface",
        title: "MT2002: Decoration of non-interface type",
        messageFormat: "Because the type is not interface the decoration will only be applied to virtual and abstract members",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ErrorRule = new(
        id: "MatryoshkiSourceGeneratorError",
        title: "MT3001: Decoration failed",
        messageFormat: "Decoration failed because of exception: {0}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var builtInAdornments = new BuiltInAdornmentsPipeline()
                                .Create(context)
                                .Collect();

        var compiledAdornments = new CompiledAdornmentsPipeline()
            .Create(context);

        var syntaxAdornments = new SyntaxAdornmentsPipeline()
            .Create(context);

        var combinedAdornments
            = compiledAdornments
              .Collect().Combine(syntaxAdornments.Collect())
              .Select((c, _) => (Compiled: c.Left, Syntax: c.Right))
              .Combine(builtInAdornments)
              .Select((c, _) => (c.Left.Compiled, c.Left.Syntax, BuiltIn: c.Right));

        var types = new MatryoshkaTypesPipeline()
            .Create(context);

        var interfaceExtractions = new InterfaceExtractionPipeline()
                                   .Create(context)
                                   .Collect();

        var typesWithAdornments
            = types
              .Collect()
              .Combine(combinedAdornments)
              .Combine(context.CompilationProvider)
              .Select(
                  (c, _) =>
                      new GenerationInput(
                          Metadata: c.Left.Left,
                          Adornments: c.Left.Right.Syntax
                                       .Concat(c.Left.Right.Compiled)
                                       .Concat(c.Left.Right.BuiltIn),
                          Compilation: c.Right,
                          InterfaceExtractions: ImmutableArray<InterfaceExtractionMetadata>.Empty
                      )
              )
              .Combine(interfaceExtractions)
              .Select((c, _) => c.Left with { InterfaceExtractions = c.Right });

        context.RegisterSourceOutput(
            typesWithAdornments,
            GenerateDecorators
        );
    }

    private void GenerateDecorators(
        SourceProductionContext context,
        GenerationInput input)
    {
        var (mixes, adornments, compilation, interfaceExtractions) = input;
        var matryoshkiCompilation = new MatryoshkiCompilation(compilation);

        foreach (var extractionMetadata in interfaceExtractions)
            GenerateInterfaces(context, extractionMetadata);

        foreach (var adornmentMetadata in adornments)
            matryoshkiCompilation.AddAdornmentMetadata(adornmentMetadata);

        foreach (var mixMetadata in mixes.Distinct())
        {
            try
            {
                GenerateDecorators(context, mixMetadata, interfaceExtractions, matryoshkiCompilation);
            }
            catch (Exception exception)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorRule, mixMetadata.Location, exception));
            }
        }
    }

    private static void GenerateInterfaces(
        SourceProductionContext context,
        InterfaceExtractionMetadata metadata)
    {
        if (metadata.Target is IErrorTypeSymbol)
            return;

        context.CancellationToken.ThrowIfCancellationRequested();

        var interfaceGenerator = new InterfaceGenerator();
        var code = interfaceGenerator.GenerateInterfaceWithAdapter(metadata, context.CancellationToken);

        context.AddSource(
            $"{metadata.Namespace}.{metadata.InterfaceName}.g.cs",
            SourceText.From(code, Encoding.UTF8));
    }

    private static void GenerateDecorators(
        SourceProductionContext context,
        MatryoshkaMetadata metadata,
        ImmutableArray<InterfaceExtractionMetadata> interfaceExtractions,
        MatryoshkiCompilation compilation)
    {
        InterfaceExtractionMetadata? extractedInterface = null;
        if (metadata.Target is IErrorTypeSymbol)
        {
            var metadataCopy = metadata;
            var interfaceExtractionMetadata = interfaceExtractions.FirstOrDefault(
                i => i.Namespace == metadataCopy.SourceNameSpace
                     && i.InterfaceName == metadataCopy.Target.Name);

            if (interfaceExtractionMetadata == null)
                return;

            extractedInterface = interfaceExtractionMetadata;
        }

        if (metadata.Target.IsSealed && extractedInterface is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(SealedTypeRule, metadata.Location));
            return;
        }

        if (metadata.Target.TypeKind != TypeKind.Interface
            && extractedInterface is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(NonInterfaceTypeRule, metadata.Location));
        }

        if (metadata.Nesting is { })
            compilation.AddNestingMetadata(metadata.Nesting, metadata.IsStrictNesting);

        var (adornments, isStrict) = metadata.GetAdornments(compilation);

        for (var i = 0; i < adornments.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var current = adornments[i];
            var next = i + 1 < adornments.Length
                ? adornments[i + 1]
                : (AdornmentMetadata?)null;

            var generationContext = new DecoratorGenerationContext(metadata, current, next, isStrict);

            var decoratorGenerator = new DecoratorGenerator(
                context: generationContext,
                extractedInterface);

            var code = decoratorGenerator.GenerateCompilationUnit(context.CancellationToken);

            context.AddSource(
                $"{generationContext.GetNamespace()}.{generationContext.GetClassName()}.g.cs",
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private record struct GenerationInput
    (
        ImmutableArray<MatryoshkaMetadata> Metadata,
        IEnumerable<AdornmentMetadata> Adornments,
        Compilation Compilation,
        ImmutableArray<InterfaceExtractionMetadata> InterfaceExtractions
    );
}