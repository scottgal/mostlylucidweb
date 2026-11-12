namespace AdaptiveAllocation;

public static class DemoFixture
{
    public static DateTimeOffset Now => new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    public static WorkRequest Work() => new("conversation", "extract-json", "context-v1", "local");

    public static SupplierSnapshot[] Suppliers(bool retainContext = true) =>
    [
        new(new ModelSupplier("local", ExecutionMode.Local, "local-tokenizer-v1", Tasks(),
                16_000, 32_000, 10_000, 4, 3, 3, 100, 10, [1, 0]),
            retainContext ? [new ContextLease("conversation", "context-v1", "local-tokenizer-v1", 8_000,
                Now.AddMinutes(5))] : []),
        new(new ModelSupplier("online", ExecutionMode.Online, "online-tokenizer-v1", Tasks(),
            16_000, 32_000, 10_000, 4, 1, 2, 50, 10, [0, 1]), [])
    ];

    public static TokenQuote[] Quotes(int historyTokens = 8_000) =>
    [
        new("conversation", "local", "local-tokenizer-v1", 1_000, historyTokens, 1_000),
        new("conversation", "online", "online-tokenizer-v1", 1_000, historyTokens, 1_000)
    ];

    public static OutcomeMemory Outcomes()
    {
        var memory = new OutcomeMemory();
        foreach (var supplier in new[] { "local", "online" })
        for (var index = 0; index < 5; index++)
            memory.Observe(new($"{supplier}-completed-{index}", "extract-json", supplier, true));
        return memory;
    }

    public static HashSet<string> Tasks() => new(["extract-json"], StringComparer.Ordinal);
}
