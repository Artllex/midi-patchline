using System.Windows;
namespace MidiRouter;
public sealed class EndpointDraft : Observable
{
    public Endpoint Original { get; }
    public string Name { get; set; }
    public string Interface { get; set; }
    public List<string> Interfaces {get;} = new(){"Windows MIDI"};
    public IEnumerable<string> Channels => Enumerable.Range(1,16).Select(c=>c.ToString());
    public string Port { get; set; }
    public List<MidiPortOption> PortOptions { get; }
    public string TagsText { get; set; }
    public bool Hidden { get; set; }
    private string imageKey=""; public string ImageKey { get=>imageKey; set {imageKey=value;Changed();Changed(nameof(Preview));} }
    public System.Windows.Media.Imaging.BitmapSource? Preview => ArtworkPicker.Load(ImageKey);
    public IEnumerable<ArtworkChoice> Artworks => ArtworkPicker.Choices;
    public string[] ImageChoices => new[]{"","roli","nektar","jp","virus","kobol","model-d","system-8","cz5000","neutron"};
    public string ChannelText { get; set; }
    public bool Available { get; set; }
    public bool Routed { get; }
    public string OctaveText { get; set; }
    public EndpointDraft(Endpoint endpoint,Route? route,bool input=false) {
        TagsText=string.Join(", ",endpoint.Tags??new());Hidden=endpoint.Hidden;ImageKey=endpoint.ImageKey??endpoint.Id;
        Interface=endpoint.Interface;
        PortOptions=input?MidiPorts.Inputs(endpoint.Port):MidiPorts.Outputs(endpoint.Port);
        Original=endpoint.Copy();Name=endpoint.Name;Port=endpoint.Port;ChannelText=endpoint.Channel.ToString();
        Available=endpoint.Available;Routed=route!=null;OctaveText=(route?.Octaves??0).ToString();
    }
}
public partial class SettingsWindow : Window
{
    private readonly RouterViewModel model;
    private readonly Source? editingSource;
    public List<EndpointDraft> Drafts { get; }
    public SettingsWindow(RouterViewModel owner, string? receiverId=null, bool adding=false)
    {
        model=owner;Drafts=model.State.Endpoints.Where(e=>receiverId==null||e.Id==receiverId).Select(e=>new EndpointDraft(e,model.ActiveRoute(e.Id))).ToList();
        if(adding)Drafts=new(){new EndpointDraft(new(){Id=Guid.NewGuid().ToString("N"),Name="",Port="",Group="ON TOP",Available=false,Tags=new()},null)};
        foreach(var draft in Drafts){
            if(!string.IsNullOrWhiteSpace(model.State.MainInterface)&&!draft.Interfaces.Contains(model.State.MainInterface))draft.Interfaces.Add(model.State.MainInterface);
            if(!draft.Interfaces.Contains(draft.Interface))draft.Interfaces.Add(draft.Interface);
            if(adding)draft.Interface=model.State.MainInterface;
        }
        InitializeComponent();DataContext=Drafts;
        if(adding)Title="Dodaj odbiorcę";
        if(receiverId!=null)Title="Ustawienia odbiorcy — "+Drafts.Single().Name;
    }
    public SettingsWindow(RouterViewModel owner,Source source){
        model=owner;editingSource=source;
        var endpoint=new Endpoint{Id=source.Id,Name=source.Name,Tags=source.Tags.ToList(),ImageKey=source.ImagePath??"",Interface=source.Interface,Port=source.Port,Channel=source.Channel};
        var draft=new EndpointDraft(endpoint,null,true);
        if(!draft.Interfaces.Contains(source.Interface))draft.Interfaces.Add(source.Interface);
        if(!draft.Interfaces.Contains(model.State.MainInterface))draft.Interfaces.Add(model.State.MainInterface);
        Drafts=new(){draft};
        InitializeComponent();DataContext=Drafts;Title="Ustawienia źródła";
    }
    private void BrowseArtwork(object sender,RoutedEventArgs e)
    {
        if(sender is FrameworkElement { DataContext: EndpointDraft draft }) {
            try { var file=ArtworkPicker.Pick();if(file!=null){draft.ImageKey=file;} }
            catch(Exception ex){Error.Text=ex.Message;}
        }
    }
    private void ClearArtwork(object sender,RoutedEventArgs e) { if(sender is FrameworkElement {DataContext: EndpointDraft draft})draft.ImageKey=""; }
    private void SaveSettings(object sender,RoutedEventArgs e)
    {
        if(editingSource!=null){
            var draft=Drafts[0];
            if(!int.TryParse(draft.ChannelText,out var channel)||channel<1||channel>16){Error.Text="Kanał musi być liczbą od 1 do 16.";return;}
            try{
                model.UpdateSource(editingSource,draft.Name,ArtworkPicker.UriFor(draft.ImageKey),draft.TagsText.Split(','),draft.Interface,draft.Port??"",channel);
                DialogResult=true;
            }catch(ArgumentException ex){Error.Text=ex.Message;}
            return;
        }
        var endpoints=new List<Endpoint>();var octaves=new Dictionary<string,int>();
        foreach(var d in Drafts)
        {
            if(!int.TryParse(d.ChannelText,out var ch)||ch<1||ch>16) {Error.Text="Kanał musi być liczbą od 1 do 16.";return;}
            var endpoint=d.Original.Copy();endpoint.Name=d.Name.Trim();endpoint.Port=(d.Port??"").Trim();endpoint.Interface=d.Interface;endpoint.Channel=ch;endpoint.Available=d.Available;endpoint.Hidden=d.Hidden;endpoint.ImageKey=d.ImageKey;
            endpoint.Tags=d.TagsText.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            endpoints.Add(endpoint);
        }
        try {model.CommitSettings(model.State.Endpoints.Where(original=>!endpoints.Any(edited=>edited.Id==original.Id)).Concat(endpoints),octaves);DialogResult=true;}
        catch(ArgumentException ex) {Error.Text=ex.Message;}
    }
}








