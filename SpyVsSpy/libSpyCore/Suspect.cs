namespace SpyCore;

public sealed class Suspect
{
    public string Name { get; }
    public bool IsAlive { get; private set; } = true;

    public Suspect(string name)
    {
        Name = name;
    }

    public void MarkCaught() => IsAlive = false;

    public override string ToString() => Name;
}
