using Matryoshki.Generators.Builders.Methods;
using Matryoshki.Generators.Builders.Properties;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Pipelines;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Builders;

internal class AdapterGenerator
{
    public ClassDeclarationSyntax GenerateClassDeclarationSyntax(
        InterfaceExtractionMetadata interfaceExtractionMetadata,
        CancellationToken cancellationToken)
    {
        var targetType = interfaceExtractionMetadata.Target.ToTypeSyntax();
        var className = "Adapter";

        var membersFactory = new TemplateMembersFactory(
                className,
                PassthroughAdornment.AdornmentMetadata)
            .AddParameter(
                DecoratorType.InnerParameter,
                type: targetType);

        var @class = SyntaxFactory.ClassDeclaration(className)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceExtractionMetadata.InterfaceName)))
            .AddMembers(membersFactory.GetMembers().ToArray());

        var decoratedMethodBuilder = new AdapterMethodBuilder();

        var propertyBuilder = new AdapterPropertyBuilder();
        var delegatedEventBuilder = new DelegatedEventBuilder();

        foreach (var member in interfaceExtractionMetadata.Target.GetMembersThatCanBeExtractedToInterface())
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
                    decoratedMethodBuilder.GenerateDecoratedMethod(
                        method,
                        explicitInterfaceSpecifierSyntax: null,
                        cancellationToken)),

                IPropertySymbol property => @class.AddMembers(
                    propertyBuilder.GenerateDecoratedProperty(
                        property,
                        explicitInterfaceSpecifierSyntax: null!,
                        cancellationToken)),

                IEventSymbol @event => @class.AddMembers(
                    delegatedEventBuilder.GenerateEventHandler(@event)
                ),

                _ => @class
            };

        return @class;
    }
}