// Copilot - GPT-5.5

using System.Reflection;
using UICatalog.Scenarios;

namespace UICatalogTests.Scenarios;

public class ImagesScenarioNullabilityTests
{
    [Fact]
    public void RasterSupportState_AllowsDetectingNullState ()
    {
        NullabilityInfoContext context = new ();
        Type imagesType = typeof (Images);

        AssertFieldIsNullable (context, imagesType, "_sixelSupportResult");
        AssertFieldIsNullable (context, imagesType, "_kittyGraphicsSupportResult");
        AssertParameterIsNullable (context, imagesType, "UpdateRasterSupportState", "sixelResult");
        AssertParameterIsNullable (context, imagesType, "UpdateRasterSupportState", "kittyResult");
        AssertEventArgsValueIsNullable (context, imagesType, "Driver_SixelSupportChanged", "e");
        AssertEventArgsValueIsNullable (context, imagesType, "Driver_KittyGraphicsSupportChanged", "e");
    }

    private static void AssertEventArgsValueIsNullable (NullabilityInfoContext context, Type type, string methodName, string parameterName)
    {
        MethodInfo method = GetNonPublicInstanceMethod (type, methodName);
        ParameterInfo parameter = method.GetParameters ().Single (p => p.Name == parameterName);
        NullabilityInfo nullability = context.Create (parameter);

        Assert.Equal (NullabilityState.Nullable, Assert.Single (nullability.GenericTypeArguments).ReadState);
    }

    private static void AssertFieldIsNullable (NullabilityInfoContext context, Type type, string fieldName)
    {
        FieldInfo field = type.GetField (fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                          ?? throw new InvalidOperationException ($"Field {fieldName} was not found.");

        Assert.Equal (NullabilityState.Nullable, context.Create (field).ReadState);
    }

    private static void AssertParameterIsNullable (NullabilityInfoContext context, Type type, string methodName, string parameterName)
    {
        MethodInfo method = GetNonPublicInstanceMethod (type, methodName);
        ParameterInfo parameter = method.GetParameters ().Single (p => p.Name == parameterName);

        Assert.Equal (NullabilityState.Nullable, context.Create (parameter).ReadState);
    }

    private static MethodInfo GetNonPublicInstanceMethod (Type type, string methodName) =>
        type.GetMethod (methodName, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException ($"Method {methodName} was not found.");
}
