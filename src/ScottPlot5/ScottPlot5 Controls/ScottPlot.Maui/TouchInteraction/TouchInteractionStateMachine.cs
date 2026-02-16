using SkiaSharp;

namespace ScottPlot.Maui.TouchInteraction;

internal class TouchInteractionStateMachine
{
    private const double DoubleTapThresholdMs = 500;
    private const float AspectRatioThreshold = 3.0f;
    private const double ScaleDetectionThreshold = 0.01;

    private readonly Dictionary<long, SKPoint> _activeTouches = new();

    private float _initialPinchDeltaX;
    private float _initialPinchDeltaY;
    private float _lastPinchDeltaX;
    private float _lastPinchDeltaY;

    private int _tapCount;
    private DateTime _lastTapTime = DateTime.MinValue;

    public TouchInteractionState CurrentState { get; private set; } = TouchInteractionState.Idle;

    public bool UseIndependentAxisScaling { get; set; } = false;

    public TouchEventResult ProcessTouchPressed(long id, SKPoint location)
    {
        _activeTouches[id] = location;

        if (_activeTouches.Count == 1)
        {
            var now = DateTime.Now;
            if ((now - _lastTapTime).TotalMilliseconds < DoubleTapThresholdMs)
            {
                _tapCount++;
                if (_tapCount >= 2)
                {
                    _tapCount = 0;
                    TransitionTo(TouchInteractionState.Idle);
                    return new TouchEventResult(TouchAction.DoubleTap);
                }
            }
            else
            {
                _tapCount = 1;
            }
            _lastTapTime = now;

            if (CurrentState != TouchInteractionState.PostPinch)
            {
                TransitionTo(TouchInteractionState.SingleTouch);
                return new TouchEventResult(TouchAction.StartPan, location);
            }
            else
            {
                TransitionTo(TouchInteractionState.PostPinch);
                return new TouchEventResult(TouchAction.None);
            }
        }
        else if (_activeTouches.Count == 2)
        {
            var points = _activeTouches.Values.ToArray();
            _initialPinchDeltaX = Math.Abs(points[1].X - points[0].X);
            _initialPinchDeltaY = Math.Abs(points[1].Y - points[0].Y);
            _lastPinchDeltaX = _initialPinchDeltaX;
            _lastPinchDeltaY = _initialPinchDeltaY;

            TransitionTo(TouchInteractionState.Pinching);
            return new TouchEventResult(TouchAction.StartPinch);
        }

        return new TouchEventResult(TouchAction.None);
    }

    public TouchEventResult ProcessTouchMoved(long id, SKPoint location)
    {
        if (!_activeTouches.ContainsKey(id))
            return new TouchEventResult(TouchAction.None);

        _activeTouches[id] = location;

        if (_activeTouches.Count == 1 &&
            CurrentState is TouchInteractionState.SingleTouch or TouchInteractionState.Panning)
        {
            TransitionTo(TouchInteractionState.Panning);
            return new TouchEventResult(TouchAction.Pan, location);
        }

        if (_activeTouches.Count == 2 && CurrentState == TouchInteractionState.Pinching)
        {
            var pinchData = CalculatePinchData();
            if (pinchData.HasValue)
            {
                return new TouchEventResult(TouchAction.Pinch, pinchData.Value);
            }
        }

        return new TouchEventResult(TouchAction.None);
    }

    public TouchEventResult ProcessTouchReleased(long id)
    {
        _activeTouches.Remove(id);

        if (_activeTouches.Count == 0)
        {
            var wasInPanningState = CurrentState == TouchInteractionState.Panning;
            var wasInPinchingState = CurrentState == TouchInteractionState.Pinching;

            TransitionTo(TouchInteractionState.Idle);

            if (wasInPanningState)
            {
                return new TouchEventResult(TouchAction.EndPan);
            }

            if (wasInPinchingState)
            {
                return new TouchEventResult(TouchAction.EndPinch);
            }
        }
        else if (_activeTouches.Count == 1 && CurrentState == TouchInteractionState.Pinching)
        {
            TransitionTo(TouchInteractionState.PostPinch);
            return new TouchEventResult(TouchAction.EndPinch);
        }

        return new TouchEventResult(TouchAction.None);
    }

    public void Reset()
    {
        _activeTouches.Clear();
        _tapCount = 0;
        _lastTapTime = DateTime.MinValue;
        _initialPinchDeltaX = 0;
        _initialPinchDeltaY = 0;
        _lastPinchDeltaX = 0;
        _lastPinchDeltaY = 0;
        TransitionTo(TouchInteractionState.Idle);
    }

    private void TransitionTo(TouchInteractionState newState) => CurrentState = newState;

    private PinchData? CalculatePinchData()
    {
        if (_activeTouches.Count != 2)
        {
            // Two touches required for pinch, no pinch data
            return null;
        }

        var points = _activeTouches.Values.ToArray();

        var currentDeltaX = Math.Abs(points[1].X - points[0].X);
        var currentDeltaY = Math.Abs(points[1].Y - points[0].Y);

        var centerX = (points[0].X + points[1].X) / 2;
        var centerY = (points[0].Y + points[1].Y) / 2;

        if (!(_initialPinchDeltaX > 0 && _initialPinchDeltaY > 0))
        {
            // Negative deltas cannot be processed, no pinch data
            return null;
        }

        double scaleX = currentDeltaX / _lastPinchDeltaX;
        double scaleY = currentDeltaY / _lastPinchDeltaY;

        if (double.IsNaN(scaleX) || double.IsInfinity(scaleX) ||
            double.IsNaN(scaleY) || double.IsInfinity(scaleY))
        {
            // Scale cannot be processed, no pinch data
            return null;
        }

        if (UseIndependentAxisScaling)
        {
            var totalChangeX = Math.Abs(currentDeltaX - _initialPinchDeltaX);
            var totalChangeY = Math.Abs(currentDeltaY - _initialPinchDeltaY);

            var xDominant = totalChangeX > totalChangeY * AspectRatioThreshold;
            var yDominant = totalChangeY > totalChangeX * AspectRatioThreshold;


            if (xDominant && !yDominant)
            {
                scaleY = 1.0;
            }
            else if (yDominant && !xDominant)
            {
                scaleX = 1.0;
            }
        }
        else
        {
            // Calculate the distance between touch points to determine overall scale
            var lastDistance = Math.Sqrt(_lastPinchDeltaX * _lastPinchDeltaX + _lastPinchDeltaY * _lastPinchDeltaY);
            var currentDistance = Math.Sqrt(currentDeltaX * currentDeltaX + currentDeltaY * currentDeltaY);
            var uniformScale = currentDistance / lastDistance;

            if (double.IsNaN(uniformScale) || double.IsInfinity(uniformScale))
            {
                // Scale cannot be processed, no pinch data
                return null;
            }

            scaleX = scaleY = uniformScale;
        }

        if (Math.Abs(scaleX - 1.0) < ScaleDetectionThreshold && Math.Abs(scaleY - 1.0) < ScaleDetectionThreshold)
        {
            // Scale is 1:1, no pinch data
            return null;
        }

        _lastPinchDeltaX = currentDeltaX;
        _lastPinchDeltaY = currentDeltaY;

        return new PinchData
        {
            ScaleX = scaleX,
            ScaleY = scaleY,
            Center = new SKPoint(centerX, centerY),
        };

    }
}
