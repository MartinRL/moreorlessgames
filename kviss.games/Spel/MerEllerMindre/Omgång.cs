﻿namespace kviss.games.Spel.MerEllerMindre.Omgång;

using static SystemGuid;
using Händelser = IReadOnlyCollection<IHändelse>;
using Frågor = ISet<Fråga>;

public record Spelare(string Namn);

public record Fråga(string Kategori, string Jämförelse, string Enhet, string Jämförelsefråga, string Deltafråga, string Alt1Namn, int Alt1Tal, string Alt2Namn, int Alt2Tal);

// händelser
public interface IHändelse { } // used to mimic a discriminated union
public record Skapad(Guid Id, Spelare Spelmästare, Frågor Frågor) : IHändelse;

// kommandon
public interface IKommando { }
public record Skapa(Guid Id, Spelare Spelmästare, Frågor Frågor) : IKommando;

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
            _ => throw new InvalidOperationException()
        };
    private static Händelser Skapa(Skapa k) =>
        new Skapad(NewGuid(), k.Spelmästare, k.Frågor).ToArray();
}

// hjälpare

public static class Extensions
{
    public static Händelser ToArray(this IHändelse h) => [h];
}

public static class SystemGuid
{
    public static Func<Guid> NewGuid = Guid.NewGuid;
}
