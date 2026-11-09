using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KeepMeAlive.Controls;

/// <summary>Draws the remaining fraction of a real keep-alive cycle as a clockwise timer ring.</summary>
public sealed class PulseRing : Control
{
    /// <summary>Identifies the remaining cycle fraction, constrained to zero through one.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(PulseRing),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, null,
            static (_, value) => value is double number && double.IsFinite(number) ? Math.Clamp(number, 0, 1) : 0d));

    /// <summary>Identifies the theme-aware brush for the inactive ring track.</summary>
    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(PulseRing),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gets or sets the fraction of the current cycle that remains.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Gets or sets the inactive track brush.</summary>
    public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

    /// <summary>Creates a decorative, non-focusable ring with dynamic theme resources.</summary>
    public PulseRing()
    {
        Focusable = false;
        IsHitTestVisible = false;
        SetResourceReference(ForegroundProperty, "PrimaryBrush");
        SetResourceReference(TrackBrushProperty, "RingTrackBrush");
    }

    /// <inheritdoc/>
    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        const double thickness = 14;
        double radius = Math.Max(0, (Math.Min(ActualWidth, ActualHeight) - thickness) / 2);
        if (radius == 0) { return; }
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        drawingContext.DrawEllipse(null, new Pen(TrackBrush, thickness), center, radius, radius);
        if (Value <= 0) { return; }
        var pen = new Pen(Foreground, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (Value >= 1)
        {
            drawingContext.DrawEllipse(null, pen, center, radius, radius);
            return;
        }
        double angle = Value * Math.PI * 2;
        var end = new Point(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle));
        var arc = new StreamGeometry();
        using (var context = arc.Open())
        {
            context.BeginFigure(new Point(center.X, center.Y - radius), false, false);
            context.ArcTo(end, new Size(radius, radius), 0, Value > 0.5, SweepDirection.Clockwise, true, false);
        }
        arc.Freeze();
        drawingContext.DrawGeometry(null, pen, arc);
    }
}
