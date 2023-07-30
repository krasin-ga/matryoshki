using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Matryoshki.Generators.Types;

internal static class NothingType
{
    public static readonly IdentifierNameSyntax IdentifierName
        = SyntaxFactory.IdentifierName("Matryoshki.Abstractions.Nothing");

    public static readonly TypeSyntax ValueTask
        = SyntaxFactory.ParseTypeName("ValueTask<Matryoshki.Abstractions.Nothing>");

    public static readonly ExpressionSyntax Instance =
        SyntaxFactory.ParseExpression("Matryoshki.Abstractions.Nothing.Instance");

    public static InvocationExpressionSyntax FromPropertyAction(
        IdentifierNameSyntax instance,
        IdentifierNameSyntax propertyName,
        IdentifierNameSyntax value)
    {
        var instanceIdentifier = SyntaxFactory.Identifier("@innerΔΔΔ");
        var valueIdentifier = SyntaxFactory.Identifier("@valueΔΔΔ");

        var lambda =
            SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Parameter(instanceIdentifier),
                            SyntaxFactory.Parameter(valueIdentifier)
                        })),
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(instanceIdentifier),
                        propertyName),
                    SyntaxFactory.IdentifierName(valueIdentifier)
                )
            ).WithModifiers(
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            );

        var methodExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName,
            SyntaxFactory.IdentifierName("FromPropertyAction"));

        return SyntaxFactory.InvocationExpression(
            methodExpression,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    new[]
                    {
                        SyntaxFactory.Argument(instance),
                        SyntaxFactory.Argument(value),
                        SyntaxFactory.Argument(lambda)
                    })
            ));
    }

    public static InvocationExpressionSyntax FromInitOnlyPropertyAction(
        IdentifierNameSyntax instance,
        string fieldName,
        IdentifierNameSyntax value)
    {
        var instanceIdentifier = SyntaxFactory.Identifier("@innerΔΔΔ");
        var valueIdentifier = SyntaxFactory.Identifier("@valueΔΔΔ");

        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
            parameterList: SyntaxFactory.ParameterList(
                SyntaxFactory.SeparatedList(
                    new[]
                    {
                        SyntaxFactory.Parameter(instanceIdentifier),
                        SyntaxFactory.Parameter(valueIdentifier)
                    })),
            body: SyntaxFactory.InvocationExpression(
                SyntaxFactory.IdentifierName(fieldName),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(instanceIdentifier)),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(valueIdentifier))
                        })
                )
            )
        ).WithModifiers(
            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
        );

        var methodExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName,
            SyntaxFactory.IdentifierName("FromPropertyAction"));

        return SyntaxFactory.InvocationExpression(
            methodExpression,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    new[]
                    {
                        SyntaxFactory.Argument(instance),
                        SyntaxFactory.Argument(value),
                        SyntaxFactory.Argument(lambda)
                    })
            ));
    }

    public static InvocationExpressionSyntax FromIndexerAction(
        IdentifierNameSyntax instance,
        IdentifierNameSyntax value,
        params IdentifierNameSyntax[] keys)
    {
        var instanceIdentifier = SyntaxFactory.Identifier("@innerΔΔΔ");
        var keyIdentifier = SyntaxFactory.Identifier("@keyΔΔΔ");
        var keyIdentifierName = SyntaxFactory.IdentifierName(keyIdentifier);
        var valueIdentifier = SyntaxFactory.Identifier("@valueΔΔΔ");

        var keyArguments = SyntaxFactory.SeparatedList(keys.Select(k => SyntaxFactory.Argument(k)).ToArray());

        ExpressionSyntax keysArg = keys.Length == 1
            ? keys.First()
            : SyntaxFactory.TupleExpression(keyArguments);

        var lambda =
            SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Parameter(instanceIdentifier),
                            SyntaxFactory.Parameter(keyIdentifier),
                            SyntaxFactory.Parameter(valueIdentifier)
                        })),
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(instanceIdentifier))
                                 .WithArgumentList(
                                     SyntaxFactory.BracketedArgumentList(
                                         keys.Length > 1
                                             ? SyntaxFactory.SeparatedList(
                                                 keys.Select(
                                                     k => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                                                                                     SyntaxKind.SimpleMemberAccessExpression,
                                                                                     keyIdentifierName,
                                                                                     k)))
                                             )
                                             : SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(keyIdentifier)))
                                     )),
                    SyntaxFactory.IdentifierName(valueIdentifier)
                )
            ).WithModifiers(
                SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword))
            );

        var methodExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName,
            SyntaxFactory.IdentifierName("FromIndexerAction"));

        return SyntaxFactory.InvocationExpression(
            methodExpression,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[] { SyntaxFactory.Argument(instance), SyntaxFactory.Argument(keysArg), SyntaxFactory.Argument(value), SyntaxFactory.Argument(lambda) })
            ));
    }
}