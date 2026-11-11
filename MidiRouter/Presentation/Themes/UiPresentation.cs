using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MidiRouter;

public static class UiPresentation
{
    private static readonly ConditionalWeakTable<DependencyObject, ElementState> States = new();
    private static readonly List<WeakReference<ElementState>> Elements = new();
    private static string theme = "Default";
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        EventManager.RegisterClassHandler(typeof(ContextMenu), ContextMenu.OpenedEvent,
            new RoutedEventHandler((sender, _) => Scan((DependencyObject)sender)));
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => Track((FrameworkElement)sender)));
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.UnloadedEvent,
            new RoutedEventHandler((sender, _) => { if (States.TryGetValue((DependencyObject)sender, out var state)) state.Detach(); }));
    }

    public static void Configure(string language, string selectedTheme)
    {
        L.Language = L.LanguageCodes.Contains(language) ? language : "PL";
        theme = GraphicsPreferences.NormalizeTheme(selectedTheme);
        foreach (Window window in Application.Current.Windows)
        {
            // Do not attach property observers while XAML is still assigning Freezables.
            if (!window.IsInitialized) continue;
            Scan(window);
            NativeTheme.Apply(window);
        }
        for (int i = Elements.Count - 1; i >= 0; i--)
            if (Elements[i].TryGetTarget(out var state))
                state.Apply();
            else
                Elements.RemoveAt(i);
    }

    private static readonly ConditionalWeakTable<Window, object> Windows = new();

    public static void Attach(Window window)
    {
        if (Windows.TryGetValue(window, out _))
            return;
        Windows.Add(window, new object());
        NativeTheme.Apply(window);
        window.ContentRendered += (_, _) => ScheduleScan(window);
        ScheduleScan(window);
    }

    private static readonly ConditionalWeakTable<DependencyObject, ScanRequest> PendingScans = new();

    private sealed class ScanRequest
    {
        public bool Pending { get; set; }
    }

    private static void ScheduleScan(DependencyObject root)
    {
        var request = PendingScans.GetOrCreateValue(root);
        if (request.Pending || root.Dispatcher.HasShutdownStarted)
            return;
        request.Pending = true;
        root.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
        {
            request.Pending = false;
            if (root is FrameworkElement element && !element.IsLoaded)
                return;
            Scan(root);
        }));
    }

    private static bool SameValue(object? first, object? second)
    {
        if (ReferenceEquals(first, second))
            return true;
        if (first is SolidColorBrush a && second is SolidColorBrush b)
            return a.Color == b.Color && a.Opacity == b.Opacity &&
                a.Transform.Value == b.Transform.Value && a.RelativeTransform.Value == b.RelativeTransform.Value;
        return Equals(first, second);
    }

    private static void Scan(DependencyObject root)
    {
        var visited = new HashSet<DependencyObject>();
        void Visit(DependencyObject item)
        {
            if (!visited.Add(item))
                return;
            if (item is FrameworkElement element)
                Track(element);
            if (item is Visual || item is System.Windows.Media.Media3D.Visual3D)
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(item); i++)
                    Visit(VisualTreeHelper.GetChild(item, i));
            foreach (var child in LogicalTreeHelper.GetChildren(item).OfType<DependencyObject>())
                Visit(child);
        }
        Visit(root);
    }

    private static void Track(FrameworkElement element)
    {
        if (!States.TryGetValue(element, out var state))
        {
            state = new ElementState(element);
            States.Add(element, state);
            Elements.Add(new WeakReference<ElementState>(state));
            if (element is ItemsControl items)
                items.ItemContainerGenerator.StatusChanged += (_, _) =>
                {
                    if (items.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                        ScheduleScan(items);
                };
            if (element is TabControl tabs)
                tabs.SelectionChanged += (_, args) =>
                {
                    if (ReferenceEquals(args.Source, tabs))
                        ScheduleScan(tabs);
                };

            if (element is ComboBox combo)
                combo.DropDownOpened += (_, _) =>
                {
                    if (combo.Template.FindName("PART_Popup", combo) is Popup popup && popup.Child != null)
                        Scan(popup.Child);
                };
        }
        state.Attach();
        if (element is Window window)
            Attach(window);
    }

    internal static bool IsLightTheme => theme is "Light" or "WhiteLight" or "GrayLight";

    internal static Brush ThemeBrush(Brush original, string role = "Background")
    {
        if (theme == "Default" || original is not SolidColorBrush brush || brush.Color.A == 0)
            return original;
        var c = brush.Color;
        double max = Math.Max(c.R, Math.Max(c.G, c.B));
        double min = Math.Min(c.R, Math.Min(c.G, c.B));
        bool green = c.G > c.R * 1.4 && c.G > c.B * 1.15;
        bool red = c.R > c.G * 1.35 && c.R > c.B * 1.15;
        bool orange = red && c.G > c.B * 1.25;
        bool blue = max - min > 30 && c.B > c.R * 1.25 && c.B > c.G * 1.1;
        bool text = role is "Foreground" or "Stroke";
        bool outline = role == "BorderBrush";
        double light = .2126 * c.R + .7152 * c.G + .0722 * c.B;

        Brush Result(byte r, byte g, byte b)
        {
            var result = new SolidColorBrush(Color.FromArgb(c.A, r, g, b));
            result.Freeze();
            return result;
        }

        if (IsLightTheme)
        {
            if (text)
            {
                if (red)
                    return Result(158, 35, 30);
                if (green)
                    return Result(0, 104, 52);
                return Result(20, 24, 30);
            }
            if (outline)
            {
                if (red)
                    return Result(179, 38, 38);
                if (green)
                    return Result(0, 116, 58);
                if (blue)
                    return Result(23, 92, 170);
                return Result(125, 137, 152);
            }
            if (role == "Fill")
            {
                if (green)
                    return Result(22, 137, 75);
                return light > 100 ? Result(252, 252, 252) : Result(37, 42, 47);
            }
            if (green)
                return Result(210, 239, 221);
            if (orange)
                return Result(255, 227, 190);
            if (blue)
                return Result(214, 231, 252);
            if (max >= 190)
                return original;
            if (theme == "WhiteLight")
            {
                byte neutral = light <= 30 ? (byte)255 : (byte)Math.Clamp(250 - (light - 30) * .3, 226, 250);
                return Result(neutral, neutral, neutral);
            }
            if (theme == "GrayLight")
            {
                byte neutral = light <= 30 ? (byte)216 : (byte)Math.Clamp(234 + (light - 40) * .5, 224, 246);
                return Result(neutral, neutral, neutral);
            }
            byte shade = (byte)Math.Clamp(255 - light * .4, 208, 255);
            return Result((byte)Math.Max(0, shade - 3), (byte)Math.Max(0, shade - 1), shade);
        }

        if (max > 110 || max == 0 || (max - min) / max > .45)
            return original;
        var factors = theme switch
        {
            "Graphite" => (1d, 1d, 1d),
            "Midnight" => (.70, .87, 1.35),
            "Plum" => (1.12, .80, 1.25),
            _ => (1d, 1d, 1d)
        };
        return Result((byte)Math.Min(255, light * factors.Item1),
            (byte)Math.Min(255, light * factors.Item2), (byte)Math.Min(255, light * factors.Item3));
    }

    private sealed class ElementState
    {
        private readonly FrameworkElement element;
        private readonly List<Slot> slots = new();
        private bool attached;
        private bool applying;

        public ElementState(FrameworkElement element)
        {
            this.element = element;
            void Add(DependencyProperty property, bool text = false)
            {
                var binding = BindingOperations.GetBindingExpression(element, property);
                if (text && binding?.ParentBinding.Path?.Path is string path &&
                    (path.EndsWith("Name") || path.EndsWith("DraftName")))
                    return;
                if (DependencyPropertyHelper.GetValueSource(element, property).BaseValueSource == BaseValueSource.Inherited)
                    return;
                var descriptor = DependencyPropertyDescriptor.FromProperty(property, element.GetType());
                if (descriptor != null)
                    slots.Add(new Slot(property, descriptor, text));
            }
            if (element is TextBlock)
            {
                Add(TextBlock.TextProperty, true);
                Add(TextBlock.ForegroundProperty);
            }
            if (element is ContentPresenter)
                Add(System.Windows.Documents.TextElement.ForegroundProperty);
            Add(UIElement.OpacityProperty);
            if (element is Control)
            {
                Add(Control.BackgroundProperty);
                Add(Control.BorderBrushProperty);
                Add(Control.ForegroundProperty);
            }
            if (element is Button)
                Add(Button.IsDefaultProperty);
            if (element is ContentControl)
                Add(ContentControl.ContentProperty, true);
            if (element is HeaderedContentControl)
                Add(HeaderedContentControl.HeaderProperty, true);
            if (element is HeaderedItemsControl)
                Add(HeaderedItemsControl.HeaderProperty, true);
            if (element is Window)
                Add(Window.TitleProperty, true);
            if (element is Border)
            {
                Add(Border.BackgroundProperty);
                Add(Border.BorderBrushProperty);
            }
            if (element is Panel)
                Add(Panel.BackgroundProperty);
            if (element is Shape)
            {
                Add(Shape.FillProperty);
                Add(Shape.StrokeProperty);
            }
            Add(FrameworkElement.ToolTipProperty, true);
        }

        public void Attach()
        {
            if (!attached)
            {
                foreach (var slot in slots)
                {
                    var current = element.GetValue(slot.Property);
                    if (slot.Applied == null || !SameValue(current, slot.Applied))
                        slot.Original = current;
                    slot.Descriptor.AddValueChanged(element, Changed);
                }
                attached = true;
                Apply();
            }
        }

        public void Detach()
        {
            if (!attached)
                return;
            foreach (var slot in slots)
                slot.Descriptor.RemoveValueChanged(element, Changed);
            attached = false;
        }

        private void Changed(object? sender, EventArgs e)
        {
            if (applying)
                return;
            foreach (var slot in slots)
            {
                var current = element.GetValue(slot.Property);
                if (!SameValue(current, slot.Applied))
                {
                    slot.Original = current;
                    if (slot.Property == ContentControl.ContentProperty)
                        ScheduleScan(element);
                }
            }
            Apply();
        }

        public void Apply()
        {
            if (!attached || applying)
                return;
            applying = true;
            try
            {
                foreach (var slot in slots)
                {
                    object? value = slot.Original;
                    if (slot.IsText && value is string text)
                        value = L.T(text);
                    else if (!slot.IsText && value is Brush brush)
                    {
                        bool defaultAction = false;
                        if (slot.Property.Name == "Background")
                        {
                            DependencyObject? current = element;
                            while (current != null)
                            {
                                if (current is Button button)
                                {
                                    defaultAction = button.IsDefault;
                                    break;
                                }
                                current = current is Visual ? VisualTreeHelper.GetParent(current) : null;
                            }
                        }
                        value = defaultAction
                            ? new SolidColorBrush(IsLightTheme ? Color.FromRgb(198, 205, 214) : Color.FromRgb(18, 22, 27))
                            : ThemeBrush(brush, slot.Property.Name);
                    }
                    else if (slot.Property == UIElement.OpacityProperty && value is double opacity && IsLightTheme && opacity > 0)
                        value = Math.Max(.85, opacity);
                    slot.Applied = value;
                    if (!SameValue(element.GetValue(slot.Property), value))
                        element.SetCurrentValue(slot.Property, value);
                }
            }
            finally { applying = false; }
        }

        private sealed class Slot
        {
            public DependencyProperty Property { get; }
            public DependencyPropertyDescriptor Descriptor { get; }
            public bool IsText { get; }
            public object? Original { get; set; }
            public object? Applied { get; set; }
            public Slot(DependencyProperty property, DependencyPropertyDescriptor descriptor, bool text)
            {
                Property = property;
                Descriptor = descriptor;
                IsText = text;
            }
        }
    }
}
