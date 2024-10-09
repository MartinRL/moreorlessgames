namespace kviss.games.Spel.MerEllerMindre.Omgång;

using System;
using FluentAssertions;
using FluentAssertions.Equivalency;
using Xunit;
using Frågor = ISet<Fråga>;

public class OmgångSkall
{
    public readonly Frågor EnFråga = new HashSet<Fråga>() { new("musik", "flest", "miljoner", "Vilken grupp har sålt flest album, {alt1_namn} eller {alt2_namn}?", "{alt1_namn} har sålt flest album, {alt1_tal} {enhet}. Hur många färre {enhet} album har { alt2_namn } sålt?", "ABBA", 400, "Roxette", 75)};

    public readonly Spelare Spelmästare = new("Martin");

    [Fact]
    public void Startas()
    {
        Skapa skapa = new (Spelmästare, EnFråga);

        Tillstånd.Initialt
        .Besluta(skapa)
        .Should()
        .Equal(new Skapad(skapa.Id, Spelmästare, EnFråga).ToArray());
    }
}
