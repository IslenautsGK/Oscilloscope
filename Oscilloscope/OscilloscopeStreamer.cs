using ScottPlot;

namespace Oscilloscope;

internal sealed class OscilloscopeStreamer
    : IPlottable,
        IManagesAxisLimits,
        IHasLine,
        IHasLegendText
{
    public bool IsVisible { get; set; } = true;

    public IAxes Axes { get; set; } = new Axes();

    public IEnumerable<LegendItem> LegendItems => LegendItem.Single(this, LegendText, LineStyle);

    public string LegendText { get; set; } = "";

    public bool ManageAxisLimits { get; set; } = true;

    public LineStyle LineStyle { get; set; } = new LineStyle { Width = 1F };

    public float LineWidth
    {
        get => LineStyle.Width;
        set => LineStyle.Width = value;
    }

    public LinePattern LinePattern
    {
        get => LineStyle.Pattern;
        set => LineStyle.Pattern = value;
    }

    public Color LineColor
    {
        get => LineStyle.Color;
        set => LineStyle.Color = value;
    }

    public int Cycle { get; set; } = 2;

    private double MaxX => (ys.Count - 1) * Cycle / 1000.0;

    private readonly List<double> ys = [];

    private double maxY;

    private double minY;

    public AxisLimits GetAxisLimits()
    {
        if (ys.Count == 0)
            return default;
        return new(0, MaxX, minY, maxY);
    }

    public void UpdateAxisLimits(Plot plot)
    {
        if (!ManageAxisLimits)
            return;
        if (MaxX != Axes.XAxis.Max)
            Axes.XAxis.Range.Pan(MaxX - Axes.XAxis.Max);
        var y = ys[^1];
        if (Axes.YAxis.Max < y)
            Axes.YAxis.Max = y;
        if (Axes.YAxis.Min > y)
            Axes.YAxis.Min = y;
    }

    public void Add(double y)
    {
        if (y > maxY)
            maxY = y;
        if (y < minY)
            minY = y;
        ys.Add(y);
    }

    public void Render(RenderPack rp) =>
        Drawing.DrawLines(rp.Canvas, rp.Paint, GetPixels(), LineStyle);

    private IEnumerable<Pixel> GetPixels()
    {
        var min = Math.Max(0, (int)(Axes.XAxis.Min * 1000 / Cycle) - 1);
        var max = Math.Min(ys.Count, (int)(Axes.XAxis.Max * 1000 / Cycle) + 2);
        var step = 1;
        if (max - min > 100)
            step = (max - min) / 100;
        for (var i = min; i < max; i += step)
            yield return Axes.GetPixel(new(i * Cycle / 1000.0, ys[i]));
    }
}
