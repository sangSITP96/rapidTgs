using System;

[Serializable]
public class ColonyEventEntry
{
    public string Id;
    public long TimestampUnixMsUtc;
    public EventCategory Category;
    public string Title;
    public string Summary;

    public string PayloadJson;

    public static ColonyEventEntry Create(
        EventCategory category,
        string title,
        string summary = "",
        string payloadJson = "")
    {
        return new ColonyEventEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            TimestampUnixMsUtc = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Category = category,
            Title = title ?? "",
            Summary = summary ?? "",
            PayloadJson = payloadJson ?? ""
        };
    }
}
