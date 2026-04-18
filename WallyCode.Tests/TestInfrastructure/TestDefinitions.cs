using WallyCode.ConsoleApp.Routing;

namespace WallyCode.Tests.TestInfrastructure;

internal static class TestDefinitions
{
    public static RoutingDefinition TwoUnit()
    {
        var def = new RoutingDefinition
        {
            Name = "test-def",
            StartUnitName = "start",
            Units =
            [
                new LogicalUnit
                {
                    Name = "start",
                    AllowedKeywords = ["[CONTINUE]", "[ASK_USER]", "[NEXT]", "[FAIL]"],
                    Transitions = new(StringComparer.Ordinal) { ["[NEXT]"] = "finish" }
                },
                new LogicalUnit
                {
                    Name = "finish",
                    AllowedKeywords = ["[CONTINUE]", "[DONE]", "[FAIL]"]
                }
            ]
        };
        def.Validate();
        return def;
    }

    public static RoutedSession NewSession(RoutingDefinition definition, string sourcePath) =>
        RoutedSession.Start(definition, "test goal", "mock-provider", "mock-default-model", sourcePath);
}
