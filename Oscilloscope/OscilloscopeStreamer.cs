using ScottPlot;

namespace Oscilloscope;

internal sealed class OscilloscopeStreamer
    : IPlottable,
        IManagesAxisLimits,
        IHasLine,
        IHasLegendText,
        IHasMarker
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

    public MarkerStyle MarkerStyle { get; set; } = new MarkerStyle(MarkerShape.FilledCircle, 0f);

    public MarkerShape MarkerShape
    {
        get => MarkerStyle.Shape;
        set => MarkerStyle.Shape = value;
    }

    public float MarkerSize
    {
        get => MarkerStyle.Size;
        set => MarkerStyle.Size = value;
    }

    public Color MarkerFillColor
    {
        get => MarkerStyle.FillColor;
        set => MarkerStyle.FillColor = value;
    }

    public Color MarkerLineColor
    {
        get => MarkerStyle.LineColor;
        set => MarkerStyle.LineColor = value;
    }

    public Color MarkerColor
    {
        get => MarkerStyle.MarkerColor;
        set => MarkerStyle.MarkerColor = value;
    }

    public float MarkerLineWidth
    {
        get => MarkerStyle.LineWidth;
        set => MarkerStyle.LineWidth = value;
    }

    private readonly List<double> ys = [];

    private double maxY;

    private double minY;

    public AxisLimits GetAxisLimits()
    {
        if (ys.Count == 0)
            return default;
        return new(0, ys.Count - 1, minY, maxY);
    }

    public void UpdateAxisLimits(Plot plot)
    {
        if (!ManageAxisLimits)
            return;
        if (ys.Count > Axes.XAxis.Max + 1)
            Axes.XAxis.Range.Pan(ys.Count - Axes.XAxis.Max - 1);
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

    public void Render(RenderPack rp)
    {
        Drawing.DrawLines(rp.Canvas, rp.Paint, GetPixels(), LineStyle);
        Drawing.DrawMarkers(rp.Canvas, rp.Paint, GetPixels(), MarkerStyle);
    }

    private IEnumerable<Pixel> GetPixels()
    {
        var max = Math.Min(ys.Count, (int)Axes.XAxis.Max + 2);
        for (var i = Math.Max(0, (int)Axes.XAxis.Min - 1); i < max; i++)
            yield return Axes.GetPixel(new(i, ys[i]));
    }
}
