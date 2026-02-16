using ScottPlot.Interactivity;
using ScottPlot.Maui.TouchInteraction;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace ScottPlot.Maui;

public class MauiPlot : SKCanvasView, IPlotControl
{
    public Plot Plot { get; internal set; }
    public IMultiplot Multiplot { get; set; }
    public GRContext? GRContext => null;
    public IPlotMenu? Menu { get; set; }
    public UserInputProcessor UserInputProcessor { get; }
    public float DisplayScale { get; set; }

    public bool UseIndependentAxisScaling
    {
        get => _touchInteractionStateMachine.UseIndependentAxisScaling;
        set => _touchInteractionStateMachine.UseIndependentAxisScaling = value;
    }

    private readonly TouchInteractionStateMachine _touchInteractionStateMachine = new();

    public MauiPlot()
    {
        Plot = new Plot() { PlotControl = this };
        Multiplot = new Multiplot(Plot);
        DisplayScale = DetectDisplayScale();
        UserInputProcessor = new UserInputProcessor(this);
        Menu = new MauiPlotMenu(this);

        IgnorePixelScaling = true;
        EnableTouchEvents = true;
        Touch += MauiPlot_Touch;
    }

    private void MauiPlot_Touch(object? sender, SKTouchEventArgs e)
    {
        TouchEventResult result;

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                result = _touchInteractionStateMachine.ProcessTouchPressed(e.Id, e.Location);
                HandleTouchResult(result, e);
                break;

            case SKTouchAction.Moved:
                result = _touchInteractionStateMachine.ProcessTouchMoved(e.Id, e.Location);
                HandleTouchResult(result, e);
                break;

            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                result = _touchInteractionStateMachine.ProcessTouchReleased(e.Id);
                HandleTouchResult(result, e);
                break;

            case SKTouchAction.WheelChanged:
                UserInputProcessor.ProcessWheelChanged(this, e);
                break;

            case SKTouchAction.Entered:
            case SKTouchAction.Exited:
            default:
                break;
        }

        e.Handled = true;
    }

    private void HandleTouchResult(TouchEventResult result, SKTouchEventArgs e)
    {
        switch (result.Action)
        {
            case TouchAction.DoubleTap:
                Plot.Axes.AutoScale();
                Refresh();
                break;

            case TouchAction.StartPan:
                if (result.Location.HasValue)
                {
                    UserInputProcessor.ProcessTouchDown(this, e);
                }
                break;

            case TouchAction.Pan:
                if (result.Location.HasValue)
                {
                    UserInputProcessor.ProcessTouchMove(this, e);
                }
                break;

            case TouchAction.EndPan:
                UserInputProcessor.ProcessTouchUp(this, e);
                break;

            case TouchAction.Pinch:
                if (result.Pinch.HasValue)
                {
                    var pinch = result.Pinch.Value;
                    var centerPixel = new Pixel(pinch.Center.X, pinch.Center.Y);
                    MouseAxisManipulation.MouseWheelZoom(Plot, pinch.ScaleX, pinch.ScaleY, centerPixel, false);
                    Refresh();
                }
                break;

            case TouchAction.StartPinch:
            case TouchAction.EndPinch:
            case TouchAction.None:
            default:
                break;
        }
    }

    public void Reset()
    {
        Reset(new Plot());
    }

    public void Reset(Plot plot)
    {
        Plot = plot;
        Plot.PlotControl = this;
        Multiplot.Reset(plot);
        _touchInteractionStateMachine.Reset();
    }

    public void Refresh()
    {
        InvalidateSurface();
    }

    public void ShowContextMenu(Pixel position)
    {
        Menu?.ShowContextMenu(position);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        e.Surface.Canvas.Clear();
        Multiplot.Render(e.Surface);
    }

    public float DetectDisplayScale()
    {
        if (Parent is VisualElement parent)
        {
            Plot.ScaleFactor = parent.Scale;
            DisplayScale = (float)parent.Scale;
        }

        return DisplayScale;
    }

    public void SetCursor(Cursor cursor)
    {
        // NOTE: I can't find a simple cross-platform way to set
        // cursor shape that works for all .NET versions

        //InputSystemCursor.Set(InputSystemCursorShape.Hand);
    }
}
