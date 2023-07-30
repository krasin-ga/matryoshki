using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders;

internal class InterfaceGenerator
{
    private readonly SymbolToInterfaceMemberTranslationStrategy _interfaceMemberTranslationStrategy;

    public InterfaceGenerator()
    {
        _interfaceMemberTranslationStrategy
            = new SymbolToInterfaceMemberTranslationStrategy();
    }

    public string GenerateInterfaceWithAdapter(
        InterfaceExtractionMetadata metadata,
        CancellationToken cancellationToken)
    {
        var namedType = metadata.Target;
        var interfaceName = metadata.InterfaceName;
        var nullableEnableDirective = SyntaxFactory.NullableDirectiveTrivia(SyntaxFactory.Token(SyntaxKind.EnableKeyword), true);

        var @interface = SyntaxFactory.InterfaceDeclaration(interfaceName)
                                      .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                      .WithLeadingTrivia(SyntaxFactory.Trivia(nullableEnableDirective));

        foreach (var member in namedType.GetMembersThatCanBeExtractedToInterface())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_interfaceMemberTranslationStrategy.Translate(member) is { } memberDeclarationSyntax)
                @interface = @interface.AddMembers(memberDeclarationSyntax);
        }

        var decoratorGenerator = new AdapterGenerator();

        @interface = @interface.AddMembers(
            decoratorGenerator.GenerateClassDeclarationSyntax(metadata, cancellationToken)
                              .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                                                       SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName(interfaceName)))))
        );

        return SyntaxFactory.CompilationUnit()
                            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")))
                            .AddMembers(
                                metadata.Namespace is { } @ns
                                    ? SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(@ns))
                                                   .AddMembers(@interface)
                                    : @interface)
                            .NormalizeWhitespace()
                            .ToFullString();
    }
}