using System.Text;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Serialization;
using Matryoshki.Generators.SyntaxRewriters;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators;

[Generator(LanguageNames.CSharp)]
public class CompiledAdornmentSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var coreSymbols = context.CompilationProvider
                                 .Select((c, _) => CoreMatryoshkiSymbols.Create(c));

        var classesWithSymbols =
            context.SyntaxProvider
                   .CreateSyntaxProvider(
                       (node, _) => node.IsAdornmentClassDeclaration(),
                       TransformToClassWithSemanticModel)
                   .Combine(coreSymbols)
                   .Select((d, _) => new AdornmentCompilationInput(
                               d.Left.Class,
                               d.Right,
                               d.Left.SemanticModel));

        context.RegisterSourceOutput(
            classesWithSymbols,
            CreateOutput
        );
    }

    private static void CreateOutput(
        SourceProductionContext context,
        AdornmentCompilationInput input)
    {
        var coreSymbols = input.CoreSymbols;
        if (coreSymbols is null)
            return;

        var semanticModel = input.SemanticModel;

        var classDeclaration = input.Class;
        var declaredSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (declaredSymbol is null)
            return;

        var namesRewriter = new AdornmentRewriter(semanticModel, classDeclaration, context.CancellationToken);
        var processedSyntaxTree = namesRewriter.Visit(classDeclaration.SyntaxTree.GetRoot()).SyntaxTree;
        var compilationUnit = processedSyntaxTree.GetCompilationUnitRoot();
        var encodedString = AdornmentSerializer.Serialize(compilationUnit.ToFullString());

        var fullName = declaredSymbol.GetFullName();

        var attribute = Attribute(
            IdentifierName($"assembly: {CompiledAttributeType.Value}"),
            AttributeArgumentList(
                SeparatedList(
                    new[]
                    {
                        AttributeArgument(fullName.AsStringLiteralExpression()),
                        AttributeArgument(classDeclaration.Identifier.Text.AsStringLiteralExpression()),
                        AttributeArgument(encodedString.AsStringLiteralExpression()),
                    }
                )));

        var newCompilationUnit = CompilationUnit()
            .AddAttributeLists(AttributeList(SeparatedList(new[] { attribute })));

        context.AddSource(
            $"{fullName.GetSafeName()}.Compiled.g.cs",
            SourceText.From(
                newCompilationUnit.NormalizeWhitespace().ToFullString(),
                Encoding.UTF8));
    }

    private static (ClassDeclarationSyntax Class, SemanticModel SemanticModel) TransformToClassWithSemanticModel(
        GeneratorSyntaxContext ctx,
        CancellationToken cancellationToken)
    {
        var node = (ClassDeclarationSyntax)ctx.Node;
        return (node, ctx.SemanticModel);
    }

    private record struct AdornmentCompilationInput
    (
        ClassDeclarationSyntax Class,
        CoreMatryoshkiSymbols? CoreSymbols,
        SemanticModel SemanticModel
    );
}