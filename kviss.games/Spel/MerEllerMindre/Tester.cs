namespace kviss.games.Spel.MerEllerMindre.Omgång;

using System;
using FluentAssertions;
using Xunit;
using Frågor = ISet<Fråga>;

public class OmgångSkall
{
    public readonly Frågor EnFråga = new HashSet<Fråga>() { new Fråga("musik", "flest", "miljoner", "Vilken grupp har sålt flest album, {alt1_namn} eller {alt2_namn}?", "{alt1_namn} har sålt flest album, {alt1_tal} { enhet}. Hur många färre {enhet} album har { alt2_namn } sålt?", "ABBA", 400, "Roxette", 75)};

    [Fact]
    public void Startas() =>
        Tillstånd.Initialt
        .Besluta(new Skapa(new Spelare("Martin"), EnFråga))
        .Should()
        .Equal(new Skapad(new Spelare("Martin"), EnFråga).ToArray());
}
