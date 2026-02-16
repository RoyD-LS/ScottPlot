using SkiaSharp;

namespace ScottPlot.Maui.TouchInteraction;

internal struct TouchEventResult
{
    public TouchAction Action { get; set; }
    public SKPoint? Location { get; set; }
    public PinchData? Pinch { get; set; }

    public TouchEventResult(TouchAction action)
    {
        Action = action;
        Location = null;
        Pinch = null;
    }

    public TouchEventResult(TouchAction action, SKPoint location)
    {
        Action = action;
        Location = location;
        Pinch = null;
    }

    public TouchEventResult(TouchAction action, PinchData pinch)
    {
        Action = action;
        Location = null;
        Pinch = pinch;
    }
}