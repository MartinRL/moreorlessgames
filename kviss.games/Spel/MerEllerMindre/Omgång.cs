namespace kviss.games.Spel.MerEllerMindre.Omgång;

using Händelser = IReadOnlyCollection<IHändelse>;
using Frågor = IReadOnlyCollection<Fråga>;

public record Fråga(string Kategori, string Jämförelse, string Enhet, string Jämförelsefråga, string Deltafråga, string Alt1Namn, int Alt1Tal, string Alt2Namn, int Alt2Tal);

// händelser
public interface IHändelse { } // used to mimic a discriminated union
public record Startad(ushort Id, Spelare Spelmästare, Frågor Frågor) : IHändelse;

// kommandon
public interface IKommando { }
public record Starta(Spelare Spelmästare, Frågor Frågor) : IKommando;

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
