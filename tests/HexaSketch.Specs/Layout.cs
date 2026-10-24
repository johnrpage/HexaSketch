using HexaSketch.Domain;

namespace HexaSketch.Specs;

public class Layout : Sketching
{
    private const double Tolerance = 0.5;

    [Fact]
    public void The_User_port_sits_at_nine_o_clock()
    {
        Start();

        var user = Queries.Diagram!.Ports.Single(p => p.IsUser);

        Assert.True(user.Centre.X < 0);
        Assert.Equal(0, user.Centre.Y, Tolerance);
    }

    [Fact]
    public void One_driven_port_sits_at_three_o_clock()
    {
        Start();
        Tasks.AddPort(Side.Driven);

        var port = Queries.Diagram!.Ports.Single(p => p.Side == Side.Driven);

        Assert.True(port.Centre.X > 0);
        Assert.Equal(0, port.Centre.Y, Tolerance);
    }

    [Fact]
    public void Driven_ports_spread_from_top_to_bottom_in_order()
    {
        Start();
        Tasks.AddPort(Side.Driven);
        Tasks.AddPort(Side.Driven);
        Tasks.AddPort(Side.Driven);

        var ys = Queries.Diagram!.Ports.Where(p => p.Side == Side.Driven).Select(p => p.Centre.Y).ToList();

        Assert.True(ys[0] < ys[1] && ys[1] < ys[2]);
        Assert.Equal(0, ys[1], Tolerance);
    }

    [Fact]
    public void Adapters_on_a_port_never_overlap()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);
        for (var i = 0; i < 4; i++) Tasks.AddAdapter(port);

        var boxes = Queries.Diagram!.Ports.Single(p => p.Id == port).Adapters.Select(a => a.Box).ToList();

        for (var i = 1; i < boxes.Count; i++)
            Assert.True(boxes[i].Y >= boxes[i - 1].Bottom);
    }

    [Fact]
    public void Everything_fits_inside_the_bounds()
    {
        Start();
        var port = Tasks.AddPort(Side.Driven);
        Tasks.Place(Item("Jira"), port);

        var view = Queries.Diagram!;
        var boxes = view.Ports.SelectMany(p => p.Adapters).Select(a => a.Box);

        Assert.All(boxes, b => Assert.True(
            b.X >= view.Bounds.X && b.Right <= view.Bounds.Right && b.Y >= view.Bounds.Y && b.Bottom <= view.Bounds.Bottom));
    }
}
