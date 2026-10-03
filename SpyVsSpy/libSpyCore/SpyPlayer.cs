namespace SpyCore;

public sealed class SpyPlayer
{
    public string Name { get; }
    public bool IsBot { get; }
    public Suspect SecretIdentity { get; internal set; } = null!;
    public int Trophies { get; internal set; }

    public SpyPlayer(string name, bool isBot)
    {
        Name = name;
        IsBot = isBot;
    }
}
