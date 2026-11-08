using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.IO;
namespace MidiRouter;

public abstract class Observable : INotifyPropertyChanged
{
    public bool IsRenaming { get; private set; }
    public string DraftName { get; set; }="";
    public void BeginRename(string name) { DraftName=name;Changed(nameof(DraftName));IsRenaming=true;Changed(nameof(IsRenaming)); }
    public void CancelRename() {IsRenaming=false;Changed(nameof(IsRenaming));}
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Changed([CallerMemberName] string? name=null) => PropertyChanged?.Invoke(this,new(name));
}
public sealed class Command : ICommand
{
    private readonly Action<object?> execute;
    public Command(Action<object?> action) => execute=action;
    public bool CanExecute(object? p)=>true;
    public void Execute(object? p)=>execute(p);
    public event EventHandler? CanExecuteChanged { add {} remove {} }
}
public sealed class SourceDetails
{
    public string Type {get;set;}="Inne";
    public int? KeyCount {get;set;}
    public int? FirstNote {get;set;}
    public SourceDetails Copy()=>new(){Type=Type,KeyCount=KeyCount,FirstNote=FirstNote};
    public static SourceDetails Detect(string name) {
        var compact=new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if(compact.Contains("seaboard2")||compact.Contains("seaboardrise2"))
            return new(){Type="Klawiatura",KeyCount=49};
        foreach(int count in new[]{49,61,88})
            if(compact.Contains("impactgxp"+count))return new(){Type="Klawiatura",KeyCount=count};
        return new();
    }
}
public sealed class Source : Observable
{
    private bool solo;
    public bool IsSolo {get=>solo;set{solo=value;Changed();}}
    private int octaves;
    public int Octaves {get=>octaves;set{octaves=value;Changed();Changed(nameof(OctaveLabel));}}
    public string OctaveLabel=>$"{Octaves:+0;-0;0} oct";
    public SourceDetails Details {get;set;}=new();
    public string Id { get; init; } = "";
    private string name=""; public string Name { get=>name; set {name=value;Changed();} }
    private string? imagePath; public string? ImagePath { get=>imagePath; set {imagePath=value;Changed();} }
    public string Interface {get;set;}="Windows MIDI"; public string Port {get;set;}=""; public int Channel {get;set;}=1;
    public List<string> Tags {get;set;}=new();
    private bool selected;
    public bool IsSelected { get=>selected; set { selected=value; Changed(); } }
}
public sealed class Endpoint
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string Interface { get; set; } = "Windows MIDI";
    public string Port { get; set; } = "";
    public int Channel { get; set; } = 1;
    public bool Available { get; set; } = true;
    public bool Hidden { get; set; }
    public List<string>? Tags { get; set; }
    public string? ImageKey { get; set; }
    public Endpoint Copy() { var copy=(Endpoint)MemberwiseClone();copy.Tags=Tags?.ToList();return copy; }
}
public sealed class SplitGroup {
 public static string[] Types => new[]{"Zakres klawiszy","Velocity","Modwheel"}
  .Concat(new[]{"Pitch Bend","Channel Aftertouch","Poly Aftertouch","Program Change","Release Velocity","Kanał MIDI"})
  .Concat(Enumerable.Range(0,128).Where(n=>n!=1).Select(n=>"CC "+n.ToString("000"))).ToArray();
 public string Type {get;set;}="Zakres klawiszy";
 public Dictionary<string,int[]> ParameterRanges {get;set;}=new();
 public int ParameterMax => Type=="Pitch Bend"?16383:Type=="Kanał MIDI"?16:127;
 public int ParameterMin => Type=="Kanał MIDI"?1:0;
 public int[] ParameterRange => ParameterRanges.TryGetValue(Type,out var range)?range:new[]{ParameterMin,ParameterMax};
 public string Name {get;set;}="Grupa";
 public int Low {get;set;}=0; public int High {get;set;}=127;
 public int VelocityLow {get;set;}=1;public int VelocityHigh {get;set;}=127;
 public int ModLow {get;set;}=0;public int ModHigh {get;set;}=127;
 public List<Route> Routes {get;set;}=new();
 public bool Accepts(int note,int velocity,int mod,IReadOnlyDictionary<string,int>? parameters=null){
  if(Type=="Zakres klawiszy")return note>=Low&&note<=High;
  if(Type=="Velocity")return velocity>=VelocityLow&&velocity<=VelocityHigh;
  if(Type=="Modwheel")return mod>=ModLow&&mod<=ModHigh;
  if(parameters==null||!parameters.TryGetValue(Type,out var value))return false;
  return value>=ParameterRange[0]&&value<=ParameterRange[1];
 }
}
public sealed class SplitProfile {
 public string Id {get;set;}=Guid.NewGuid().ToString("N"); public string Name {get;set;}="Nowy split";
 public List<SplitGroup> Groups {get;set;}=new(){new(){Name="Warstwa 1",High=59},new(){Name="Warstwa 2",Low=60}};
}
public sealed class Route
{
    public string SourceId { get; set; } = "";
    public string ReceiverId { get; set; } = "";
    public int Octaves { get; set; }
}
public sealed class RoutingCard : Observable
{
    private bool pinned=true;
    public bool IsPinned {get=>pinned;set{pinned=value;Changed();}}
    public string Id { get; set; } = "studio";
    private string name="Studio";
    public string Name {get=>name;set{name=value;Changed();}}
    private bool active;
    [System.Text.Json.Serialization.JsonIgnore] public bool IsActive {get=>active;set{active=value;Changed();}}
    public Dictionary<string,string> SplitIds {get;set;}=new();
    public Dictionary<string,int> SourceOctaves {get;set;}=new();
    public List<Route> Routes { get; set; } = new();
}
public sealed class SourceSection { public string Name {get;init;}=""; public List<Source> Sources {get;init;}=new(); public bool IsExpanded {get;set;}=true; }
public sealed class SourceConfig { public SourceDetails? Details {get;set;} public string Interface {get;set;}="Windows MIDI"; public string Port {get;set;}=""; public int Channel {get;set;}=1; public List<string>? Tags {get;set;} public string Name { get; set; }=""; public string? ImagePath { get; set; } }
public sealed class StudioState
{
    public Dictionary<string,List<SplitProfile>> SplitProfiles {get;set;}=new();
    public Dictionary<string,string> DefaultSplitIds {get;set;}=new();
    public List<string> SourceSectionOrder {get;set;}=new();
    public List<string> VisibleSourceIds {get;set;}=new(){"a","b"};
    public Dictionary<string,SourceConfig> SourceSettings { get; set; } = new();
    public List<string> SectionOrder { get; set; } = new(){"RACK","KEYBOARDS","ON TOP"};
    public int Version { get; set; } = 1;
    public string SelectedSourceId { get; set; } = "a";
    public string MainInterface {get;set;}="Windows MIDI";
    public string ActiveCardId { get; set; } = "studio";
    public List<Endpoint> Endpoints { get; set; } = new();
    public List<RoutingCard> Cards { get; set; } = new();
}
public interface IMidiBackend
{
    string Description { get; }
    void ApplyRoutes(IEnumerable<Route> routes);
    void Panic();
}
public sealed class MockMidiBackend : IMidiBackend
{
    public string Description => "Tryb makiety · MIDI symulowane";
    public int PanicCount { get; private set; }
    public void ApplyRoutes(IEnumerable<Route> routes) { /* No hardware I/O in mockup. */ }
    public void Panic() => PanicCount++;
}
public sealed class Receiver : Observable
{
    private readonly RouterViewModel owner;
    internal Endpoint Endpoint { get; }
    public Receiver(RouterViewModel model,Endpoint endpoint) { owner=model; Endpoint=endpoint; }
    public string Name => Endpoint.Name;
    public string? ImagePath => System.IO.Path.IsPathRooted(Endpoint.ImageKey??"") ? Endpoint.ImageKey : (Endpoint.ImageKey??Endpoint.Id) switch {
        "nord" => "pack://application:,,,/Assets/nord.png",
        "roland" => "pack://application:,,,/Assets/jd800.png",
        "jd800" => "pack://application:,,,/Assets/jd800.png",
        "roli" => "pack://application:,,,/Assets/roli.png",
        "nektar" => "pack://application:,,,/Assets/nektar.png",
        "virus" => "pack://application:,,,/Assets/virus.png",
        "cz5000" => "pack://application:,,,/Assets/cz5000.png",
        "neutron" => "pack://application:,,,/Assets/neutron.png",
        "jp" => "pack://application:,,,/Assets/jp8080.png",
        "kobol" => "pack://application:,,,/Assets/kobol.png",
        "model-d" => "pack://application:,,,/Assets/model-d.png",
        "system-8" => "pack://application:,,,/Assets/system-8.png",
        _ => null
    };
    private System.Windows.Media.Imaging.BitmapSource? picture;
    private bool? pictureUnavailable;
    public System.Windows.Media.Imaging.BitmapSource? Picture {
        get {
            if(ImagePath==null)return null;
            if(picture!=null && pictureUnavailable==IsUnavailable)return picture;
            var original=new System.Windows.Media.Imaging.BitmapImage(new Uri(ImagePath));
            if(!IsUnavailable)picture=original;
            else {
                var rgba=new System.Windows.Media.Imaging.FormatConvertedBitmap(original,System.Windows.Media.PixelFormats.Bgra32,null,0);
                int stride=rgba.PixelWidth*4;
                var pixels=new byte[stride*rgba.PixelHeight];
                rgba.CopyPixels(pixels,stride,0);
                for(int i=0;i<pixels.Length;i+=4) {
                    byte gray=(byte)((pixels[i]*114+pixels[i+1]*587+pixels[i+2]*299)/1000);
                    pixels[i]=pixels[i+1]=pixels[i+2]=gray;
                }
                picture=System.Windows.Media.Imaging.BitmapSource.Create(rgba.PixelWidth,rgba.PixelHeight,96,96,System.Windows.Media.PixelFormats.Bgra32,null,pixels,stride);
            }
            picture.Freeze();pictureUnavailable=IsUnavailable;return picture;
        }
    }
    public bool IsUnavailable => !Endpoint.Available || string.IsNullOrWhiteSpace(Endpoint.Port);
    public bool IsSolo => owner.SoloReceiverId==Endpoint.Id;
    public bool IsRouted => owner.ActiveRoute(Endpoint.Id)!=null;
    public string OctaveLabel => $"{(owner.ActiveRoute(Endpoint.Id)?.Octaves??0):+0;-0;0} oct";
    public string Hint => IsUnavailable ? "Niedostępny · możesz przypisać teraz i włączyć później" : $"{Endpoint.Port} · CH {Endpoint.Channel} · kliknij, aby zmienić przypisanie";
    public void Refresh() { Changed(nameof(Name)); Changed(nameof(IsUnavailable)); Changed(nameof(Picture)); Changed(nameof(IsRouted)); Changed(nameof(IsSolo)); Changed(nameof(OctaveLabel)); Changed(nameof(Hint)); }
}
public sealed class RouterViewModel : Observable
{
    public Source[] Sources { get; private set; } = {
        new() { Id="a", Name="ROLI Seaboard 2", Tags=new(){"Klawiatury"}, Details=SourceDetails.Detect("ROLI Seaboard 2"), ImagePath="pack://application:,,,/Assets/roli.png" }, new() { Id="b", Name="Nektar Impact GXP61", Tags=new(){"Klawiatury"}, Details=SourceDetails.Detect("Nektar Impact GXP61"), ImagePath="pack://application:,,,/Assets/nektar.png" },
        new() { Id="other", Name="Źródło 3" },
        new() { Id="extra4", Name="Źródło 4" },
        new() { Id="extra5", Name="Źródło 5" },
        new() { Id="extra6", Name="Źródło 6" },
        new() { Id="extra7", Name="Źródło 7" },
        new() { Id="extra8", Name="Źródło 8" },
        new() { Id="extra9", Name="Źródło 9" },
        new() { Id="extra10", Name="Źródło 10" },
        new() { Id="extra11", Name="Źródło 11" },
        new() { Id="extra12", Name="Źródło 12" }

    };
    public IEnumerable<Source> PrimarySources => Sources.Where(s=>State.VisibleSourceIds.Contains(s.Id));
    private string searchText="";
    public string SearchText {
        get=>searchText;
        set {
            if(searchText==value)return;
            searchText=value??"";Changed();
            RefreshSourceSections();RefreshFilter();
        }
    }
    private bool MatchesSearch(string name,IEnumerable<string>? tags) =>
        string.IsNullOrWhiteSpace(SearchText) ||
        SearchText.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).All(word=>
            name.Contains(word,StringComparison.CurrentCultureIgnoreCase) ||
            (tags?.Any(tag=>tag.Contains(word,StringComparison.CurrentCultureIgnoreCase))??false));
    private readonly Dictionary<string,bool> sourceExpansion=new(StringComparer.OrdinalIgnoreCase);
    public List<SourceSection> SourceSections {get;private set;}=new();
    private void RefreshSourceSections(){
        foreach(var section in SourceSections)sourceExpansion[section.Name]=section.IsExpanded;
        var expanded=sourceExpansion;
        var matchingSources=PrimarySources.Where(s=>MatchesSearch(s.Name,s.Tags)).ToList();
        var tags=PrimarySources.SelectMany(s=>s.Tags.Count==0?new[]{"Bez tagów"}:s.Tags.AsEnumerable()).Distinct(StringComparer.OrdinalIgnoreCase);
        SourceSections=tags.Select(tag=>new SourceSection{Name=tag,Sources=matchingSources.Where(s=>tag=="Bez tagów"?s.Tags.Count==0:s.Tags.Contains(tag,StringComparer.OrdinalIgnoreCase)).ToList(),IsExpanded=!expanded.TryGetValue(tag,out var value)||value})
            .Where(s=>s.Sources.Count>0).OrderBy(s=>{int index=State.SourceSectionOrder.FindIndex(t=>t.Equals(s.Name,StringComparison.OrdinalIgnoreCase));return index<0?int.MaxValue:index;}).ToList();
        Changed(nameof(SourceSections));
    }
    public void ReorderSourceSection(string name,int index){
        var ordered=SourceSections.Select(s=>s.Name).ToList();if(!ordered.Remove(name))return;
        ordered.Insert(Math.Clamp(index,0,ordered.Count),name);State.SourceSectionOrder=ordered;
        RefreshSourceSections();Save();
    }
    public IEnumerable<Source> OtherSources => Sources.Skip(2);
    public List<Receiver> Receivers { get; private set; } = new();
    public IEnumerable<Receiver> HiddenReceivers => Receivers.Where(r=>r.Endpoint.Hidden && MatchesSearch(r.Name,r.Endpoint.Tags));
    public IEnumerable<Receiver> Rack => Receivers.Where(r=>Matches(r.Endpoint) && r.Endpoint.Group=="RACK");
    public IEnumerable<Receiver> Keyboards => Receivers.Where(r=>Matches(r.Endpoint) && r.Endpoint.Group=="KEYBOARDS");
    public IEnumerable<Receiver> OnTop => Receivers.Where(r=>Matches(r.Endpoint) && r.Endpoint.Group=="ON TOP");
    public HashSet<string> SelectedTags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool ShowHidden { get; set; }
    public IEnumerable<string> AllTags => State.Endpoints.SelectMany(e=>e.Tags??new()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t=>t);
    public string TagCaption => SelectedTags.Count==0 ? "Tagi" : "Tagi: "+string.Join(", ",SelectedTags);
    private bool Matches(Endpoint e)=>MatchesSearch(e.Name,e.Tags)&&(ShowHidden||!e.Hidden)&&(SelectedTags.Count==0||(e.Tags??new()).Any(t=>SelectedTags.Contains(t)));
    public void RefreshFilter() { Changed(nameof(Rack));Changed(nameof(Keyboards));Changed(nameof(OnTop));Changed(nameof(HiddenReceivers));Changed(nameof(TagCaption));Changed(nameof(HasRack));Changed(nameof(HasKeyboards));Changed(nameof(HasOnTop));Changed(nameof(HasHiddenReceivers)); }
    public bool HasRack => Rack.Any();
    public bool HasKeyboards => Keyboards.Any();
    public bool HasOnTop => OnTop.Any();
    public bool HasHiddenReceivers => HiddenReceivers.Any();
    public StudioState State { get; private set; }
    public IMidiBackend Backend { get; }
    private readonly string statePath;
    private bool preserveInvalidFile;
    private string status="";
    public string Status { get=>status; private set {status=value;Changed();} }
    public void RestoreClickRoutes(List<Route> routes){
        if(EditingGroup!=null)EditingGroup.Routes=routes;else ActiveCard.Routes=routes;Refresh();ApplyCurrentRoutes();Save();
    }
    public SplitProfile? EditingSplit {get;private set;}
    public SplitGroup? EditingGroup {get;private set;}
    public List<Route> EditingRoutes=>EditingGroup?.Routes??ActiveCard.Routes;
    public void EditSplit(Source source,SplitProfile? profile=null){
        ChooseSource(source,true);
        if(!State.SplitProfiles.TryGetValue(source.Id,out var profiles))State.SplitProfiles[source.Id]=profiles=new();
        if(profile==null){profile=profiles.FirstOrDefault();if(profile==null){profile=new();profiles.Add(profile);}}
        EditingSplit=profile;EditingGroup=profile.Groups.FirstOrDefault();Refresh();Save();
    }
    public void SelectSplitGroup(SplitGroup group){EditingGroup=group;Refresh();}
    public void CloseSplit(){EditingSplit=null;EditingGroup=null;Refresh();ApplyCurrentRoutes();Save();}
    public bool ToggleDefaultSplit(Source source){
        if(!State.DefaultSplitIds.TryGetValue(source.Id,out var id)||!State.SplitProfiles.TryGetValue(source.Id,out var list)||!list.Any(p=>p.Id==id))return false;
        if(ActiveCard.SplitIds.GetValueOrDefault(source.Id)==id)ActiveCard.SplitIds.Remove(source.Id);else ActiveCard.SplitIds[source.Id]=id;
        ApplyCurrentRoutes();Save();return true;
    }
    public IEnumerable<Route> RoutesForNote(string sourceId,int note,int velocity,int modwheel,IReadOnlyDictionary<string,int>? parameters=null){
        if(SoloReceiverId!=null)return PlaybackRoutes.Where(r=>r.SourceId==sourceId);
        if(ActiveCard.SplitIds.TryGetValue(sourceId,out var id)&&State.SplitProfiles.TryGetValue(sourceId,out var profiles)){
            var profile=profiles.FirstOrDefault(p=>p.Id==id);
            if(profile!=null)return profile.Groups.Where(g=>g.Accepts(note,velocity,modwheel,parameters)).SelectMany(g=>g.Routes).ToList();
        }
        return ActiveCard.Routes.Where(r=>r.SourceId==sourceId);
    }
    public string? SoloReceiverId {get;private set;}
    public void ToggleSolo(Receiver receiver){
        if(!Receivers.Contains(receiver))return;
        SoloReceiverId=SoloReceiverId==receiver.Endpoint.Id?null:receiver.Endpoint.Id;
        Refresh();ApplyCurrentRoutes();
    }
    public IEnumerable<Route> PlaybackRoutes => SoloReceiverId==null?ActiveCard.Routes:
        new[]{new Route{SourceId=State.SelectedSourceId,ReceiverId=SoloReceiverId,Octaves=ActiveRoute(SoloReceiverId)?.Octaves??0}};
    private void ApplyCurrentRoutes()=>Backend.ApplyRoutes(PlaybackRoutes);
    public Command SelectSource { get; }
    public Command ToggleReceiver { get; }
    public Command Panic { get; }
    private bool extendedSelection;
    private bool controlSelection;
    public bool ExtendedSelection => extendedSelection||controlSelection;
    public void SetControlSelection(bool value){
        if(controlSelection==value)return;
        controlSelection=value;Changed(nameof(ExtendedSelection));
    }
    public string SelectionLabel => "Wybrana";
    public Command OctaveDown { get; }
    public Command OctaveUp { get; }
    public void ChooseSource(Source source, bool extended)
    {
        if(!Sources.Contains(source))return;
        SoloReceiverId=null;
        State.SelectedSourceId=source.Id;
        extendedSelection=extended;
        foreach(var s in Sources)s.IsSelected=s==source;
        Changed(nameof(ExtendedSelection));Changed(nameof(SelectionLabel));
        Status=extended ? "Wybór wielu odbiorców · klikaj, aby dodawać lub usuwać." : "Wybór jednego odbiorcy · kliknięcie odbiorcy zastępuje poprzednie przypisania.";
        Refresh();ApplyCurrentRoutes();Save();
    }
    private void ChangeOctave(object? parameter,int delta)
    {
        if(parameter is Source source){
            source.Octaves=Math.Clamp(source.Octaves+delta,-4,4);
            ActiveCard.SourceOctaves[source.Id]=source.Octaves;Save();return;
        }
        if(parameter is not Receiver receiver)return;
        if(!receiver.IsRouted){Toggle(receiver);return;}
        var route=ActiveRoute(receiver.Endpoint.Id)!;
        SetOctaves(receiver.Endpoint.Id,Math.Clamp(route.Octaves+delta,-4,4));
        ApplyCurrentRoutes();Save();
    }
    public List<RoutingCard> Cards => State.Cards;
    public IEnumerable<RoutingCard> PinnedCards => State.Cards.Where(c=>c.IsPinned);
    public void DeleteCard(RoutingCard card){
        if(!State.Cards.Remove(card))return;
        if(State.Cards.Count==0)State.Cards.Add(new RoutingCard{Id=Guid.NewGuid().ToString("N"),Name="Nowa scena"});
        Changed(nameof(Cards));Changed(nameof(PinnedCards));
        if(State.ActiveCardId==card.Id)ActivateCard(State.Cards[0].Id);
        Save();
    }
    public void SetCardPinned(RoutingCard card,bool value){
        if(!State.Cards.Contains(card))return;
        card.IsPinned=value;Changed(nameof(PinnedCards));Save();
    }
    public Command SelectCard => new(p=>{if(p is RoutingCard card)ActivateCard(card.Id);});
    public void ActivateCard(string id){
        if(!State.Cards.Any(c=>c.Id==id))return;
        SoloReceiverId=null;EditingSplit=null;EditingGroup=null;
        State.ActiveCardId=id;foreach(var c in State.Cards)c.IsActive=c.Id==id;
        Changed(nameof(ActiveCard));Refresh();ApplyCurrentRoutes();Save();
    }
    public RoutingCard CreateCard(bool copy){
        var card=new RoutingCard{Id=Guid.NewGuid().ToString("N"),Name="Karta "+(State.Cards.Count+1)};
        if(copy)card.SplitIds=new(ActiveCard.SplitIds);
        if(copy)card.SourceOctaves=new(ActiveCard.SourceOctaves);
        if(copy)card.Routes=ActiveCard.Routes.Select(r=>new Route{SourceId=r.SourceId,ReceiverId=r.ReceiverId,Octaves=r.Octaves}).ToList();
        State.Cards.Add(card);Changed(nameof(Cards));Changed(nameof(PinnedCards));ActivateCard(card.Id);return card;
    }
    public bool SaveCard(){
        if(string.IsNullOrWhiteSpace(ActiveCard.Name)){Status="Wpisz nazwę karty.";return false;}
        if(!Save())return false;Status="Zapisano kartę: "+ActiveCard.Name;return true;
    }
    public void SetMainInterface(string value){State.MainInterface=value;Save();}
    public RoutingCard ActiveCard => State.Cards.Single(c=>c.Id==State.ActiveCardId);
    public RouterViewModel(string? path=null, IMidiBackend? backend=null)
    {
        statePath=path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MidiRouter","studio.json");
        Backend=backend??new MockMidiBackend();
        State=Load();
        foreach(var id in State.VisibleSourceIds)
            if(!Sources.Any(s=>s.Id==id)&&State.SourceSettings.TryGetValue(id,out var config))
                Sources=Sources.Append(new Source{Id=id,Name=config.Name,ImagePath=config.ImagePath}).ToArray();
        foreach(var c in State.Cards)c.IsActive=c.Id==State.ActiveCardId;
        if(!State.VisibleSourceIds.Contains(State.SelectedSourceId))State.SelectedSourceId="a";
        foreach(var s in Sources)if(State.SourceSettings.TryGetValue(s.Id,out var saved)){s.Name=saved.Name;s.Details=saved.Details?.Copy()??SourceDetails.Detect(saved.Name);s.ImagePath=saved.ImagePath;s.Interface=saved.Interface;s.Port=saved.Port;s.Channel=saved.Channel;if(saved.Tags!=null)s.Tags=saved.Tags.ToList();}
        foreach(var endpoint in new Endpoint[] {
            new(){Id="kobol",Name="Kobol",Group="RACK",Port="OUT 6"},
            new(){Id="model-d",Name="Model D",Group="RACK",Port="OUT 7"},
            new(){Id="system-8",Name="Roland System-8",Group="KEYBOARDS",Port="OUT 8"}
        })
            if(!State.Endpoints.Any(e=>e.Id==endpoint.Id))State.Endpoints.Add(endpoint);
        foreach(var endpoint in new Endpoint[] {
            new(){Id="cz5000",Name="Casio CZ-5000",Group="KEYBOARDS",Port="",Available=false},
            new(){Id="neutron",Name="Neutron",Group="RACK",Port="",Available=false}
        }) if(!State.Endpoints.Any(e=>e.Id==endpoint.Id))State.Endpoints.Add(endpoint);
        foreach(var endpoint in State.Endpoints)
            endpoint.Tags??=new(){endpoint.Group=="KEYBOARDS"?"KB":"Rack"};
        RefreshSourceSections();
        BuildReceivers();
        foreach(var s in Sources) s.IsSelected=s.Id==State.SelectedSourceId;
        foreach(var source in Sources)source.Octaves=ActiveCard.SourceOctaves.GetValueOrDefault(source.Id);
        SelectSource=new(p=> {if(p is Source s)ChooseSource(s,false);});
        OctaveDown=new(p=>ChangeOctave(p,-1)); OctaveUp=new(p=>ChangeOctave(p,1));
        ToggleReceiver=new(p=> {if(p is Receiver r)Toggle(r);});
        Panic=new(_=> {Backend.Panic();Status="PANIC · symulacja zatrzymania wszystkich nut. Przypisania zachowane.";});
        if(string.IsNullOrEmpty(Status)) Status=Backend.Description;
    }
    public Route? ActiveRoute(string id)=>EditingRoutes.FirstOrDefault(r=>r.SourceId==State.SelectedSourceId&&r.ReceiverId==id);
    private void Toggle(Receiver receiver)
    {

        var route=ActiveRoute(receiver.Endpoint.Id);
        if(!ExtendedSelection)
            EditingRoutes.RemoveAll(r=>r.SourceId==State.SelectedSourceId&&r!=route);
        if(route==null)EditingRoutes.Add(new(){SourceId=State.SelectedSourceId,ReceiverId=receiver.Endpoint.Id});
        else EditingRoutes.Remove(route);
        ApplyCurrentRoutes();
        Refresh();Save();
    }
    public void UpdateSource(Source source,string name,string? image,IEnumerable<string>? tags=null,string? midiInterface=null,string? port=null,int? channel=null,SourceDetails? details=null)
    {
        if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Wpisz nazwę źródła.");
        if(!Sources.Contains(source)){
            Sources=Sources.Append(source).ToArray();
            if(!State.VisibleSourceIds.Contains(source.Id))State.VisibleSourceIds.Add(source.Id);
        }
        if(details!=null)source.Details=details.Copy();
        source.Name=name.Trim();source.ImagePath=image;
        Changed(nameof(PrimarySources));
        if(tags!=null)source.Tags=tags.Select(t=>t.Trim()).Where(t=>t.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if(midiInterface!=null)source.Interface=midiInterface;if(port!=null)source.Port=port;if(channel!=null)source.Channel=channel.Value;
        State.SourceSettings[source.Id]=new(){Details=source.Details.Copy(),Name=source.Name,ImagePath=image,Tags=source.Tags.ToList(),Interface=source.Interface,Port=source.Port,Channel=source.Channel};RefreshSourceSections();Save();
    }
    public void SetOctaves(string id,int value)
    {
        if(value < -4 || value > 4)throw new ArgumentOutOfRangeException(nameof(value));
        var route=ActiveRoute(id);
        if(route!=null)route.Octaves=value;
        Refresh();
    }
    public void CommitSettings(IEnumerable<Endpoint> endpoints,Dictionary<string,int> octaves)
    {
        var copies=endpoints.Select(e=>e.Copy()).ToList();
        if(copies.Any(e=>string.IsNullOrWhiteSpace(e.Name)||e.Channel<1||e.Channel>16))throw new ArgumentException("Wpisz nazwę oraz kanał od 1 do 16. Port może pozostać pusty.");
        if(octaves.Values.Any(v=>v < -4 || v > 4))throw new ArgumentException("Transpozycja: od −4 do +4 oktaw.");
        State.Endpoints=copies;
        foreach(var pair in octaves)SetOctaves(pair.Key,pair.Value);
        RefreshSourceSections();
        if(SoloReceiverId!=null&&!State.Endpoints.Any(e=>e.Id==SoloReceiverId&&e.Available&&!string.IsNullOrWhiteSpace(e.Port)))SoloReceiverId=null;
        BuildReceivers();Refresh();ApplyCurrentRoutes();Save();
    }
    private void BuildReceivers()
    {
        Receivers=State.Endpoints.Select(e=>new Receiver(this,e)).ToList();
        RefreshFilter();
    }
    private void Refresh() {foreach(var source in Sources){source.Octaves=ActiveCard.SourceOctaves.GetValueOrDefault(source.Id);source.IsSolo=SoloReceiverId!=null&&source.Id==State.SelectedSourceId;}foreach(var r in Receivers)r.Refresh();}
    private static StudioState Defaults()=>new()
    {
        Endpoints=new() {
            new(){Id="jp",Name="JP-8080",Group="RACK",Port="OUT 1"},
            new(){Id="virus",Name="Virus TI",Group="RACK",Port="OUT 2"},
            new(){Id="nord",Name="Nord Rack",Group="RACK",Port="OUT 3",Available=false},
            new(){Id="roland",Name="Roland JD-800",Group="KEYBOARDS",Port="OUT 4"},
            new(){Id="top",Name="Pozostałe instrumenty",Group="ON TOP",Port="OUT 5"}
        },
        Cards=new(){new(){Routes=new(){new(){SourceId="a",ReceiverId="jp"},new(){SourceId="a",ReceiverId="virus"}}}}
    };
    private StudioState Load()
    {
        if(!File.Exists(statePath))return Defaults();
        try {
            var state=JsonSerializer.Deserialize<StudioState>(File.ReadAllText(statePath))??throw new InvalidDataException();
            if(state.SelectedSourceId=="roli")state.SelectedSourceId="a";
            if(state.Cards!=null)foreach(var card in state.Cards.Where(c=>c?.Routes!=null)) {
                foreach(var route in card.Routes.Where(r=>r!=null&&r.SourceId=="roli"))route.SourceId="a";
                card.Routes=card.Routes.GroupBy(r=>(r.SourceId,r.ReceiverId)).Select(g=>g.First()).ToList();
            }
            if(state.Version!=1 || state.Endpoints==null || state.Cards==null ||
               state.Endpoints.Any(e=>e==null||string.IsNullOrWhiteSpace(e.Id)||string.IsNullOrWhiteSpace(e.Name)||e.Channel<1||e.Channel>16||!new[]{"RACK","KEYBOARDS","ON TOP"}.Contains(e.Group)) ||
               state.Endpoints.Select(e=>e.Id).Distinct().Count()!=state.Endpoints.Count ||
               state.Cards.Count==0 || state.Cards.Any(c=>c==null||string.IsNullOrWhiteSpace(c.Id)||c.Routes==null) ||
               state.Cards.Select(c=>c.Id).Distinct().Count()!=state.Cards.Count ||
               !state.Cards.Any(c=>c.Id==state.ActiveCardId) || (!Sources.Any(s=>s.Id==state.SelectedSourceId)&&!state.SourceSettings.ContainsKey(state.SelectedSourceId)) ||
               state.Cards.Any(c=>c.Routes.Any(r=>r==null||(!Sources.Any(s=>s.Id==r.SourceId)&&!state.SourceSettings.ContainsKey(r.SourceId))||!state.Endpoints.Any(e=>e.Id==r.ReceiverId)||r.Octaves < -4||r.Octaves>4) ||
                   c.Routes.Select(r=>(r.SourceId,r.ReceiverId)).Distinct().Count()!=c.Routes.Count))
                throw new InvalidDataException();
            return state;
        }
        catch(Exception ex) when(ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
        {
            preserveInvalidFile=true;Status="Nie można odczytać konfiguracji. Użyto makiety; oryginalny plik pozostaje zachowany.";
            return Defaults();
        }
    }
    public bool Save()
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            if(preserveInvalidFile&&File.Exists(statePath)) {File.Copy(statePath,statePath+".invalid-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"));preserveInvalidFile=false;}
            File.WriteAllText(statePath+".tmp",JsonSerializer.Serialize(State,new JsonSerializerOptions{WriteIndented=true}));
            File.Move(statePath+".tmp",statePath,true);
            return true;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {Status="Nie udało się zapisać konfiguracji: "+ex.Message;return false;}
    }
}




















