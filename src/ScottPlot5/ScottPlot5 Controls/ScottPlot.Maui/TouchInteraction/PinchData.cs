using SkiaSharp;

namespace ScottPlot.Maui.TouchInteraction;

internal struct PinchData
{
    public double ScaleX { get; set; }
    public double ScaleY { get; set; }
    public SKPoint Center { get; set; }
}
