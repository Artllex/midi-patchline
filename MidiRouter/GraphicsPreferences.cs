using System.Text.Json.Serialization;
using System.Windows;

namespace MidiRouter;

public sealed class GraphicsPreferences
{
    public int SourceHeight { get; init; } = 128;
    public int ReceiverHeight { get; init; } = 70;
    public int VerticalGap { get; init; } = 12;
    public int HorizontalGap { get; init; } = 20;
    public int OutlineWidth { get; init; } = 2;
    public bool ShowSourceThumbnails { get; init; } = true;
    public bool ShowReceiverThumbnails { get; init; } = true;

    [JsonIgnore] public Thickness Outline => new(OutlineWidth);
    [JsonIgnore] public Thickness SourceMargin => new(0, 0, 0, VerticalGap);
    [JsonIgnore] public Thickness ReceiverMargin => new(0, 0, HorizontalGap, VerticalGap);
    [JsonIgnore] public GridLength ColumnGap => new(HorizontalGap * 2);
    [JsonIgnore] public GridLength SourceThumbnailWidth => new(ShowSourceThumbnails ? 96 : 0);
    [JsonIgnore] public GridLength ReceiverThumbnailWidth => new(ShowReceiverThumbnails ? 108 : 0);
    [JsonIgnore] public double SourceControlsHeight => Math.Min(56, (SourceHeight - 16 - 2 * OutlineWidth) * .55);
    [JsonIgnore] public GridLength SourceControlsRow => new(SourceControlsHeight);
    [JsonIgnore] public double ReceiverControlsHeight => Math.Min(56, ReceiverHeight - 2 * OutlineWidth);
}
