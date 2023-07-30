using System.Linq.Expressions;
using System.Reflection;

namespace Matryoshki.Abstractions;

public static class Assignment
{
    public static Action<TType, TValue> CreateAssignmentAction<TType, TValue>(
        Expression<Func<TType, TValue>> memberExpression)
    {
        var typeParameter = Expression.Parameter(typeof(TType), "instance");
        var valueParameter = Expression.Parameter(typeof(TValue), "newValue");

        return Expression.Lambda<Action<TType, TValue>>(
            Expression.Assign(
                Expression.Property(
                    typeParameter,
                    (PropertyInfo)((MemberExpression)memberExpression.Body).Member),
                valueParameter),
            typeParameter,
            valueParameter).Compile();
    }
}