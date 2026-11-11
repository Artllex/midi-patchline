
namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public List<RoutingCard> Cards => State.Cards;
    public IEnumerable<RoutingCard> PinnedCards => State.Cards.Where(c => c.IsPinned);
    public void DeleteCard(RoutingCard card)
    {
        if (!State.Cards.Remove(card))
            return;
        if (State.Cards.Count == 0)
            State.Cards.Add(new RoutingCard { Id = Guid.NewGuid().ToString("N"), Name = "Nowa scena" });
        Changed(nameof(Cards));
        Changed(nameof(PinnedCards));
        if (State.ActiveCardId == card.Id)
            ActivateCard(State.Cards[0].Id);
        Save();
    }
    public void SetCardPinned(RoutingCard card, bool value)
    {
        if (!State.Cards.Contains(card))
            return;
        card.IsPinned = value;
        Changed(nameof(PinnedCards));
        Save();
    }
    public Command SelectCard => new(p => { if (p is RoutingCard card) ActivateCard(card.Id); });
    public void ActivateCard(string id)
    {
        if (!State.Cards.Any(c => c.Id == id))
            return;
        SoloReceiverId = null;
        EditingSplit = null;
        EditingGroup = null;
        State.ActiveCardId = id;
        foreach (var c in State.Cards)
            c.IsActive = c.Id == id;
        Changed(nameof(ActiveCard));
        Refresh();
        ApplyCurrentRoutes();
        Save();
    }
    public RoutingCard CreateCard(bool copy)
    {
        var card = new RoutingCard { Id = Guid.NewGuid().ToString("N"), Name = "Karta " + (State.Cards.Count + 1) };
        if (copy)
            card.SplitIds = new(ActiveCard.SplitIds);
        if (copy)
            card.SourceOctaves = new(ActiveCard.SourceOctaves);
        if (copy)
            card.Routes = ActiveCard.Routes.Select(r => new Route { SourceId = r.SourceId, ReceiverId = r.ReceiverId, Octaves = r.Octaves }).ToList();
        State.Cards.Add(card);
        Changed(nameof(Cards));
        Changed(nameof(PinnedCards));
        ActivateCard(card.Id);
        return card;
    }
    public bool SaveCard()
    {
        if (string.IsNullOrWhiteSpace(ActiveCard.Name))
        {
            Status = "Wpisz nazwę karty.";
            return false;
        }
        if (!Save())
            return false;
        Status = "Zapisano kartę: " + ActiveCard.Name;
        return true;
    }
    public void SetMainInterface(string value)
    {
        State.MainInterface = value;
        Save();
    }
    public RoutingCard ActiveCard => State.Cards.Single(c => c.Id == State.ActiveCardId);

}
