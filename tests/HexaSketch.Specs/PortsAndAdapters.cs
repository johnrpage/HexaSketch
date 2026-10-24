using HexaSketch.Domain;

namespace HexaSketch.Specs;

public class PortsAndAdapters : Sketching
{
    [Fact]
    public void Ports_are_added_with_a_default_name_on_the_chosen_side()
    {
        Start();

        Tasks.AddPort(Side.Driven);
        Tasks.AddPort(Side.Driving);

        Assert.Equal(Side.Driven, Port("Port 1").Side);
        Assert.Equal(Side.Driving, Port("Port 2").Side);
    }

    [Fact]
    public void Dropping_an_item_on_a_port_makes_it_an_adapter_and_takes_it_out_of_the_list()
    {
        Start();
        var tracker = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(tracker, "Tracker");

        Tasks.Place(Item("Azure DevOps"), tracker);
        Tasks.Place(Item("Jira"), tracker);

        Assert.Equal(["Azure DevOps", "Jira"], Port("Tracker").Adapters.Select(a => a.Name));
        Assert.Equal(["Storage 1"], Sketch.Unplaced.Select(i => i.Name));
        Assert.Equal("Azure DevOps", Adapter("Azure DevOps").Source?.Name);
    }

    [Fact]
    public void An_item_keeps_its_technology_and_description_as_an_adapter()
    {
        Start();
        var storage = Tasks.AddPort(Side.Driven);

        Tasks.Place(Item("Storage 1"), storage);

        Assert.Equal("Flat files", Adapter("Storage 1").Technology);
    }

    [Fact]
    public void Any_item_can_go_on_any_port()
    {
        Start();

        Tasks.Place(Item("Jira"), UserPort.Id);

        Assert.Contains("Jira", UserPort.Adapters.Select(a => a.Name));
    }

    [Fact]
    public void An_item_can_be_placed_only_once()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);
        var jira = Item("Jira");
        Tasks.Place(jira, port);

        Assert.Throws<SketchRuleException>(() => Tasks.Place(jira, port));
    }

    [Fact]
    public void Adapters_can_be_added_by_hand_and_have_no_source()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);

        var id = Tasks.AddAdapter(port);
        Tasks.RenameAdapter(id, "In-memory store");
        Tasks.SetAdapterKind(id, AdapterKind.Fake);

        var adapter = Adapter("In-memory store");
        Assert.Null(adapter.Source);
        Assert.Equal(AdapterKind.Fake, adapter.Kind);
        Assert.Equal("InMemoryStore", adapter.CodeName);
    }

    [Theory]
    [InlineData(AdapterKind.Fake, "Fake")]
    [InlineData(AdapterKind.Simulated, "Simulated")]
    public void Fake_and_simulated_adapters_are_named_after_their_kind(AdapterKind kind, string name)
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);

        Tasks.AddAdapter(port, kind);
        Tasks.AddAdapter(port, kind);

        Assert.Equal([name, $"{name} 2"], Port("Port 1").Adapters.Select(a => a.Name));
        Assert.All(Port("Port 1").Adapters, a => Assert.Equal(kind, a.Kind));
    }

    [Fact]
    public void Dropping_an_item_on_the_hexagon_makes_a_port_named_after_it()
    {
        Start();

        Tasks.PlaceOnNewPort(Item("Jira"), Side.Driven);

        var port = Port("Jira");
        Assert.Equal(Side.Driven, port.Side);
        Assert.Equal(["Jira"], port.Adapters.Select(a => a.Name));
        Assert.DoesNotContain("Jira", Sketch.Unplaced.Select(i => i.Name));
    }

    [Fact]
    public void A_new_port_from_an_item_gets_a_fresh_name_if_the_name_is_taken()
    {
        Start();
        Tasks.RenamePort(Tasks.AddPort(Side.Driving), "Jira");

        Tasks.PlaceOnNewPort(Item("Jira"), Side.Driving);

        Assert.Equal(Side.Driving, Port("Jira 2").Side);
    }

    [Fact]
    public void Making_a_port_from_an_item_is_one_undo_step()
    {
        Start();
        Tasks.PlaceOnNewPort(Item("Jira"), Side.Driven);

        Tasks.Undo();

        Assert.Single(Sketch.Ports);
        Assert.Contains("Jira", Sketch.Unplaced.Select(i => i.Name));
    }

    [Fact]
    public void An_item_already_placed_cannot_make_a_new_port()
    {
        Start();
        var jira = Item("Jira");
        Tasks.PlaceOnNewPort(jira, Side.Driven);

        Assert.Throws<SketchRuleException>(() => Tasks.PlaceOnNewPort(jira, Side.Driven));
        Assert.Equal(2, Sketch.Ports.Count);
    }

    [Fact]
    public void An_adapter_can_be_moved_off_the_User_port_onto_another_port()
    {
        Start();
        var console = Tasks.AddPort(Side.Driving);

        Tasks.MoveAdapter(Adapter("Agent").Id, console);

        Assert.Equal(["Viewer"], UserPort.Adapters.Select(a => a.Name));
        Assert.Equal(["Agent"], Port("Port 1").Adapters.Select(a => a.Name));
    }

    [Fact]
    public void Adapters_can_be_reordered_on_their_port()
    {
        Start();

        Tasks.MoveAdapter(Adapter("Agent").Id, UserPort.Id, 0);

        Assert.Equal(["Agent", "Viewer"], UserPort.Adapters.Select(a => a.Name));
    }

    [Fact]
    public void Ports_can_be_reordered_on_their_side()
    {
        Start();
        var first = Tasks.AddPort(Side.Driven);
        Tasks.AddPort(Side.Driven);

        Tasks.MovePort(first, 1);

        Assert.Equal(["Port 2", "Port 1"], Sketch.Ports.Where(p => p.Side == Side.Driven).Select(p => p.Name));
    }

    [Fact]
    public void Removing_an_adapter_that_came_from_the_diagram_returns_it_to_the_list()
    {
        Start();

        Tasks.RemoveAdapter(Adapter("Viewer").Id);

        Assert.Contains("Viewer", Sketch.Unplaced.Select(i => i.Name));
    }

    [Fact]
    public void Deleting_a_port_returns_its_diagram_items_to_the_list_and_drops_hand_made_adapters()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);
        Tasks.Place(Item("Jira"), port);
        Tasks.RenameAdapter(Tasks.AddAdapter(port), "Fake tracker");

        Tasks.DeletePort(port);

        Assert.Contains("Jira", Sketch.Unplaced.Select(i => i.Name));
        Assert.DoesNotContain("Fake tracker", Sketch.Ports.SelectMany(p => p.Adapters).Select(a => a.Name));
    }

    [Fact]
    public void The_User_port_cannot_be_deleted()
    {
        Start();

        Assert.Throws<SketchRuleException>(() => Tasks.DeletePort(UserPort.Id));
        Assert.False(Queries.CanUndo);
    }
}
