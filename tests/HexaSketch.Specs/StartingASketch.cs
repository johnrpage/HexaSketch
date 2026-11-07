using HexaSketch.Domain;

namespace HexaSketch.Specs;

public class StartingASketch : Sketching
{
    [Fact]
    public void The_hexagon_is_the_system_from_the_context_diagram()
    {
        Start();

        Assert.Equal("Cumulative Flow", Sketch.SystemName);
        Assert.Equal("Cumulative Flow - Context Diagram", Sketch.Title);
    }

    [Fact]
    public void There_is_always_a_User_port_on_the_driving_side()
    {
        Start();

        var user = Assert.Single(Sketch.Ports);
        Assert.True(user.IsUser);
        Assert.Equal("User", user.Name);
        Assert.Equal(Side.Driving, user.Side);
    }

    [Fact]
    public void Each_primary_actor_becomes_a_real_adapter_on_the_User_port()
    {
        Start();

        Assert.Equal(["Viewer", "Agent"], UserPort.Adapters.Select(a => a.Name));
        Assert.All(UserPort.Adapters, a => Assert.Equal(AdapterKind.Real, a.Kind));
    }

    [Fact]
    public void A_primary_actor_adapter_takes_its_technology_from_the_actor()
    {
        Start();

        Assert.Equal("WPF", Adapter("Viewer").Technology);
        Assert.Equal("CLI", Adapter("Agent").Technology);
    }

    [Fact]
    public void Everything_else_waits_in_the_list()
    {
        Start();

        Assert.Equal(["Azure DevOps", "Jira", "Storage 1"], Sketch.Unplaced.Select(i => i.Name));
    }

    [Fact]
    public void A_new_sketch_has_nothing_to_save_or_undo()
    {
        Start();

        Assert.False(Queries.HasUnsavedChanges);
        Assert.False(Queries.CanUndo);
        Assert.Null(Queries.Location);
    }

    [Fact]
    public void A_missing_context_diagram_is_reported()
    {
        var outcome = Tasks.StartFrom("nowhere.contextsketch.json");

        Assert.False(outcome.Succeeded);
        Assert.Contains("nowhere", Assert.Single(outcome.Problems));
        Assert.Null(Queries.Sketch);
    }

    [Fact]
    public void A_broken_context_diagram_is_reported_with_every_problem()
    {
        ContextDiagrams.Add("broken", new ContextDiagram("Broken",
            [
                new("a", ElementType.PrimaryActor, "Lonely", null, null, null, null, false),
            ],
            []));

        var outcome = Tasks.StartFrom("broken");

        Assert.False(outcome.Succeeded);
        Assert.Equal(2, outcome.Problems.Count);
    }
}
