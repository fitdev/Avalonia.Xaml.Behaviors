﻿using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Avalonia.Xaml.Interactions.DragAndDrop;

/// <summary>
/// 
/// </summary>
public class ContextDragBehavior : Behavior<Control>
{
    private Point _dragStartPoint;
    private PointerEventArgs? _triggerEvent;
    private bool _lock;

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<object?> ContextProperty =
        AvaloniaProperty.Register<ContextDragBehavior, object?>(nameof(Context));

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<IDragHandler?> HandlerProperty =
        AvaloniaProperty.Register<ContextDragBehavior, IDragHandler?>(nameof(Handler));

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<double> HorizontalDragThresholdProperty = 
        AvaloniaProperty.Register<ContextDragBehavior, double>(nameof(HorizontalDragThreshold), 3);

    /// <summary>
    /// 
    /// </summary>
    public static readonly StyledProperty<double> VerticalDragThresholdProperty =
        AvaloniaProperty.Register<ContextDragBehavior, double>(nameof(VerticalDragThreshold), 3);

    /// <summary>
    /// 
    /// </summary>
    public object? Context
    {
        get => GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public IDragHandler? Handler
    {
        get => GetValue(HandlerProperty);
        set => SetValue(HandlerProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public double HorizontalDragThreshold
    {
        get => GetValue(HorizontalDragThresholdProperty);
        set => SetValue(HorizontalDragThresholdProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    public double VerticalDragThreshold
    {
        get => GetValue(VerticalDragThresholdProperty);
        set => SetValue(VerticalDragThresholdProperty, value);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, AssociatedObject_PointerPressed, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AssociatedObject?.AddHandler(InputElement.PointerReleasedEvent, AssociatedObject_PointerReleased, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AssociatedObject?.AddHandler(InputElement.PointerMovedEvent, AssociatedObject_PointerMoved, RoutingStrategies.Direct | RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, AssociatedObject_PointerPressed);
        AssociatedObject?.RemoveHandler(InputElement.PointerReleasedEvent, AssociatedObject_PointerReleased);
        AssociatedObject?.RemoveHandler(InputElement.PointerMovedEvent, AssociatedObject_PointerMoved);
    }

    private async Task DoDragDrop(PointerEventArgs triggerEvent, object? value)
    {
        var data = new DataObject();
        data.Set(ContextDropBehavior.DataFormat, value!);

        var effect = DragDropEffects.None;

        if (triggerEvent.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            effect |= DragDropEffects.Link;
        }
        else if (triggerEvent.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            effect |= DragDropEffects.Move;
        }
        else if (triggerEvent.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            effect |= DragDropEffects.Copy;
        }
        else
        {
            effect |= DragDropEffects.Move;
        }

        await DragDrop.DoDragDrop(triggerEvent, data, effect);
    }

    private void AssociatedObject_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (properties.IsLeftButtonPressed)
        {
            if (e.Source is IControl control
                && AssociatedObject?.DataContext == control.DataContext)
            {
                _dragStartPoint = e.GetPosition(null);
                _triggerEvent = e;
                _lock = true;
                e.Pointer.Capture(AssociatedObject);
            }
        }
    }

    private void AssociatedObject_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Equals(e.Pointer.Captured, AssociatedObject))
        {
            if (e.InitialPressMouseButton == MouseButton.Left && _triggerEvent is { })
            {
                _triggerEvent = null;
                _lock = false;
            }

            e.Pointer.Capture(null);
        }
    }

    private async void AssociatedObject_PointerMoved(object? sender, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(AssociatedObject).Properties;
        if (Equals(e.Pointer.Captured, AssociatedObject) 
            && properties.IsLeftButtonPressed &&
            _triggerEvent is { })
        {
            var point = e.GetPosition(null);
            var diff = _dragStartPoint - point;
            var horizontalDragThreshold = HorizontalDragThreshold;
            var verticalDragThreshold = VerticalDragThreshold;

            if (Math.Abs(diff.X) > horizontalDragThreshold || Math.Abs(diff.Y) > verticalDragThreshold)
            {
                if (_lock)
                {
                    _lock = false;
                }
                else
                {
                    return;
                }

                var context = Context ?? AssociatedObject?.DataContext;
                    
                Handler?.BeforeDragDrop(sender, _triggerEvent, context);

                await DoDragDrop(_triggerEvent, context);

                Handler?.AfterDragDrop(sender, _triggerEvent, context);

                _triggerEvent = null;
            }
        }
    }
}
