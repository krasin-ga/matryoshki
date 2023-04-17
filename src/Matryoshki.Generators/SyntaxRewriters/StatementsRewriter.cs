using Matryoshki.Generators.Builders;
using Matryoshki.Generators.Extensions;
using Matryoshki.Generators.Models;
using Matryoshki.Generators.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Matryoshki.Generators.SyntaxRewriters;

internal class StatementsRewriter : CSharpSyntaxRewriter
{
    private readonly ITypeSymbol? _actualReturnType;
    private readonly MethodTemplate _bodyTemplate;
    private readonly CancellationToken _cancellationToken;
    private readonly ISymbol _decoratedSymbol;
    private readonly bool _isAsync;
    private readonly bool _isSetter;
    private readonly ExpressionSyntax _nextInvocationExpression;
    private readonly ParameterNamesFieldBuilder _parameterNamesFieldBuilder;
    private readonly IEnumerable<IParameterSymbol> _parameters;
    private readonly bool _returnsNothing;
    private readonly ITypeSymbol _returnType;
    private bool _break;

    public StatementsRewriter(
        MethodTemplate bodyTemplate,
        ExpressionSyntax nextInvocationExpression,
        IEnumerable<IParameterSymbol> parameters,
        ISymbol decoratedSymbol,
        ITypeSymbol returnType,
        ParameterNamesFieldBuilder parameterNamesFieldBuilder,
        bool returnsNothing,
        bool isAsync,
        bool isSetter,
        CancellationToken cancellationToken)
    {
        _nextInvocationExpression = nextInvocationExpression;

        _bodyTemplate = bodyTemplate;
        _parameters = parameters;
        _decoratedSymbol = decoratedSymbol;
        _returnType = returnType;
        _parameterNamesFieldBuilder = parameterNamesFieldBuilder;
        _returnsNothing = returnsNothing;
        _isAsync = isAsync;
        _isSetter = isSetter;
        _cancellationToken = cancellationToken;

        _actualReturnType = _returnType;

        if (_isAsync && _returnType is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol)
            _actualReturnType = namedTypeSymbol.TypeArguments.First();

        if (_returnsNothing)
            _actualReturnType = null;
    }

    public BlockSyntax CreateBody()
    {
        var visited = (MethodDeclarationSyntax)Visit(_bodyTemplate.Syntax)!;

        var updatedBody = visited.Body;
        if (updatedBody is null && visited.ExpressionBody is { } expression)
            updatedBody = Block(ExpressionStatement(expression.Expression));

        updatedBody ??= Block();

        return updatedBody;
    }

    public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var isRootStatement = node.Parent?.Parent?.IsEquivalentTo(_bodyTemplate.Syntax) is true;

        var visited = base.VisitIfStatement(node);

        if (visited is not IfStatementSyntax ifStatement)
            return visited;

        if (ifStatement.Condition is not LiteralExpressionSyntax literal)
            return visited;

