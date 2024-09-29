using System.Collections.Immutable;
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
            .AddAttributeLists(
                _context.CurrentAdornment.ClassDeclaration.AttributeLists.ToArray()
            )
            .WithLeadingTrivia(Trivia(nullableEnableDirective));

        var parameterNamesFieldBuilder = new ParameterNamesFieldBuilder();

        var decoratedMethodBuilder = new DecoratedMethodBuilder(
            parameterNamesFieldBuilder,
            _context.CurrentAdornment);

        var decoratedPropertyBuilder = new DecoratedPropertyBuilder(
            parameterNamesFieldBuilder,
            _context.CurrentAdornment,
            targetType);

        var delegatedEventBuilder = new DelegatedEventBuilder();

        var members = _extractedInterface is { Target: { } target }
            ? target.GetMembersThatCanBeExtractedToInterface()
            : _context.MatryoshkaMetadata.Target.GetMembersThatCanBeDecorated();

        var decoratedMethods = new Dictionary<string, List<ImmutableArray<IParameterSymbol>>>();
        var decoratedProperties = new Dictionary<string, List<ImmutableArray<IParameterSymbol>>>();

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
                    GenerateDecoratedMethod(
                        decoratedMethodBuilder,
                        method,
                        decoratedMethods,
                        cancellationToken)
                ),

                IPropertySymbol property => @class.AddMembers(
                    GenerateDecoratedProperty(decoratedPropertyBuilder, property, decoratedProperties, cancellationToken)
                    ),

                IEventSymbol @event => @class.AddMembers(
                    delegatedEventBuilder.GenerateEventHandler(@event)
                ),

                _ => @class
            };
        return @class;
    }

    private static MemberDeclarationSyntax[] GenerateDecoratedProperty(
        DecoratedPropertyBuilder decoratedPropertyBuilder, 
        IPropertySymbol property,
        Dictionary<string, List<ImmutableArray<IParameterSymbol>>> decoratedProperties,
        CancellationToken cancellationToken)
    {
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax = null;

        if (!decoratedProperties.TryGetValue(property.Name, out var parameterVariations))
            decoratedProperties[property.Name] = [property.Parameters];
        else
        {
            if (parameterVariations.Any(p => p.SequenceEqual(property.Parameters, ParameterSymbolEqualityComparer.Instance))
                && property.ContainingType is { })
                explicitInterfaceSpecifierSyntax = ExplicitInterfaceSpecifier(
                    IdentifierName(property.ContainingType.GetFullName()),
                    Token(SyntaxKind.DotToken));
            else
                parameterVariations.Add(property.Parameters);
        }

        return decoratedPropertyBuilder.GenerateDecoratedProperty(
            property, 
            explicitInterfaceSpecifierSyntax, 
            cancellationToken);
    }

    private static MemberDeclarationSyntax[] GenerateDecoratedMethod(
        DecoratedMethodBuilder decoratedMethodBuilder,
        IMethodSymbol method,
        Dictionary<string, List<ImmutableArray<IParameterSymbol>>> decoratedMethods,
        CancellationToken cancellationToken)
    {
        ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifierSyntax = null;

        if (!decoratedMethods.TryGetValue(method.Name, out var parameterVariations))
            decoratedMethods[method.Name] = [method.Parameters];
        else
        {
            if (parameterVariations.Any(p => p.SequenceEqual(method.Parameters, ParameterSymbolEqualityComparer.Instance))
                && method.ContainingType is { })
                explicitInterfaceSpecifierSyntax = ExplicitInterfaceSpecifier(
                    IdentifierName(method.ContainingType.GetFullName()),
                    Token(SyntaxKind.DotToken));
            else
                parameterVariations.Add(method.Parameters);
        }

        return decoratedMethodBuilder.GenerateDecoratedMethod(
            method,
            explicitInterfaceSpecifierSyntax,
            cancellationToken);
    }

    public class ParameterSymbolEqualityComparer : IEqualityComparer<IParameterSymbol>
    {
        public static readonly ParameterSymbolEqualityComparer Instance = new();

        public bool Equals(IParameterSymbol x, IParameterSymbol y)
        {
            return SymbolEqualityComparer.Default.Equals(x, y);
        }

        public int GetHashCode(IParameterSymbol obj)
        {
            return SymbolEqualityComparer.Default.GetHashCode(obj);
        }
    }
}