namespace Matryoshki.Types;

internal static class CallType
{
    public const string TypeName = "Call";
    public const string GenericTypeName = "Matryoshki.Abstractions.Call`1";

    public static class Properties
    {
        public const string MemberName = nameof(MemberName);
        public const string IsProperty = nameof(IsProperty);
        public const string IsMethod = nameof(IsMethod);

        public const string IsGetter = nameof(IsGetter);
        public const string IsSetter = nameof(IsSetter);
    }

    public static class Methods
    {
        public const string GetSetterValue = nameof(GetSetterValue);

        public const string Forward = nameof(Forward);
        public const string ForwardAsync = nameof(ForwardAsync);
        public const string DynamicForward = nameof(DynamicForward);

        public const string Pass = nameof(Pass);

        public const string GetParameterNames = nameof(GetParameterNames);

        public const string GetArgumentsOfType = nameof(GetArgumentsOfType);
        public const string GetArgumentsValuesOfType = nameof(GetArgumentsValuesOfType);

        public const string GetFirstArgumentOfType = nameof(GetFirstArgumentOfType);
        public const string GetFirstArgumentValueOfType = nameof(GetFirstArgumentValueOfType);
    }
}