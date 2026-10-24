namespace HexaSketch.Application;

/// <summary>What happened when a task touched the outside world: success, or every problem found.</summary>
public sealed record Outcome(bool Succeeded, IReadOnlyList<string> Problems)
{
    public static Outcome Ok { get; } = new(true, []);

    public static Outcome Failed(IReadOnlyList<string> problems) => new(false, problems);

    public static Outcome Failed(string problem) => new(false, [problem]);
}
