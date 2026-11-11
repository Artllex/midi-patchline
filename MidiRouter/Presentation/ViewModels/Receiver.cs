namespace MidiRouter;

public sealed class Receiver : Observable
{
    private readonly RouterViewModel owner;
    internal Endpoint Endpoint { get; }
    public Receiver(RouterViewModel model, Endpoint endpoint)
    {
        owner = model;
        Endpoint = endpoint;
    }
    public string Name => Endpoint.Name;
    public string? ImagePath => ArtworkPicker.UriFor(Endpoint.ImageKey ?? Endpoint.Id);
    private System.Windows.Media.Imaging.BitmapSource? picture;
    private bool? pictureUnavailable;
    public System.Windows.Media.Imaging.BitmapSource? Picture
    {
        get
        {
            if (ImagePath == null)
                return null;
            if (picture != null && pictureUnavailable == IsUnavailable)
                return picture;
            var original = new System.Windows.Media.Imaging.BitmapImage(new Uri(ImagePath));
            if (!IsUnavailable)
                picture = original;
            else
            {
                var rgba = new System.Windows.Media.Imaging.FormatConvertedBitmap(original, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                int stride = rgba.PixelWidth * 4;
                var pixels = new byte[stride * rgba.PixelHeight];
                rgba.CopyPixels(pixels, stride, 0);
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte gray = (byte)((pixels[i] * 114 + pixels[i + 1] * 587 + pixels[i + 2] * 299) / 1000);
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = gray;
                }
                picture = System.Windows.Media.Imaging.BitmapSource.Create(rgba.PixelWidth, rgba.PixelHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
            }
            picture.Freeze();
            pictureUnavailable = IsUnavailable;
            return picture;
        }
    }
    public bool IsUnavailable => !Endpoint.Available || string.IsNullOrWhiteSpace(Endpoint.Port);
    public bool IsSolo => owner.SoloReceiverId == Endpoint.Id;
    public bool IsRouted => owner.ActiveRoute(Endpoint.Id) != null;
    public string OctaveLabel => $"{(owner.ActiveRoute(Endpoint.Id)?.Octaves ?? 0):+0;-0;0} oct";
    public string Hint => IsUnavailable ? "Niedostępny · możesz przypisać teraz i włączyć później" : $"{Endpoint.Port} · CH {Endpoint.Channel} · kliknij, aby zmienić przypisanie";
    public void Refresh()
    {
        Changed(nameof(Name));
        Changed(nameof(IsUnavailable));
        Changed(nameof(Picture));
        Changed(nameof(IsRouted));
        Changed(nameof(IsSolo));
        Changed(nameof(OctaveLabel));
        Changed(nameof(Hint));
    }
}
