using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MidiRouter;

internal sealed class SectionDragPreview : Adorner
{
    private readonly VisualBrush brush;
    private readonly Size size;
    private readonly AdornerLayer? layer;
    private Point position;

    public SectionDragPreview(UIElement host, FrameworkElement section) : base(host)
    {
        IsHitTestVisible = false;
        brush = new VisualBrush(section) { Opacity = .85 };
        size = section.TransformToAncestor(host).TransformBounds(new Rect(section.RenderSize)).Size;
        layer = AdornerLayer.GetAdornerLayer(host);
    }

    public void Show() => layer?.Add(this);
    public void Close() => layer?.Remove(this);
    public void Move(Point point)
    {
        position = point;
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext drawingContext) =>
        drawingContext.DrawRectangle(brush, null, new Rect(position, size));
}
