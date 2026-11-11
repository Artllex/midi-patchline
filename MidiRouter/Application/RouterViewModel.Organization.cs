namespace MidiRouter;

public sealed partial class RouterViewModel
{
    public IEnumerable<string> ReceiverTags => Receivers.SelectMany(receiver =>
        receiver.Endpoint.Tags is { Count: > 0 } tags ? tags : new List<string> { "Bez tagów" })
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(tag => !State.HiddenReceiverTags.Contains(tag))
        .OrderBy(tag => { int index = State.SectionOrder.FindIndex(value => value.Equals(tag, StringComparison.OrdinalIgnoreCase)); return index < 0 ? int.MaxValue : index; });

    public IEnumerable<Receiver> ReceiversForTag(string tag) => Receivers.Where(receiver =>
        Matches(receiver.Endpoint) && (tag == "Bez tagów" ? receiver.Endpoint.Tags?.Count == 0 :
        receiver.Endpoint.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true));

    public void RefreshOrganization()
    {
        RefreshSourceSections();
        BuildReceivers();
        Changed(nameof(PrimarySources));
    }
}
