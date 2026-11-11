using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace MidiRouter;

public partial class MainWindow
{
    private Expander? movingSection;
    private FrameworkElement? sectionGrip;
    private Point sectionStart;
    private bool sectionDragged;
    private static Expander? FindSection(DependencyObject? item)
    {
        while (item != null)
        {
            if (item is Expander section)
                return section;
            item = System.Windows.Media.VisualTreeHelper.GetParent(item);
        }
        return null;
    }
    private void RestoreSectionOrder()
    {
        var sections = ReceiverSections.Children.OfType<Expander>().ToList();
        var order = Model.State.SectionOrder ?? new();
        var sorted = sections.OrderBy(s => { int index = order.IndexOf(s.Header.ToString()!); return index < 0 ? int.MaxValue : index; }).ToList();
        ReceiverSections.Children.Clear();
        foreach (var section in sorted)
            ReceiverSections.Children.Add(section);
    }
    private void StartSection(FrameworkElement grip, Point point)
    {
        wheelTimer.Stop();
        holdTimer.Stop();
        movingSection = FindSection(grip);
        sectionGrip = grip;
        sectionStart = point;
        sectionDragged = false;
    }
    private void MoveSection(Point point)
    {
        if (movingSection == null || movingSection.Tag?.ToString() == "hidden")
            return;
        if ((point - sectionStart).Length > 8)
        {
            sectionDragged = true;
            movingSection.Opacity = .55;
        }
    }
    private void FinishSection(Point point)
    {
        var section = movingSection;
        if (section == null)
            return;
        movingSection = null;
        section.Opacity = 1;
        if (!sectionDragged)
        {
            section.IsExpanded = !section.IsExpanded;
            return;
        }
        if (section.Tag?.ToString() == "source")
        {
            var sourceOthers = VisualExpanders(SourceSectionList).Where(s => s != section).ToList();
            int position = sourceOthers.Count;
            for (int i = 0; i < sourceOthers.Count; i++)
                if (point.Y < sourceOthers[i].TranslatePoint(new Point(0, 20), this).Y)
                {
                    position = i;
                    break;
                }
            Model.ReorderSourceSection(section.Header.ToString()!, position);
            return;
        }
        var others = ReceiverSections.Children.OfType<Expander>().Where(s => s != section && s.Tag?.ToString() != "hidden").ToList();
        int index = others.Count;
        for (int i = 0; i < others.Count; i++)
        {
            var header = others[i].TranslatePoint(new Point(0, 20), this);
            if (point.Y < header.Y)
            {
                index = i;
                break;
            }
        }
        ReceiverSections.Children.Remove(section);
        ReceiverSections.Children.Insert(index, section);
        Model.State.SectionOrder = ReceiverSections.Children.OfType<Expander>().Where(s => s.Tag?.ToString() != "hidden").Select(s => s.Header.ToString()!).ToList();
        Model.Save();
    }
    private static IEnumerable<Expander> VisualExpanders(DependencyObject root)
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is Expander expander && expander.Tag?.ToString() == "source")
                yield return expander;
            else
                foreach (var nested in VisualExpanders(child))
                    yield return nested;
        }
    }
    private void SectionMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.StylusDevice != null)
            return;
        StartSection((FrameworkElement)sender, e.GetPosition(this));
        sectionGrip?.CaptureMouse();
        e.Handled = true;
    }
    private void SectionMouseMove(object sender, MouseEventArgs e)
    {
        if (movingSection != null && e.LeftButton == MouseButtonState.Pressed)
        {
            MoveSection(e.GetPosition(this));
            e.Handled = true;
        }
    }
    private void SectionMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (movingSection == null)
            return;
        sectionGrip?.ReleaseMouseCapture();
        FinishSection(e.GetPosition(this));
        e.Handled = true;
    }
    private void SectionTouchDown(object sender, TouchEventArgs e)
    {
        StartSection((FrameworkElement)sender, e.GetTouchPoint(this).Position);
        sectionGrip?.CaptureTouch(e.TouchDevice);
        e.Handled = true;
    }
    private void SectionTouchMove(object sender, TouchEventArgs e)
    {
        if (movingSection != null)
        {
            MoveSection(e.GetTouchPoint(this).Position);
            e.Handled = true;
        }
    }
    private void SectionTouchUp(object sender, TouchEventArgs e)
    {
        if (movingSection == null)
            return;
        sectionGrip?.ReleaseTouchCapture(e.TouchDevice);
        FinishSection(e.GetTouchPoint(this).Position);
        e.Handled = true;
    }

}
