using HexaSketch.Domain;

namespace HexaSketch.Specs;

public class UndoAndSaving : Sketching
{
    [Fact]
    public void Each_edit_is_one_undo_step()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(port, "Tracker");

        Tasks.Undo();
        Assert.Equal("Port 1", Port("Port 1").Name);

        Tasks.Undo();
        Assert.Single(Sketch.Ports);
        Assert.False(Queries.CanUndo);
    }

    [Fact]
    public void Undone_edits_can_be_redone_until_something_new_is_done()
    {
        Start();
        Tasks.AddPort(Side.Driven);
        Tasks.Undo();

        Tasks.Redo();
        Assert.Equal(2, Sketch.Ports.Count);

        Tasks.Undo();
        Tasks.AddPort(Side.Driving);
        Assert.False(Queries.CanRedo);
    }

    [Fact]
    public void Edits_are_unsaved_changes_until_saved()
    {
        Start();
        Tasks.AddPort(Side.Driven);
        Assert.True(Queries.HasUnsavedChanges);

        Assert.True(Tasks.SaveAs("cf.hexasketch.json").Succeeded);

        Assert.False(Queries.HasUnsavedChanges);
        Assert.Equal("cf.hexasketch.json", Queries.Location);
    }

    [Fact]
    public void Undoing_back_to_the_saved_state_means_nothing_is_unsaved()
    {
        Start();
        Tasks.AddPort(Side.Driven);
        Tasks.SaveAs("cf.hexasketch.json");

        Tasks.AddPort(Side.Driven);
        Tasks.Undo();

        Assert.False(Queries.HasUnsavedChanges);
    }

    [Fact]
    public void Save_needs_somewhere_to_save_the_first_time()
    {
        Start();

        Assert.False(Tasks.Save().Succeeded);
    }

    [Fact]
    public void A_saved_sketch_opens_as_it_was()
    {
        Start();
        var tracker = Tasks.AddPort(Side.Driven);
        Tasks.RenamePort(tracker, "Tracker");
        Tasks.Place(Item("Jira"), tracker);
        Tasks.SaveAs("cf.hexasketch.json");
        Tasks.Close();

        Assert.True(Tasks.Open("cf.hexasketch.json").Succeeded);

        Assert.Equal(["Jira"], Port("Tracker").Adapters.Select(a => a.Name));
        Assert.Equal(["Azure DevOps", "Storage 1"], Sketch.Unplaced.Select(i => i.Name));
        Assert.False(Queries.HasUnsavedChanges);
    }

    [Fact]
    public void Opening_a_missing_sketch_is_reported()
    {
        var outcome = Tasks.Open("nowhere.hexasketch.json");

        Assert.False(outcome.Succeeded);
        Assert.Null(Queries.Sketch);
    }

    [Fact]
    public void Queries_say_when_something_changed()
    {
        var changes = 0;
        Queries.Changed += (_, _) => changes++;

        Start();
        Tasks.AddPort(Side.Driven);
        Tasks.Undo();

        Assert.Equal(3, changes);
    }
}
