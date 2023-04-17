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

    public DecoratorGenerator(DecoratorGenerationContext context)
    {
        _context = context;
    }

    public string GenerateDecoratorClass(CancellationToken cancellationToken)
    {
        var namedType = _context.MatryoshkaMetadata.Target;
        var className = _context.GetClassName();

        var targetType = namedType.ToTypeSyntax();

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
        var decoratedMethodBuilder = new DecoratedMethodBuilder(parameterNamesFieldBuilder, _context.CurrentAdornment);
        var decoratedPropertyBuilder = new DecoratedPropertyBuilder(parameterNamesFieldBuilder, _context.CurrentAdornment);
        var delegatedEventBuilder = new DelegatedEventBuilder();

        foreach (var member in namedType.GetMembersThatCanBeDecorated())
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
                    decoratedMethodBuilder.GenerateDecoratedMethodWithHelperFields(method, cancellationToken)),

                IPropertySymbol property => @class.AddMembers(
                    decoratedPropertyBuilder.GenerateDecoratedProperty(property, cancellationToken)),

                IEventSymbol @event => @class.AddMembers(
                    delegatedEventBuilder.GenerateEventHandler(@event)
                ),

                _ => @class
            };

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

        var str = compilationUnit.NormalizeWhitespace().ToFullString();

        return str;
    }

}