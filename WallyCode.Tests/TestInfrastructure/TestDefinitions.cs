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
                    KeywordOptions =
                    [
                        new() { Keyword = "[CONTINUE]", Description = "Keep working in the same unit." },
                        new() { Keyword = "[ASK_USER]", Description = "Pause and ask the user for input." },
                        new() { Keyword = "[NEXT]", Description = "Move to the finish unit." },
                        new() { Keyword = "[FAIL]", Description = "Stop because the flow cannot continue." }
                    ],
                    Transitions = new(StringComparer.Ordinal) { ["[NEXT]"] = "finish" }
                },
                new LogicalUnit
                {
                    Name = "finish",
                    AllowedKeywords = ["[CONTINUE]", "[DONE]", "[FAIL]"],
                    KeywordOptions =
                    [
                        new() { Keyword = "[CONTINUE]", Description = "Keep working in the finish unit." },
                        new() { Keyword = "[DONE]", Description = "Complete the flow." },
                        new() { Keyword = "[FAIL]", Description = "Stop because the flow cannot continue." }
                    ]
                }
            ]
        };
        def.Validate();
        return def;
    }

    public static RoutedSession NewSession(RoutingDefinition definition, string sourcePath) =>
        RoutedSession.Start(definition, "test goal", "mock-provider", "mock-default-model", sourcePath);
}
