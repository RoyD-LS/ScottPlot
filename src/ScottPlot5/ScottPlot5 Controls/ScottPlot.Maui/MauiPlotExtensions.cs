using SkiaSharp;
using SkiaSharp.Views.Maui;
using ScottPlot.Interactivity;

namespace ScottPlot.Maui;

internal static class MauiPlotExtensions
{
    internal static Pixel ToPixel(this SKPoint e) => new(e.X, e.Y);

    internal static void ProcessTouchDown(this UserInputProcessor processor, MauiPlot plot, SKTouchEventArgs e)
    {
        var pixel = e.Location.ToPixel();

        IUserAction action = e.DeviceType switch
        {
            SKTouchDeviceType.Mouse => e.MouseButton switch
            {
                SKMouseButton.Left => new Interactivity.UserActions.LeftMouseDown(pixel),
                SKMouseButton.Middle => new Interactivity.UserActions.MiddleMouseDown(pixel),
                SKMouseButton.Right => new Interactivity.UserActions.RightMouseDown(pixel),
                _ => new Interactivity.UserActions.Unknown()
            },
            SKTouchDeviceType.Touch or SKTouchDeviceType.Pen => new Interactivity.UserActions.LeftMouseDown(pixel),
            _ => new Interactivity.UserActions.Unknown()
        };

        processor.Process(action);
    }

    internal static void ProcessTouchUp(this UserInputProcessor processor, MauiPlot plot, SKTouchEventArgs e)
    {
        var pixel = e.Location.ToPixel();

        IUserAction action = e.DeviceType switch
        {
            SKTouchDeviceType.Mouse => e.MouseButton switch
            {
                SKMouseButton.Left => new Interactivity.UserActions.LeftMouseUp(pixel),
                SKMouseButton.Middle => new Interactivity.UserActions.MiddleMouseUp(pixel),
                SKMouseButton.Right => new Interactivity.UserActions.RightMouseUp(pixel),
                _ => new Interactivity.UserActions.Unknown()
            },
            SKTouchDeviceType.Touch or SKTouchDeviceType.Pen => new Interactivity.UserActions.LeftMouseUp(pixel),
            _ => new Interactivity.UserActions.Unknown()
        };

        processor.Process(action);
    }

    internal static void ProcessTouchMove(this UserInputProcessor processor, MauiPlot plot, SKTouchEventArgs e)
    {
        var pixel = e.Location.ToPixel();

        IUserAction action = new Interactivity.UserActions.MouseMove(pixel);
        processor.Process(action);
    }

    internal static void ProcessWheelChanged(this UserInputProcessor processor, MauiPlot plot, SKTouchEventArgs e)
    {
        var pixel = e.Location.ToPixel();

        IUserAction action = e.WheelDelta > 0
            ? new Interactivity.UserActions.MouseWheelUp(pixel)
            : new Interactivity.UserActions.MouseWheelDown(pixel);

        processor.Process(action);
    }
}
