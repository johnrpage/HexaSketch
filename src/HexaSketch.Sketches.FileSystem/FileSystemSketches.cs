using System.Text.Json;
using System.Text.Json.Serialization;
using HexaSketch.Application;
using HexaSketch.Domain;

namespace HexaSketch.Sketches.FileSystem;

/// <summary>Sketches adapter. Saves and loads .hexasketch.json files, with the context diagram embedded.</summary>
public sealed class FileSystemSketches : HexaSketch.Application.Sketches
{
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Save(string location, Sketch sketch)
    {
        var json = JsonSerializer.Serialize(ToDto(sketch), Options);
        File.WriteAllText(location, json + Environment.NewLine);
    }

    public Sketch Load(string location)
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

        SketchDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<SketchDto>(json, Options);
        }
        catch (JsonException e)
        {
            throw new UnreadableException($"This isn't a valid sketch file: {e.Message}");
        }
        if (dto is null) throw new UnreadableException("The file is empty.");
        if (dto.SchemaVersion != SchemaVersion)
            throw new UnreadableException($"Schema version {dto.SchemaVersion} isn't supported (expected {SchemaVersion}).");
        if (dto.ContextDiagram is null)
            throw new UnreadableException("The sketch has no context diagram.");

        try
        {
            return FromDto(dto);
        }
        catch (FormatException e)
        {
            throw new UnreadableException(e.Message);
        }
        catch (SketchRuleException e)
        {
            throw new UnreadableException(e.Message.Split(Environment.NewLine));
        }
    }

    private static SketchDto ToDto(Sketch sketch) => new(
        SchemaVersion,
        sketch.Title,
        new ContextDiagramDto(
            1,
            sketch.ContextDiagram.Title,
            sketch.ContextDiagram.Elements.Select(e => new ElementDto(
                e.Id, TypeName(e.Type), e.Name, e.Description, e.Technology, e.X, e.Y, e.Pinned)).ToList(),
            sketch.ContextDiagram.Relationships.Select(r => new RelationshipDto(
                r.Id, r.From, r.To, r.Label, r.Technology, r.Direction == Direction.OneWay ? "oneWay" : "twoWay")).ToList()),
        sketch.Ports.Select(p => new PortDto(
            p.Id, p.Name, p.Description, p.Side == Side.Driving ? "driving" : "driven", p.IsUser ? true : null,
            p.Adapters.Select(a => new AdapterDto(
                a.Id, a.Name, a.Technology, a.Description, KindName(a.Kind), a.SourceElementId)).ToList())).ToList());

    private static Sketch FromDto(SketchDto dto)
    {
        var cd = dto.ContextDiagram!;
        var diagram = new ContextDiagram(
            cd.Title ?? "Untitled",
            (cd.Elements ?? []).Select(e => new ContextElement(
                e.Id, ParseType(e.Type), e.Name, e.Description, e.Technology, e.X, e.Y, e.Pinned)).ToList(),
            (cd.Relationships ?? []).Select(r => new ContextRelationship(
                r.Id, r.From, r.To, r.Label ?? "", r.Technology,
                r.Direction == "twoWay" ? Direction.TwoWay : Direction.OneWay)).ToList());

        var ports = (dto.Ports ?? []).Select(p => new PortRecord(
            p.Id, p.Name, p.Description,
            p.Side switch
            {
                "driving" => Side.Driving,
                "driven" => Side.Driven,
                _ => throw new FormatException($"Port '{p.Name}' has an unknown side '{p.Side}'."),
            },
            p.IsUser == true,
            (p.Adapters ?? []).Select(a => new AdapterRecord(
                a.Id, a.Name, a.Technology, a.Description, ParseKind(a.Kind), a.Source)).ToList()));

        return Sketch.Restore(dto.Title ?? diagram.Title, diagram, ports);
    }

    private static string TypeName(ElementType type) => type switch
    {
        ElementType.System => "system",
        ElementType.PrimaryActor => "primaryActor",
        ElementType.SecondaryActor => "secondaryActor",
        ElementType.ExternalSystem => "externalSystem",
        _ => "storage",
    };

    private static ElementType ParseType(string type) => type switch
    {
        "system" => ElementType.System,
        "primaryActor" => ElementType.PrimaryActor,
        "secondaryActor" => ElementType.SecondaryActor,
        "externalSystem" => ElementType.ExternalSystem,
        "storage" => ElementType.Storage,
        _ => throw new FormatException($"Unknown element type '{type}'."),
    };

    private static string KindName(AdapterKind kind) => kind switch
    {
        AdapterKind.Real => "real",
        AdapterKind.Fake => "fake",
        _ => "simulated",
    };

    private static AdapterKind ParseKind(string? kind) => kind switch
    {
        null or "real" => AdapterKind.Real,
        "fake" => AdapterKind.Fake,
        "simulated" => AdapterKind.Simulated,
        _ => throw new FormatException($"Unknown adapter kind '{kind}'."),
    };

    private sealed record SketchDto(int SchemaVersion, string? Title, ContextDiagramDto? ContextDiagram, List<PortDto>? Ports);

    private sealed record ContextDiagramDto(
        int SchemaVersion, string? Title, List<ElementDto>? Elements, List<RelationshipDto>? Relationships);

    private sealed record ElementDto(
        string Id, string Type, string Name, string? Description, string? Technology, double? X, double? Y, bool Pinned);

    private sealed record RelationshipDto(
        string Id, string From, string To, string? Label, string? Technology, string? Direction);

    private sealed record PortDto(
        string Id, string Name, string? Description, string Side, bool? IsUser, List<AdapterDto>? Adapters);

    private sealed record AdapterDto(
        string Id, string Name, string? Technology, string? Description, string? Kind, string? Source);
}