        if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            _break = isRootStatement
                     && ifStatement.Statement.DescendantNodesAndSelf(_ => false).LastOrDefault() is ReturnStatementSyntax;
            return ifStatement.Statement;
        }

        if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
            return ifStatement.Else?.Statement;

        return visited;
    }

    public override SyntaxNode? Visit(SyntaxNode? node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (_break && node is StatementSyntax)
            return null;

        return base.Visit(node);
    }

    public override SyntaxNode? VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (TryReplaceTypeOfEqualityExpression(node, out var replaced))
            return replaced;

        var visited = base.VisitBinaryExpression(node);

        if (visited is not BinaryExpressionSyntax binaryExpression)
            return visited;

        node = binaryExpression;

        if (node.OperatorToken.IsKind(SyntaxKind.BarBarToken))
        {
            if (node.Left.IsKind(SyntaxKind.FalseLiteralExpression)
                && node.Right.IsKind(SyntaxKind.FalseLiteralExpression))
                return LiteralExpression(SyntaxKind.FalseLiteralExpression);

            if (node.Left.IsKind(SyntaxKind.TrueLiteralExpression)
                || node.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                return LiteralExpression(SyntaxKind.TrueLiteralExpression);
        }

        if (node.OperatorToken.IsKind(SyntaxKind.AmpersandAmpersandToken))
        {
            if (node.Left.IsKind(SyntaxKind.FalseLiteralExpression)
                || node.Right.IsKind(SyntaxKind.FalseLiteralExpression))
                return LiteralExpression(SyntaxKind.FalseLiteralExpression);

            if (node.Left.IsKind(SyntaxKind.TrueLiteralExpression)
                && node.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                return LiteralExpression(SyntaxKind.TrueLiteralExpression);
        }

        return node;
    }

    /// <summary>
    /// Replaces typeof(A) == typeof(B) with boolean literal.
    /// </summary>
    private bool TryReplaceTypeOfEqualityExpression(BinaryExpressionSyntax node, out SyntaxNode? result)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        result = null;

        if (!(node.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken)
              || node.OperatorToken.IsKind(SyntaxKind.AmpersandEqualsToken)))
            return false;

        var isEqualsOperator = node.OperatorToken.IsKind(SyntaxKind.EqualsEqualsToken);

        if (node is { Left: TypeOfExpressionSyntax left, Right: TypeOfExpressionSyntax right })
        {
            var leftType = _bodyTemplate.SemanticModel.GetTypeInfo(left.Type).Type;
            var rightType = _bodyTemplate.SemanticModel.GetTypeInfo(right.Type).Type;

            if (left.Type is IdentifierNameSyntax leftIdentifier
                && leftIdentifier.Identifier.IsEquivalentTo(_bodyTemplate.TypeParameterIdentifier))
                leftType = _actualReturnType;

            if (right.Type is IdentifierNameSyntax rightIdentifier
                && rightIdentifier.Identifier.IsEquivalentTo(_bodyTemplate.TypeParameterIdentifier))
                rightType = _actualReturnType;

            var typesAreEqual = SymbolEqualityComparer.Default.Equals(leftType, rightType);
            result = LiteralExpression(isEqualsOperator == typesAreEqual
                                           ? SyntaxKind.TrueLiteralExpression
                                           : SyntaxKind.FalseLiteralExpression);

            return true;
        }

        return false;
    }

    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax @return)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var visited = base.VisitReturnStatement(@return);
        if (visited is not ReturnStatementSyntax updatedReturn)
            return visited;

        if (!_returnsNothing)
            return visited;

        return updatedReturn.Expression switch
        {
            IdentifierNameSyntax => ReturnStatement(),
            { } => Block(
                updatedReturn.Expression.CanBeUsedAsStatement()
                    ? ExpressionStatement(updatedReturn.Expression)
                    : ExpressionStatement(updatedReturn.Expression.ToDiscardVariable()),
                ReturnStatement()),
            _ => @return
        };
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var visited = base.VisitMemberAccessExpression(node);
        if (visited is not MemberAccessExpressionSyntax memberAccess)
            return visited;

        return memberAccess.Name.Identifier.Text switch
        {
            CallType.Properties.MemberName
                => _decoratedSymbol.Name.AsStringLiteralExpression(),

            CallType.Properties.IsProperty
                => LiteralExpression(_decoratedSymbol is IPropertySymbol
                                         ? SyntaxKind.TrueLiteralExpression
                                         : SyntaxKind.FalseLiteralExpression),
            CallType.Properties.IsMethod
                => LiteralExpression(_decoratedSymbol is IMethodSymbol
                                         ? SyntaxKind.TrueLiteralExpression
                                         : SyntaxKind.FalseLiteralExpression),

            CallType.Properties.IsGetter
                => LiteralExpression(_decoratedSymbol is IPropertySymbol && !_isSetter
                                         ? SyntaxKind.TrueLiteralExpression
                                         : SyntaxKind.FalseLiteralExpression),

            CallType.Properties.IsSetter
                => LiteralExpression(_decoratedSymbol is IPropertySymbol && _isSetter
                                         ? SyntaxKind.TrueLiteralExpression
                                         : SyntaxKind.FalseLiteralExpression),
            _ => visited
        };
    }

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var visited = base.VisitInvocationExpression(node);
        if (visited is not InvocationExpressionSyntax invocation)
            return visited;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return visited;

        if (memberAccess is not { Expression: SimpleNameSyntax identifierName })
            if (memberAccess is { Expression: MemberAccessExpressionSyntax innerMemberAccess })
                identifierName = innerMemberAccess.Name;
            else
                return visited;

        var identifierText = identifierName.Identifier.Text;

        if (!identifierText.Equals(_bodyTemplate.ParameterIdentifier.Text)
            && !identifierText.Equals(PretenseType.TypeName)
            && memberAccess.Name is not GenericNameSyntax { Identifier.Text: PretenseType.Methods.Pretend }
           )
            return visited;

        switch (memberAccess.Name.Identifier.Text)
        {
            case CallType.Methods.DynamicForward:
            case CallType.Methods.ForwardAsync:
            case CallType.Methods.Forward:
                return _nextInvocationExpression;

            case CallType.Methods.GetSetterValue:
                if (!_isSetter)
                {
                    return _returnsNothing
                        ? NothingType.Instance
                        : DefaultExpression(_returnType.ToTypeSyntax());
                }

                return IdentifierName("value");
            case CallType.Methods.Pass:
                return invocation.ArgumentList.Arguments.First().Expression;
            case PretenseType.Methods.Pretend:
                return invocation.ArgumentList.Arguments.Count > 0
                    ? invocation.ArgumentList.Arguments[0].Expression
                    : memberAccess.Expression;

            case CallType.Methods.GetArgumentsOfType:
            case CallType.Methods.GetArgumentsValuesOfType:
            case CallType.Methods.GetFirstArgumentOfType:
            case CallType.Methods.GetFirstArgumentValueOfType:
                return ProcessArguments(
                    invocation,
                    memberAccess.Name.Identifier.Text);
            case CallType.Methods.GetParameterNames:
                return IdentifierName(
                    _parameterNamesFieldBuilder.GetParameterNamesArrayHelperFieldIdentifier(
                        _decoratedSymbol));
        }

        return visited;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!node.Identifier.IsEquivalentTo(_bodyTemplate.TypeParameterIdentifier))
            return base.VisitIdentifierName(node);

        return _actualReturnType is null
            ? NothingType.IdentifierName
            : IdentifierName(_actualReturnType.GetFullName());
    }

    private SyntaxNode ProcessArguments(
        InvocationExpressionSyntax invocationExpression,
        string methodName)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var genericArgument = invocationExpression
                              .DescendantNodes().OfType<GenericNameSyntax>()
                              .First()
                              .TypeArgumentList.Arguments.Single();

        var type = _bodyTemplate.SemanticModel.GetTypeInfo(genericArgument).ConvertedType;
        if (type is null)
            throw new InvalidOperationException($"Cannot resolve type: `{invocationExpression.ToFullString()}`");

        var suitableArguments = new List<ExpressionSyntax>();
        var onlyFirst = methodName is CallType.Methods.GetFirstArgumentOfType
            or CallType.Methods.GetFirstArgumentValueOfType;

        var needArgument = methodName is CallType.Methods.GetArgumentsOfType
            or CallType.Methods.GetFirstArgumentOfType;

        //T
        var targetArgumentValueType = type.ToTypeSyntax();

        //Argument<T>
        var argumentType = ArgumentType.Of(targetArgumentValueType);

        foreach (var parameterSymbol in _parameters)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (!type.IsAssignableFrom(parameterSymbol.Type))
                continue;

            ExpressionSyntax expressionSyntax =
                needArgument
                    ? argumentType.CreateNew(
                        parameterSymbol.Name.AsStringLiteralExpression(),
                        parameterSymbol.Name.ToMatryoshkiIdentifierName())
                    : parameterSymbol.Name.ToMatryoshkiIdentifierName();

            suitableArguments.Add(expressionSyntax);

            if (onlyFirst)
                break;
        }

        if (onlyFirst)
            return FirstArgumentOrDefault(
                suitableArguments,
                needArgument,
                argumentType,
                targetArgumentValueType);

        var elementType = needArgument
            ? argumentType
            : targetArgumentValueType;

        return elementType.InitializedArray(suitableArguments);
    }

    private static ExpressionSyntax FirstArgumentOrDefault(
        List<ExpressionSyntax> suitableArguments,
        bool needArgument,
        GenericNameSyntax argumentType,
        TypeSyntax targetArgumentValueType)
    {
        if (suitableArguments.Count > 0)
            return suitableArguments[0];

        if (needArgument)
            return DefaultExpression(argumentType);

        return DefaultExpression(targetArgumentValueType);
    }
}