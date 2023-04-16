using System.Collections.Immutable;
using System.Text;
using Matryoshki.Builders;
using Matryoshki.Models;
using Matryoshki.Pipelines;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Matryoshki;

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
        var compiledAdornments = new CompiledAdornmentsPipeline()
            .Create(context);

        var syntaxAdornments = new SyntaxAdornmentsPipeline()
            .Create(context);

        var combinedAdornments
            = compiledAdornments
              .Collect().Combine(syntaxAdornments.Collect())
              .Select((c, _) => (Compiled: c.Left, Syntax: c.Right));

        var types = new MatryoshkaTypesPipeline()
            .Create(context);

        var typesWithAdornments
            = types
              .Collect()
              .Combine(combinedAdornments)
              .Combine(context.CompilationProvider)
              .Select(
                  (c, _) =>
                      new GenerationInput(
                          Mixes: c.Left.Left,
                          Adornments: c.Left.Right.Syntax.Concat(c.Left.Right.Compiled),
                          Compilation: c.Right
                      )
              );

        context.RegisterSourceOutput(
            typesWithAdornments,
            GenerateDecorators
        );
    }

    private void GenerateDecorators(
        SourceProductionContext context,
        GenerationInput input)
    {
        var (mixes, adornments, compilation) = input;
        var matryoshkiCompilation = new MatryoshkiCompilation(compilation);

        foreach (var adornmentMetadata in adornments)
            matryoshkiCompilation.AddAdornmentMetadata(adornmentMetadata);

        foreach (var mixMetadata in mixes.Distinct())
        {
            try
            {
                GenerateDecorators(context, mixMetadata, matryoshkiCompilation);
            }
            catch (Exception exception)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorRule, mixMetadata.Location, exception));
            }
        }
    }

    private static void GenerateDecorators(
        SourceProductionContext context, 
        MatryoshkaMetadata mixMetadata, 
        MatryoshkiCompilation matryoshkiCompilation)
    {
        if (mixMetadata.Target is IErrorTypeSymbol)
            return;

        if (mixMetadata.Target.IsSealed)
        {
            context.ReportDiagnostic(Diagnostic.Create(SealedTypeRule, mixMetadata.Location));
            return;
        }

        if (mixMetadata.Target.TypeKind != TypeKind.Interface)
        {
            context.ReportDiagnostic(Diagnostic.Create(NonInterfaceTypeRule, mixMetadata.Location));
        }

        if (mixMetadata.Nesting is { })
            matryoshkiCompilation.AddPackMetadata(mixMetadata.Nesting);

        var mixAdornments = mixMetadata.GetAdornments(matryoshkiCompilation);

        for (var i = 0; i < mixAdornments.Length; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var current = mixAdornments[i];
            var next = i + 1 < mixAdornments.Length
                ? mixAdornments[i + 1]
                : (AdornmentMetadata?)null;

            var generationContext = new DecoratorGenerationContext(mixMetadata, current, next);
            var decoratorGenerator = new DecoratorGenerator(generationContext);
            var code = decoratorGenerator.GenerateDecoratorClass(context.CancellationToken);

            context.AddSource(
                $"{generationContext.GetNamespace()}.{generationContext.GetClassName()}.g.cs",
                SourceText.From(code, Encoding.UTF8));
        }
    }

    private record struct GenerationInput
    (
        ImmutableArray<MatryoshkaMetadata> Mixes,
        IEnumerable<AdornmentMetadata> Adornments,
        Compilation Compilation
    );
}