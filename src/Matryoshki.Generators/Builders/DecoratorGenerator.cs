using Matryoshki.Generators.Builders.Methods;
using Matryoshki.Generators.Builders.Properties;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.Builders;

internal class DecoratorGenerator
{
    private readonly DecoratorGenerationContext _context;
    private readonly InterfaceExtractionMetadata? _extractedInterface;

    public DecoratorGenerator(
        DecoratorGenerationContext context,
        InterfaceExtractionMetadata? extractedInterface)
    {
        _context = context;
        _extractedInterface = extractedInterface;
    }

    public string GenerateCompilationUnit(CancellationToken cancellationToken)
    {
        var @class = GenerateClassDeclarationSyntax(cancellationToken);

        var compilationUnit = CompilationUnit();

        var adornmentSyntaxTree = _context.CurrentAdornment.Tree;

        var usingDirectives = adornmentSyntaxTree
                              .GetRoot()
                              .DescendantNodes()
                              .OfType<UsingDirectiveSyntax>();

        var containingNamespace = _context
                                  .CurrentAdornment.Symbol
                                  .ContainingNamespace.GetFullName();

        compilationUnit = compilationUnit
                          .AddUsings(UsingDirective(IdentifierName("System")))
                          .AddUsings(UsingDirective(IdentifierName(containingNamespace)))
                          .AddUsings(usingDirectives.ToArray())
                          .AddMembers(
                              _context.GetNamespace() is { } @ns
                                  ? NamespaceDeclaration(IdentifierName(@ns))
                                      .AddMembers(@class)
                                  : @class);

        return compilationUnit.NormalizeWhitespace().ToFullString();
    }

    public ClassDeclarationSyntax GenerateClassDeclarationSyntax(
        CancellationToken cancellationToken)
    {
        var namedType = _context.MatryoshkaMetadata.Target;
        var className = _context.GetClassName();

        var targetType = namedType.ToTypeSyntax();

        if (_extractedInterface is { Namespace: { } } extractedInterface)
            targetType = IdentifierName(
                $"{extractedInterface.Namespace}.{extractedInterface.InterfaceName}");

        var membersFactory = new TemplateMembersFactory(
                className,
                _context.CurrentAdornment)
            .AddParameter(
                DecoratorType.InnerParameter,
                type: IdentifierName(_context.GetInnerTypeName()));

        var nullableEnableDirective = NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true);

        var @class = ClassDeclaration(className)
                     .AddModifiers(Token(SyntaxKind.PublicKeyword))
                     .AddBaseListTypes(SimpleBaseType(targetType))
                     .AddMembers(membersFactory.GetMembers().ToArray())
                     .WithLeadingTrivia(Trivia(nullableEnableDirective));

        var parameterNamesFieldBuilder = new ParameterNamesFieldBuilder();

        var decoratedMethodBuilder = new DecoratedMethodBuilder(
            parameterNamesFieldBuilder,
            _context.CurrentAdornment);

        var decoratedPropertyBuilder = new DecoratedPropertyBuilder(
            parameterNamesFieldBuilder,
            _context.CurrentAdornment);

        var delegatedEventBuilder = new DelegatedEventBuilder();

        var members = _extractedInterface is { Target: { } target }
            ? target.GetMembersThatCanBeExtractedToInterface()
            : _context.MatryoshkaMetadata.Target.GetMembersThatCanBeDecorated();


        foreach (var member in members)
            @class = member switch
            {
                IMethodSymbol
                    {
                        MethodKind: not (MethodKind.PropertyGet or MethodKind.PropertySet) and not (
                        MethodKind.EventAdd
                        or MethodKind.EventRemove
                        or MethodKind.EventRaise)
                    }
                    method => @class.AddMembers(
                    decoratedMethodBuilder.GenerateDecoratedMethod(method, cancellationToken)),

                IPropertySymbol property => @class.AddMembers(
                    decoratedPropertyBuilder.GenerateDecoratedProperty(property, cancellationToken)),

                IEventSymbol @event => @class.AddMembers(
                    delegatedEventBuilder.GenerateEventHandler(@event)
                ),

                _ => @class
            };
        return @class;
    }
}

