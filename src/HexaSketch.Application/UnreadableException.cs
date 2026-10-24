namespace HexaSketch.Application;

/// <summary>Thrown by a driven adapter when what it was asked to read is missing or malformed.</summary>
public sealed class UnreadableException(IReadOnlyList<string> problems)
    : Exception(string.Join(Environment.NewLine, problems))
{
    public UnreadableException(string problem) : this([problem]) { }

    public IReadOnlyList<string> Problems { get; } = problems;
}
