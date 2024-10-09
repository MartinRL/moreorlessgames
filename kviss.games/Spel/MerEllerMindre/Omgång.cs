namespace kviss.games.Spel.MerEllerMindre.Omgång;

using Händelser = IReadOnlyCollection<IHändelse>;
using static System.Guid;
using Frågor = ISet<Fråga>;
using Microsoft.Extensions.Logging;

public record Spelare(string Namn);

public record Fråga(string Kategori, string Jämförelse, string Enhet, string Jämförelsefråga, string Deltafråga, string Alt1Namn, int Alt1Tal, string Alt2Namn, int Alt2Tal);

// händelser
public interface IHändelse { } // used to mimic a discriminated union
public record Skapad : IHändelse
{
    public Guid Id { get; init; } = NewGuid();
    public Spelare Spelmästare { get; init; }
    public Frågor Frågor { get; init; }

    public Skapad(Spelare spelmästare, Frågor frågor) { Spelmästare = spelmästare; Frågor = frågor; }
}

// kommandon
public interface IKommando { }
public record Skapa(Spelare Spelmästare, Frågor Frågor /* antal frågor = omgångens längd */ ) : IKommando;

// vyer
public interface IVy { }

public record Tillstånd(IDictionary<string, ushort> Ställning, bool ÄrStartad, bool ÄrAvslutad)
{
    public static readonly Tillstånd Initialt = new(new Dictionary<string, ushort>(), false, false);
}
public static class Beslutare
{
    public static Händelser Besluta(this Tillstånd @this, IKommando kommando) =>
        kommando switch
        {
            Skapa k => Skapa(k),
            /*Withdraw k => Withdraw(c),
            Close k => Close(state, c),*/
            _ => throw new InvalidOperationException()
        };
    private static Händelser Skapa(Skapa k) =>
        new Skapad(k.Spelmästare, k.Frågor).ToArray();
}

// hjälpfunktioner

public static class Extensions
{
    public static Händelser ToArray(this IHändelse h) => [h];
}
