namespace MoreOrLessGames.Spel;

using Händelser = IReadOnlyCollection<IHändelse>;

// händelser
public interface IHändelse { } // used to mimic a discriminated union

// kommandon
public interface IKommando { }

// vyer
public interface IVy { }

public record Tillstånd(decimal Balance, bool IsClosed)
{
    public static readonly Tillstånd Initialt = new(0, false);
}

public record Spelare(byte id, string name);

public static class MerEllerMindre
{
}