public interface ISymbolTranslationStrategy<out T>
{
    T? CreateFromMethodSymbol(IMethodSymbol methodSymbol);
    T? CreateFromPropertySymbol(IPropertySymbol propertySymbol);
    T? CreateFromFieldSymbol(IFieldSymbol fieldSymbol);
    T? CreateFromIndexerSymbol(IPropertySymbol indexerSymbol);
    T? CreateFromEventSymbol(IEventSymbol eventSymbol);
}

public static class SymbolTranslationStrategyExtensions
{
    public static T? Translate<T>(this ISymbolTranslationStrategy<T> translationStrategy, ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol methodSymbol
                => translationStrategy.CreateFromMethodSymbol(methodSymbol),

            IPropertySymbol { IsIndexer: true } propertySymbol
                => translationStrategy.CreateFromIndexerSymbol(propertySymbol),

            IPropertySymbol propertySymbol
                => translationStrategy.CreateFromPropertySymbol(propertySymbol),

            IFieldSymbol fieldSymbol
                => translationStrategy.CreateFromFieldSymbol(fieldSymbol),

            IEventSymbol eventSymbol
                => translationStrategy.CreateFromEventSymbol(eventSymbol),

            _ => default
        };
    }
}

public class SymbolToInterfaceMemberTranslationStrategy : ISymbolTranslationStrategy<MemberDeclarationSyntax>
{
    public MemberDeclarationSyntax? CreateFromMethodSymbol(IMethodSymbol methodSymbol)
    {
        return methodSymbol
               .ToMethodDeclarationSyntax(
                   Enumerable.Empty<SyntaxToken>(),
                   renameParameters: false)
               .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    public MemberDeclarationSyntax? CreateFromPropertySymbol(IPropertySymbol propertySymbol)
    {
        return propertySymbol.ToPropertyDeclarationSyntax(
            Enumerable.Empty<SyntaxToken>());
    }

    public MemberDeclarationSyntax? CreateFromFieldSymbol(IFieldSymbol fieldSymbol)
    {
        //TODO: Translate to property?
        return null;
    }

    public MemberDeclarationSyntax? CreateFromIndexerSymbol(IPropertySymbol indexerSymbol)
    {
        return indexerSymbol.ToIndexerDeclarationSyntax(
            Enumerable.Empty<SyntaxToken>(),
            renameIndexerParameters: false);
    }

    public MemberDeclarationSyntax? CreateFromEventSymbol(IEventSymbol eventSymbol)
    {
        return eventSymbol.ToEventDeclarationSyntax();
    }
}

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
        var nullableEnableDirective = NullableDirectiveTrivia(Token(SyntaxKind.EnableKeyword), true);

        var @interface = InterfaceDeclaration(interfaceName)
                         .AddModifiers(Token(SyntaxKind.PublicKeyword))
                         .WithLeadingTrivia(Trivia(nullableEnableDirective));

        foreach (var member in namedType.GetMembersThatCanBeExtractedToInterface())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_interfaceMemberTranslationStrategy.Translate(member) is { } memberDeclarationSyntax)
                @interface = @interface.AddMembers(memberDeclarationSyntax);
        }

        var decoratorGenerator = new AdapterGenerator();

        @interface = @interface.AddMembers(
            decoratorGenerator.GenerateClassDeclarationSyntax(metadata, cancellationToken)
                              .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
                                                         SimpleBaseType(IdentifierName(interfaceName)))))
        );

        return CompilationUnit()
               .AddUsings(UsingDirective(IdentifierName("System")))
               .AddMembers(
                   metadata.Namespace is { } @ns
                       ? NamespaceDeclaration(IdentifierName(@ns))
                           .AddMembers(@interface)
                       : @interface)
               .NormalizeWhitespace()
               .ToFullString();
    }
}