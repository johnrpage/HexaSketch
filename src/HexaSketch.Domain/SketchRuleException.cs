namespace HexaSketch.Domain;

/// <summary>Thrown when an operation would break one of the sketch's rules.</summary>
public sealed class SketchRuleException(string message) : Exception(message);
