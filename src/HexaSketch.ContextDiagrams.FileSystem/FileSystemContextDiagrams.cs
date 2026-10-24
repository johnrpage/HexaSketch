using System.Text.Json;
using HexaSketch.Application;
using HexaSketch.Domain;

namespace HexaSketch.ContextDiagrams.FileSystem;

/// <summary>ContextDiagrams adapter. Reads Context Sketch's .contextsketch.json files.</summary>
public sealed class FileSystemContextDiagrams : HexaSketch.Application.ContextDiagrams
{
    public ContextDiagram Load(string location)
    {
        string json;
        try
        {
            json = File.ReadAllText(location);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new UnreadableException($"Can't read '{location}': {e.Message}");
        }
        return ContextSketchJson.Parse(json);
    }
}

/// <summary>Context Sketch's file format (schema version 1).</summary>
internal static class ContextSketchJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static ContextDiagram Parse(string json)
    {
        FileDto? file;
        try
        {
            file = JsonSerializer.Deserialize<FileDto>(json, Options);
        }
        catch (JsonException e)
        {
            throw new UnreadableException($"This isn't a valid context diagram file: {e.Message}");
        }
        if (file is null) throw new UnreadableException("The file is empty.");

        var problems = new List<string>();
        if (file.SchemaVersion != 1)
            problems.Add($"Schema version {file.SchemaVersion} isn't supported (expected 1).");

        var elements = new List<ContextElement>();
        foreach (var e in file.Elements ?? [])
        {
            var type = e.Type switch
            {
                "system" => ElementType.System,
                "primaryActor" => ElementType.PrimaryActor,
                "secondaryActor" => ElementType.SecondaryActor,
                "externalSystem" => ElementType.ExternalSystem,
                "storage" => ElementType.Storage,
                _ => (ElementType?)null,
            };
            if (type is null) problems.Add($"Element '{e.Id}' has an unknown type '{e.Type}'.");
            if (string.IsNullOrWhiteSpace(e.Id)) problems.Add($"An element ('{e.Name}') has no id.");
            else if (type is not null)
                elements.Add(new ContextElement(e.Id, type.Value, e.Name ?? "", e.Description, e.Technology, e.X, e.Y, e.Pinned));
        }

        var relationships = new List<ContextRelationship>();
        foreach (var r in file.Relationships ?? [])
        {
            var direction = r.Direction switch
            {
                null or "oneWay" => Direction.OneWay,
                "twoWay" => Direction.TwoWay,
                _ => (Direction?)null,
            };
            if (direction is null) problems.Add($"Relationship '{r.Id}' has an unknown direction '{r.Direction}'.");
            else if (r.Id is null || r.From is null || r.To is null) problems.Add("A relationship is missing its id, from or to.");
            else relationships.Add(new ContextRelationship(r.Id, r.From, r.To, r.Label ?? "", r.Technology, direction.Value));
        }

        if (problems.Count > 0) throw new UnreadableException(problems);

        var diagram = new ContextDiagram(file.Title ?? "Untitled", elements, relationships);
        var rules = diagram.Problems();
        if (rules.Count > 0) throw new UnreadableException(rules);
        return diagram;
    }

    private sealed record FileDto(int SchemaVersion, string? Title, List<ElementDto>? Elements, List<RelationshipDto>? Relationships);

    private sealed record ElementDto(
        string? Id, string? Type, string? Name, string? Description, string? Technology, double? X, double? Y, bool Pinned);

    private sealed record RelationshipDto(
        string? Id, string? From, string? To, string? Label, string? Technology, string? Direction);
}
