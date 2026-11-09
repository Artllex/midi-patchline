using System.IO;
namespace MidiRouter;
public static class ModelChecks
{
    private static void Check(bool value,string message) {if(!value)throw new Exception(message);}
    public static void Run()
    {
        var dir=Path.Combine(Path.GetTempPath(),"MidiRouter-check-"+Guid.NewGuid().ToString("N"));
        var file=Path.Combine(dir,"studio.json");
        Directory.CreateDirectory(dir);
        var backend=new MockMidiBackend();
        var model=new RouterViewModel(file,backend);
        Check(model.Receivers.Count(r=>r.IsRouted)==2,"Initial reference routing");
        Check(SplitEditor.TryNote("C4",out var middle)&&middle==60,"Typed middle C");
        Check(SplitEditor.TryNote("C-1",out var bottom)&&bottom==0&&SplitEditor.TryNote("G9",out var upper)&&upper==127,"MIDI note limits");
        Check(SplitEditor.TryNote("c#4",out var sharp)&&sharp==61&&!SplitEditor.TryNote("C10",out _),"Typed accidental and invalid note");
        Check(SplitGroup.Types.Take(3).SequenceEqual(new[]{"Zakres klawiszy","Velocity","Modwheel"}),"Preferred MIDI parameters first");
        Check(SplitGroup.Types.Count(t=>t.StartsWith("CC "))==127,"All CC numbers with CC1 represented by Modwheel");
        var parameterGroup=new SplitGroup{Type="CC 074"};
        parameterGroup.ParameterRanges["CC 074"]=new[]{64,127};
        Check(parameterGroup.Accepts(60,100,0,new Dictionary<string,int>{{"CC 074",100}}),"CC group accepts matching values");
        Check(!parameterGroup.Accepts(60,100,0)&&!parameterGroup.Accepts(60,100,0,new Dictionary<string,int>{{"CC 074",20}}),"Missing or outside CC does not match");
        parameterGroup.Type="Pitch Bend";
        Check(parameterGroup.ParameterRange[1]==16383,"Pitch Bend 14 bit range");
        var splits=new RouterViewModel(Path.Combine(dir,"splits.json"));
        var splitSource=splits.Sources[0];
        splits.EditSplit(splitSource);
        var profile=splits.EditingSplit!;
        var splitView=new SplitEditor(splits,splitSource,()=>{});
        splitView.Measure(new System.Windows.Size(1300,700));
        Check(splitView.DesiredSize.Width>0,"Split editor layout");
        splits.SelectSplitGroup(profile.Groups[0]);splits.ToggleReceiver.Execute(splits.Receivers[0]);
        splits.SelectSplitGroup(profile.Groups[1]);splits.ToggleReceiver.Execute(splits.Receivers[1]);
        splits.CloseSplit();
        splits.ToggleSplit(splitSource,profile);
        Check(splits.RoutesForNote("a",40,100,0).Single().ReceiverId=="jp","Lower split routes");
        Check(splits.RoutesForNote("a",70,100,0).Single().ReceiverId=="virus","Upper split routes");
        profile.Groups[0].High=80;
        Check(splits.RoutesForNote("a",70,100,0).Count()==2,"Overlapping split layers");
        splits.SetSplitGroupEnabled(profile.Groups[0],false);
        Check(splits.RoutesForNote("a",70,100,0).Single().ReceiverId=="virus","Disabled Split layer does not route");
        var disabledSplit=new RouterViewModel(Path.Combine(dir,"splits.json"));
        Check(!disabledSplit.State.SplitProfiles["a"][0].Groups[0].IsEnabled,"Disabled Split layer persists");
        splits.SetSplitGroupEnabled(profile.Groups[0],true);
        Check(splits.RoutesForNote("a",70,100,0).Count()==2,"Re-enabling Split restores its routes");
        profile.Groups[0].Type="Velocity";profile.Groups[0].VelocityHigh=50;
        Check(splits.RoutesForNote("a",70,100,0).Count()==1,"Split velocity condition");
        profile.Groups[1].Type="Modwheel";profile.Groups[1].ModLow=64;
        Check(!splits.RoutesForNote("a",70,100,0).Any(),"Split modwheel condition");
        splits.Save();
        var splitReload=new RouterViewModel(Path.Combine(dir,"splits.json"));
        Check(splitReload.State.SplitProfiles["a"].Count==1,"Split persistence");
        Check(splitReload.ActiveCard.SplitIds["a"]==profile.Id,"Active split persists in scene");

        var studioFile = Path.Combine(dir, "test.patchline.json");
        StudioFiles.Write(studioFile, splits.State);
        var target = new RouterViewModel(Path.Combine(dir, "target.json"));
        Check(target.ReplaceStudio(RouterViewModel.ReadStudio(studioFile)) &&
            target.ActiveCard.SplitIds["a"] == profile.Id, "Open saved studio with active Split");
        Check(target.ReplaceStudio(RouterViewModel.NewStudio()) &&
            target.ActiveCard.Routes.Count == 0 && target.State.SplitProfiles.Count == 0, "New studio clears routing");

        var sceneFile = Path.Combine(dir, "scene.patchline-export.json");
        StudioFiles.Write(sceneFile, StudioFiles.ExportScene(splits, splits.ActiveCard));
        target.State.Endpoints.Single(endpoint => endpoint.Id == "jp").Name = "Existing receiver";
        var merged = StudioFiles.Merge(target, StudioFiles.ReadExchange(sceneFile));
        var imported = merged.Cards.Last();
        var importedSplit = merged.SplitProfiles["a"].Single();
        var remappedReceiver = imported.Routes.First(route => route.ReceiverId != "virus").ReceiverId;
        Check(merged.Cards.Count == 2 && imported.Name != target.ActiveCard.Name && importedSplit.Id == imported.SplitIds["a"],
            "Scene import adds dependent profile and resolves name collision");
        Check(remappedReceiver != "jp" && merged.Endpoints.Single(endpoint => endpoint.Id == "jp").Name == "Existing receiver" &&
            importedSplit.Groups[0].Routes.Single().ReceiverId == remappedReceiver, "Import remaps receiver collision consistently");
        Check(target.ReplaceStudio(merged), "Imported scene can be loaded");

        var splitFile = Path.Combine(dir, "split.patchline-export.json");
        StudioFiles.Write(splitFile, StudioFiles.ExportSplit(splits, splitSource, profile));
        var mergedProfile = StudioFiles.Merge(target, StudioFiles.ReadExchange(splitFile));
        Check(mergedProfile.Cards.Count == target.Cards.Count && mergedProfile.SplitProfiles["a"].Count == 2,
            "Standalone Split import adds a profile without adding a scene");
        splits.ToggleSplit(splitSource,profile);
        Check(splits.RoutesForNote("a",70,100,0).Count()==2,"Disable split restores routing");

        var ctrlModel=new RouterViewModel(Path.Combine(dir,"ctrl.json"));
        ctrlModel.ChooseSource(ctrlModel.Sources[1],false);
        ctrlModel.SetControlSelection(true);
        ctrlModel.ToggleReceiver.Execute(ctrlModel.Receivers[0]);
        ctrlModel.ToggleReceiver.Execute(ctrlModel.Receivers[1]);
        Check(ctrlModel.ExtendedSelection&&ctrlModel.Receivers.Count(r=>r.IsRouted)==2,"CTRL adds multiple receivers");
        ctrlModel.SetControlSelection(false);
        Check(!ctrlModel.ExtendedSelection,"CTRL release restores single selection");
        ctrlModel.ToggleReceiver.Execute(ctrlModel.Receivers[3]);
        Check(ctrlModel.Receivers.Count(r=>r.IsRouted)==1,"Single selection after CTRL release");
        ctrlModel.ChooseSource(ctrlModel.Sources[1],true);
        ctrlModel.SetControlSelection(true);ctrlModel.SetControlSelection(false);
        Check(ctrlModel.ExtendedSelection,"CTRL release preserves existing extended mode");

        var soloSnapshot=System.Text.Json.JsonSerializer.Serialize(model.ActiveCard.Routes);
        var soloTarget=model.Receivers.Single(r=>r.Endpoint.Id=="kobol");
        Check(!soloTarget.IsRouted,"Solo target initially unassigned");
        model.ToggleSolo(soloTarget);
        Check(soloTarget.IsSolo&&model.Sources[0].IsSolo&&model.PlaybackRoutes.Single().ReceiverId=="kobol","Solo routes only target and marks source");
        Check(System.Text.Json.JsonSerializer.Serialize(model.ActiveCard.Routes)==soloSnapshot,"Solo preserves assignments");
        model.Save();
        Check(new RouterViewModel(file).SoloReceiverId==null,"Solo is temporary");
        model.ToggleSolo(soloTarget);
        Check(model.SoloReceiverId==null&&model.PlaybackRoutes.Count()==2&&!model.Sources[0].IsSolo,"Second solo restores original routing");
        model.ToggleSolo(soloTarget);
        model.ChooseSource(model.Sources[1],false);
        Check(model.SoloReceiverId==null,"Source switch exits solo");
        model.ChooseSource(model.Sources[0],false);
        model.ToggleSolo(model.Receivers.Single(r=>r.Endpoint.Id=="nord"));
        Check(model.SoloReceiverId=="nord","Unavailable target can solo");
        model.ToggleSolo(model.Receivers.Single(r=>r.Endpoint.Id=="nord"));

        var activationModel=new RouterViewModel(Path.Combine(dir,"activation.json"));
        var inactive=activationModel.Receivers.Single(r=>r.Endpoint.Id=="nord");
        activationModel.OctaveUp.Execute(inactive);
        Check(inactive.IsRouted && activationModel.ActiveRoute("nord")?.Octaves==0,"First plus activates without transpose");
        activationModel.OctaveUp.Execute(inactive);
        Check(activationModel.ActiveRoute("nord")?.Octaves==1,"Second plus transposes");
        activationModel.ToggleReceiver.Execute(inactive);
        activationModel.OctaveDown.Execute(inactive);
        Check(inactive.IsRouted && activationModel.ActiveRoute("nord")?.Octaves==0,"First minus activates without transpose");
        activationModel.OctaveDown.Execute(inactive);
        Check(activationModel.ActiveRoute("nord")?.Octaves== -1,"Second minus transposes");

        Check(model.Sources[0].Details.Type=="Klawiatura" && model.Sources[0].Details.KeyCount==49,"ROLI keyboard profile");
        Check(model.Sources[1].Details.KeyCount==61,"GXP61 keyboard profile");
        Check(SourceDetails.Detect("Unknown 61").KeyCount==null,"Unknown model is not guessed");
        var sourceDetails=model.Sources[0].Details.Copy();
        sourceDetails.FirstNote=36;
        model.UpdateSource(model.Sources[0],model.Sources[0].Name,model.Sources[0].ImagePath,details:sourceDetails);
        Check(new RouterViewModel(file).Sources[0].Details.FirstNote==36,"Keyboard range persistence");
        var sourceDraft=new SourceSettingsWindow(model,model.Sources[0]);
        sourceDraft.Drafts[0].KeyCountText="88";
        Check(model.Sources[0].Details.KeyCount==49,"Source settings cancel isolation");
        sourceDraft.Close();
        var detection=new EndpointDraft(new Endpoint{Name="Nektar Impact GXP61"},null,true);
        detection.DetectKeyboard();
        Check(detection.KeyCountText=="61" && detection.SourceType=="Klawiatura","Profile detection");
        detection.IsSource=true;
        detection.Name="Nektar Impact GXP88";
        Check(detection.KeyCountText=="88","Instant detection on name change");
        detection.Name="Unknown";
        detection.Port="Nektar Impact GXP49";
        Check(detection.KeyCountText=="49","Instant detection on port change");
        detection.Name="Nektar Impact GXP61";
        detection.FirstNoteText="36";
        Check(detection.RangeLabel.Contains("MIDI 96"),"Keyboard upper note calculation");

        var routeSnapshot=System.Text.Json.JsonSerializer.Serialize(model.ActiveCard.Routes);
        var allSources=model.SourceSections.Sum(section=>section.Sources.Count);
        var allReceivers=model.Rack.Count()+model.Keyboards.Count()+model.OnTop.Count();
        model.SourceSections[0].IsExpanded=false;
        model.SearchText="  roLI  ";
        Check(model.SourceSections.SelectMany(section=>section.Sources).Single().Id=="a","Search source case and whitespace");
        Check(!model.Rack.Any()&&!model.Keyboards.Any()&&!model.OnTop.Any(),"Search filters receivers independently");
        model.SearchText="VIRUS";
        Check(!model.SourceSections.Any()&&model.Rack.Single().Name=="Virus TI","Search receiver name");
        model.SearchText="Klawiatury";
        Check(model.SourceSections.Sum(section=>section.Sources.Count)==allSources,"Search source tags");
        model.SearchText="Rack";
        Check(model.Rack.Any(),"Search receiver tags");
        model.SearchText="no-such-instrument-123";
        Check(!model.SourceSections.Any()&&!model.HasRack&&!model.HasKeyboards&&!model.HasOnTop,"No matching results");
        model.SearchText="";
        Check(model.SourceSections.Sum(section=>section.Sources.Count)==allSources &&
            model.Rack.Count()+model.Keyboards.Count()+model.OnTop.Count()==allReceivers,"Clear search restores lists");
        Check(!model.SourceSections[0].IsExpanded,"Search preserves collapsed sections");
        Check(System.Text.Json.JsonSerializer.Serialize(model.ActiveCard.Routes)==routeSnapshot,"Search preserves routing");

        model.SelectSource.Execute(model.Sources[1]);
        Check(model.Receivers.All(r=>!r.IsRouted),"Source isolation");
        model.ToggleReceiver.Execute(model.Receivers[3]);
        model.ToggleReceiver.Execute(model.Receivers[0]);
        Check(model.Receivers.Count(r=>r.IsRouted)==1 && model.Receivers[0].IsRouted,"Single selection replaces prior receiver");
        model.ChooseSource(model.Sources[1],true);
        model.ToggleReceiver.Execute(model.Receivers[3]);
        Check(model.Receivers.Count(r=>r.IsRouted)==2,"Multiple receivers");
        model.OctaveUp.Execute(model.Receivers[3]);
        Check(model.ActiveRoute("roland")?.Octaves==1 && model.ActiveRoute("jp")?.Octaves==0,"Independent transpose");
        for(int i=0;i<10;i++)model.OctaveUp.Execute(model.Receivers[3]);
        Check(model.ActiveRoute("roland")?.Octaves==4,"Upper bound");
        for(int i=0;i<10;i++)model.OctaveDown.Execute(model.Receivers[3]);
        Check(model.ActiveRoute("roland")?.Octaves== -4,"Lower bound");
        Check(model.SelectionLabel=="Wybrana" && model.ExtendedSelection,"Color mode");
        model.SetOctaves("roland",1);
        model.ToggleReceiver.Execute(model.Receivers[2]);
        Check(model.Receivers[2].IsRouted,"Unavailable receiver can be selected");
        model.OctaveUp.Execute(model.Receivers[2]);
        Check(model.ActiveRoute("nord")?.Octaves==1,"Unavailable receiver transpose");
        model.Save();
        var offlineReload=new RouterViewModel(file);
        Check(offlineReload.Receivers.Single(r=>r.Name=="Nord Rack").IsRouted,"Offline choice persists");
        var connected=model.State.Endpoints.Select(e=>e.Copy()).ToList();
        connected.Single(e=>e.Id=="nord").Available=true;
        model.CommitSettings(connected,new());
        Check(model.Receivers.Single(r=>r.Name=="Nord Rack").IsRouted,"Reconnecting preserves selection");
        model.ToggleReceiver.Execute(model.Receivers.Single(r=>r.Name=="Nord Rack"));
        var count=model.ActiveCard.Routes.Count;
        model.Panic.Execute(null);
        Check(backend.PanicCount==1&&model.ActiveCard.Routes.Count==count,"Panic preserves configuration");
        var drafts=model.State.Endpoints.Select(e=>new EndpointDraft(e,model.ActiveRoute(e.Id))).ToList();
        drafts[0].Name="Cancelled name";
        Check(model.State.Endpoints[0].Name=="JP-8080","Settings cancel isolation");
        var endpoints=model.State.Endpoints.Select(e=>e.Copy()).ToList();
        endpoints[0].Name="JP edited";
        model.CommitSettings(endpoints,new(){{"roland",2}});
        Check(model.Save(),"Save");
        var restored=new RouterViewModel(file);
        Check(restored.State.SelectedSourceId=="b"&&restored.ActiveRoute("roland")?.Octaves==2&&restored.State.Endpoints[0].Name=="JP edited","Persistence");
        restored.SelectSource.Execute(restored.Sources[0]);
        Check(restored.ActiveRoute("roland")==null&&restored.Receivers.Count(r=>r.IsRouted)==2,"Original source preserved");
        restored.ChooseSource(restored.Sources[0],true);
        restored.ToggleReceiver.Execute(restored.Receivers[0]);
        Check(!restored.Receivers[0].IsRouted,"Toggle off");
        restored.ChooseSource(restored.Sources[0],true);
        restored.ToggleReceiver.Execute(restored.Receivers[0]);
        Check(restored.Receivers[0].IsRouted,"Toggle reversal");
        File.WriteAllText(file,"broken");
        var recovered=new RouterViewModel(file);
        Check(recovered.Receivers.Count==10&&recovered.Save()&&Directory.GetFiles(dir,"*.invalid-*").Length==1,"Invalid state backup");
        var catalog=model.State.Endpoints.Select(e=>e.Copy()).ToList();
        catalog.Add(new(){Id="planned",Name="Przyszły synth",Group="ON TOP",Port="",Hidden=true,Tags=new(){"Mini","Modular"}});
        model.CommitSettings(catalog,new());
        Check(!model.OnTop.Any(r=>r.Name=="Przyszły synth"),"Hidden excluded");
        model.ShowHidden=true;model.SelectedTags.Add("Mini");model.RefreshFilter();
        Check(model.OnTop.Single().Name=="Przyszły synth","Tag filter and show hidden");
        model.SelectedTags.Clear();model.ShowHidden=false;model.RefreshFilter();
        var catalogReload=new RouterViewModel(file);
        Check(catalogReload.State.Endpoints.Single(e=>e.Id=="planned").Port=="" && catalogReload.State.Endpoints.Single(e=>e.Id=="planned").Tags!.Contains("Modular"),"Unconnected catalog persists");
        model.ChooseSource(model.Sources.Single(s=>s.Id=="a"),false);
        model.ActiveCard.Routes.RemoveAll(r=>r.SourceId=="a");
        model.ToggleReceiver.Execute(model.Receivers[0]);
        var roliReload=new RouterViewModel(file);
        Check(roliReload.State.SelectedSourceId=="a" && roliReload.ActiveRoute(model.Receivers[0].Endpoint.Id)!=null,"ROLI routing persists");
        var roliBitmap=new System.Windows.Media.Imaging.BitmapImage(new Uri(model.Sources.Single(s=>s.Id=="a").ImagePath!));
        Check(roliBitmap.PixelWidth>0,"ROLI resource readable");
        Check(model.PrimarySources.Count()==2 && model.OtherSources.Count()==10,"Two primary and ten additional sources");
        Check(model.Sources[1].Name=="Nektar Impact GXP61","Second source Nektar");
        var nektar=new System.Windows.Media.Imaging.BitmapImage(new Uri(model.Sources[1].ImagePath!));
        Check(nektar.PixelWidth>0,"Nektar artwork embedded");
        var custom=model.OtherSources.Last();
        model.UpdateSource(custom,"Moje źródło",model.Sources[1].ImagePath);
        var configured=new RouterViewModel(file);
        Check(configured.Sources.Single(s=>s.Id==custom.Id).Name=="Moje źródło" && configured.Sources.Single(s=>s.Id==custom.Id).ImagePath==model.Sources[1].ImagePath,"Source settings persist");
        var settingsWindow=new SettingsWindow(model,model.State.Endpoints[0].Id);
        settingsWindow.ApplyTemplate();settingsWindow.Measure(new System.Windows.Size(760,670));
        var sourceWindow=new SourceSettingsWindow(model,model.Sources[0]);
        sourceWindow.ApplyTemplate();sourceWindow.Measure(new System.Windows.Size(600,640));
        Check(ArtworkPicker.Load("jp")!=null && ArtworkPicker.Load("")==null,"Artwork preview loads and clears");
        var previewDraft=new EndpointDraft(model.State.Endpoints[0],null);
        previewDraft.ImageKey="virus";Check(previewDraft.Preview!=null,"Draft artwork preview");
        previewDraft.Name="Nie zapisuj";Check(model.State.Endpoints[0].Name!="Nie zapisuj","Draft changes isolated");
        var ports=MidiPorts.Outputs("missing-port-test");
        Check(ports[0].Value=="" && ports.Any(p=>p.Value=="missing-port-test"&&!p.Present),"Port list preserves missing saved port and supports no connection");
        var assets=model.Receivers.Where(r=>r.ImagePath!=null).ToList();
        Check(assets.Count==9,"Four instrument mappings");
        foreach(var receiver in assets) {
            var bitmap=new System.Windows.Media.Imaging.BitmapImage(new Uri(receiver.ImagePath!));
            Check(bitmap.PixelWidth>0 && bitmap.PixelHeight>0,"Embedded bitmap readable");
        }
        var again=new RouterViewModel(file);
        Check(again.State.Endpoints.Select(e=>e.Id).Distinct().Count()==11,"No duplicate endpoints on reload");
        var originalCard=model.ActiveCard;
        var originalRoutes=originalCard.Routes.Select(r=>(r.SourceId,r.ReceiverId,r.Octaves)).ToList();
        var emptyCard=model.CreateCard(false);
        Check(emptyCard.Routes.Count==0 && model.Receivers.All(r=>!r.IsRouted),"New card empty");
        model.ToggleReceiver.Execute(model.Receivers[0]);
        model.SetOctaves(model.Receivers[0].Endpoint.Id,3);
        Check(originalCard.Routes.Select(r=>(r.SourceId,r.ReceiverId,r.Octaves)).SequenceEqual(originalRoutes),"Card isolation");
        var copied=model.CreateCard(true);
        Check(copied.Routes.Count==1 && copied.Routes[0].Octaves==3 && !ReferenceEquals(copied.Routes[0],emptyCard.Routes[0]),"Card copy deep snapshot");
        model.SetOctaves(model.Receivers[0].Endpoint.Id,-2);
        Check(emptyCard.Routes[0].Octaves==3,"Copy independent");
        copied.Name="Warstwa testowa";Check(model.SaveCard(),"Explicit card save");
        model.SetMainInterface("Test MIDI preference");
        var restoredCards=new RouterViewModel(file);
        Check(restoredCards.ActiveCard.Id==copied.Id && restoredCards.ActiveCard.Name=="Warstwa testowa" && restoredCards.ActiveCard.Routes[0].Octaves==-2,"Card restore");
        Check(restoredCards.State.MainInterface=="Test MIDI preference","Main interface preference persists");
        restoredCards.ActivateCard(emptyCard.Id);
        Check(restoredCards.ActiveCard.Routes[0].Octaves==3,"Recall previous card");
        model.SetCardPinned(copied,false);
        Check(!model.PinnedCards.Contains(copied)&&model.Cards.Contains(copied),"Unpin preserves catalog preset");
        var pinReload=new RouterViewModel(file);
        Check(!pinReload.State.Cards.Single(c=>c.Id==copied.Id).IsPinned,"Pin state persists");
        model.SetCardPinned(copied,true);
        Check(model.PinnedCards.Count(c=>c.Id==copied.Id)==1,"Repin without duplicates");
        var saveDialog=new RenameWindow("Preset",_=>{});
        Check(saveDialog.SizeToContent==System.Windows.SizeToContent.Height,"Save dialog height follows content");
        var catalogWindow=new CardCatalogWindow(model);catalogWindow.ApplyTemplate();catalogWindow.Measure(new System.Windows.Size(480,520));
        var generalWindow=new GeneralSettingsWindow(model);generalWindow.ApplyTemplate();generalWindow.Measure(new System.Windows.Size(430,260));
        var sourceCount=model.PrimarySources.Count();
        var newSource=new Source{Id="custom-source-test",Name=""};
        Check(!model.PrimarySources.Contains(newSource),"Draft source not added before saving");
        model.UpdateSource(newSource,"Nowe źródło",null);
        model.ChooseSource(newSource,false);model.ToggleReceiver.Execute(model.Receivers[0]);
        var sourceReload=new RouterViewModel(file);
        Check(sourceReload.PrimarySources.Count()==sourceCount+1 && sourceReload.State.SelectedSourceId==newSource.Id && sourceReload.ActiveRoute(model.Receivers[0].Endpoint.Id)!=null,"Custom source and routing persist");
        model.UpdateSource(newSource,newSource.Name,null,new[]{"Mini","Mini","Modular"});
        Check(model.SourceSections.Single(s=>s.Name=="Mini").Sources.Contains(newSource) && model.SourceSections.Single(s=>s.Name=="Modular").Sources.Contains(newSource),"Source appears in its tag sections");
        model.ReorderSourceSection("Modular",0);
        var taggedReload=new RouterViewModel(file);
        Check(taggedReload.SourceSections[0].Name=="Modular" && taggedReload.Sources.Single(s=>s.Id==newSource.Id).Tags.Count==2,"Source tags and section order persist");
        var originalSource=taggedReload.Sources.Single(s=>s.Id==newSource.Id);
        taggedReload.UpdateSource(originalSource,"Nowa nazwa",originalSource.ImagePath);
        Check(originalSource.Tags.Contains("Mini"),"Rename preserves source tags");
        var sharedSourceWindow=new SourceSettingsWindow(model,newSource);
        Check(sharedSourceWindow is SettingsWindow && sharedSourceWindow.Width==settingsWindow.Width && sharedSourceWindow.Height==settingsWindow.Height,"Source and receiver use same form layout");
        model.UpdateSource(newSource,"Wejście testowe",null,new[]{"KB"},"Windows MIDI","Test input",7);
        var inputReload=new RouterViewModel(file);
        var inputSource=inputReload.Sources.Single(s=>s.Id==newSource.Id);
        Check(inputSource.Port=="Test input" && inputSource.Channel==7 && inputSource.Tags.Single()=="KB","Source connection and tags persist");
        Check(MidiPorts.Inputs("Test input").Any(p=>p.Value=="Test input"),"Missing input remains selectable");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"self-test-result.txt"),"PASS: single/extended selection, independent octave controls and limits, source isolation, multiple receivers, offline selection and reconnect, transpose, PANIC, cancel, persistence, reversal, recovery.");
        foreach(var item in Directory.GetFiles(dir))File.Delete(item);
        Directory.Delete(dir);
    }
}
















