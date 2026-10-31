using StoneLasley.Domain.Posts;

namespace StoneLasley.Domain.UnitTests.Posts;

public class WatchTests
{
    private const string ValidWatch = """
        ---
        id: 01JBX5Q7Z9K3M8N2P4R6T0V1W5
        title: Rolex Submariner Date 16610
        slug: rolex-submariner-16610
        area: watches
        date: 2026-08-01
        audience: public
        status: published
        make: Rolex
        reference: "16610"
        modelName: Submariner Date
        variants: [16610LV, 16610T]
        productionStart: 1988
        productionEnd: 2010
        caseDiameterMm: 40
        calibre: "3135"
        caseMaterial: Oystersteel
        waterResistanceM: 300
        ---
        The last aluminium-bezel Submariner Date.
        """;

    [Test]
    public void Parses_the_watch_typed_core()
    {
        var watch = Parse(ValidWatch).Watch.ShouldNotBeNull();

        watch.Make.ShouldBe(WatchMake.Rolex);
        watch.Reference.ShouldBe("16610");
        watch.ModelName.ShouldBe("Submariner Date");
        watch.Variants.ShouldBe(["16610LV", "16610T"]);
        watch.ProductionStart.ShouldBe(1988);
        watch.ProductionEnd.ShouldBe(2010);
        watch.CaseDiameterMm.ShouldBe(40m);
        watch.Calibre.ShouldBe("3135");
        watch.CaseMaterial.ShouldBe("Oystersteel");
        watch.WaterResistanceM.ShouldBe(300);
    }

    [Test]
    public void Only_make_reference_and_model_name_are_required()
    {
        var minimal = """
            ---
            id: 01JBX5Q7Z9K3M8N2P4R6T0V1W6
            title: Patek Philippe Calatrava 96
            slug: patek-philippe-calatrava-96
            area: watches
            date: 2026-08-01
            audience: public
            status: published
            make: Patek Philippe
            reference: "96"
            modelName: Calatrava
            ---
            """;

        var watch = Parse(minimal).Watch.ShouldNotBeNull();

        watch.Make.ShouldBe(WatchMake.PatekPhilippe);
        watch.Variants.ShouldBeEmpty();
        watch.ProductionStart.ShouldBeNull();
        watch.InProduction.ShouldBeFalse();
    }

    [TestCase("Omega", WatchMake.Omega)]
    [TestCase("Audemars Piguet", WatchMake.AudemarsPiguet)]
    [TestCase("Vacheron Constantin", WatchMake.VacheronConstantin)]
    [TestCase("A. Lange & Söhne", WatchMake.ALangeAndSohne)]
    [TestCase("Tudor", WatchMake.Tudor)]
    [TestCase("Cartier", WatchMake.Cartier)]
    [TestCase("Grand Seiko", WatchMake.GrandSeiko)]
    [TestCase("IWC", WatchMake.Iwc)]
    [TestCase("Jaeger-LeCoultre", WatchMake.JaegerLeCoultre)]
    [TestCase("Breitling", WatchMake.Breitling)]
    [TestCase("Seiko", WatchMake.Seiko)]
    [TestCase("Swatch", WatchMake.Swatch)]
    [TestCase("Breguet", WatchMake.Breguet)]
    [TestCase("F.P. Journe", WatchMake.FPJourne)]
    [TestCase("Casio", WatchMake.Casio)]
    [TestCase("Glycine", WatchMake.Glycine)]
    [TestCase("Panerai", WatchMake.Panerai)]
    [TestCase("DIW", WatchMake.Diw)]
    [TestCase("Artisans de Genève", WatchMake.ArtisansDeGeneve)]
    [TestCase("Richard Mille", WatchMake.RichardMille)]
    [TestCase("Blancpain", WatchMake.Blancpain)]
    [TestCase("Zenith", WatchMake.Zenith)]
    [TestCase("Longines", WatchMake.Longines)]
    [TestCase("True North", WatchMake.TrueNorth)]
    [TestCase("Christopher Ward", WatchMake.ChristopherWard)]
    [TestCase("Yema", WatchMake.Yema)]
    [TestCase("Baltic", WatchMake.Baltic)]
    [TestCase("Furlan Marri", WatchMake.FurlanMarri)]
    [TestCase("anOrdain", WatchMake.AnOrdain)]
    [TestCase("Serica", WatchMake.Serica)]
    [TestCase("Lorier", WatchMake.Lorier)]
    [TestCase("Studio Underd0g", WatchMake.StudioUnderdog)]
    [TestCase("Kurono Tokyo", WatchMake.KuronoTokyo)]
    [TestCase("Atelier Wen", WatchMake.AtelierWen)]
    [TestCase("Monta", WatchMake.Monta)]
    [TestCase("Venezianico", WatchMake.Venezianico)]
    [TestCase("Unimatic", WatchMake.Unimatic)]
    [TestCase("Orient", WatchMake.Orient)]
    [TestCase("Watchdives", WatchMake.Watchdives)]
    [TestCase("Sugess", WatchMake.Sugess)]
    [TestCase("Timex", WatchMake.Timex)]
    [TestCase("Sinn", WatchMake.Sinn)]
    [TestCase("Bulova", WatchMake.Bulova)]
    [TestCase("Caravelle", WatchMake.Caravelle)]
    [TestCase("Shinola", WatchMake.Shinola)]
    [TestCase("D1 Milano", WatchMake.D1Milano)]
    [TestCase("TAG Heuer", WatchMake.TagHeuer)]
    [TestCase("Helm", WatchMake.Helm)]
    [TestCase("Vaer", WatchMake.Vaer)]
    [TestCase("Squale", WatchMake.Squale)]
    [TestCase("Hamilton", WatchMake.Hamilton)]
    [TestCase("Marathon", WatchMake.Marathon)]
    [TestCase("Wyler", WatchMake.Wyler)]
    [TestCase("Abercrombie & Fitch", WatchMake.AbercrombieAndFitch)]
    [TestCase("Heuer", WatchMake.Heuer)]
    [TestCase("Wakmann", WatchMake.Wakmann)]
    [TestCase("Doxa", WatchMake.Doxa)]
    [TestCase("Favre Leuba", WatchMake.FavreLeuba)]
    [TestCase("Eberhard & Co.", WatchMake.Eberhard)]
    public void Parses_a_make_by_its_written_name(string name, WatchMake make)
    {
        var watch = Parse(ValidWatch.Replace("make: Rolex", $"make: {name}")).Watch.ShouldNotBeNull();

        watch.Make.ShouldBe(make);
        make.Name().ShouldBe(name);
    }

    [Test]
    public void A_watch_with_a_start_and_no_end_is_still_in_production()
    {
        var watch = Parse(ValidWatch.Replace("productionEnd: 2010\n", "")).Watch!;

        watch.InProduction.ShouldBeTrue();
    }

    [TestCase(300, 984)]
    [TestCase(100, 328)]
    [TestCase(30, 98)]
    public void Water_resistance_converts_to_feet(int metres, int feet)
    {
        Watch.ToFeet(metres).ShouldBe(feet);
    }

    [TestCase(1988, 2010, new[] { 1980, 1990, 2000, 2010 })]
    [TestCase(2020, null, new[] { 2020 })]
    public void Covers_every_decade_it_was_produced_in(int start, int? end, int[] decades)
    {
        var watch = Parse(ValidWatch.Replace("productionStart: 1988", $"productionStart: {start}")
            .Replace("productionEnd: 2010\n", end is null ? "" : $"productionEnd: {end}\n")).Watch!;

        watch.Decades(currentYear: 2026).ShouldBe(decades);
    }

    [Test]
    public void A_watch_with_no_production_years_has_no_decades()
    {
        var watch = Parse(ValidWatch.Replace("productionStart: 1988\n", "").Replace("productionEnd: 2010\n", "")).Watch!;

        watch.Decades(currentYear: 2026).ShouldBeEmpty();
    }

    [Test]
    public void Rejects_an_unknown_make()
    {
        var result = MarkdownPost.Parse(ValidWatch.Replace("make: Rolex", "make: Acme"));

        result.Post.ShouldBeNull();
        result.Errors.ShouldBe([$"'make' must be one of: {string.Join(", ", WatchMakes.All)}."]);
    }

    [TestCase("reference: \"16610\"", "reference: ''", "'reference' is required.")]
    [TestCase("modelName: Submariner Date", "modelName: ''", "'modelName' is required.")]
    [TestCase("productionStart: 1988", "productionStart: eighties", "'productionStart' must be a year.")]
    [TestCase("productionEnd: 2010", "productionEnd: 1980", "'productionEnd' must not be before 'productionStart'.")]
    [TestCase("caseDiameterMm: 40", "caseDiameterMm: big", "'caseDiameterMm' must be a number above 0.")]
    [TestCase("waterResistanceM: 300", "waterResistanceM: deep", "'waterResistanceM' must be a whole number, 1 or more.")]
    public void Rejects_an_invalid_typed_core_field(string valid, string invalid, string error)
    {
        var result = MarkdownPost.Parse(ValidWatch.Replace(valid, invalid));

        result.Post.ShouldBeNull();
        result.Errors.ShouldBe([error]);
    }

    [Test]
    public void An_end_without_a_start_is_rejected()
    {
        var result = MarkdownPost.Parse(ValidWatch.Replace("productionStart: 1988\n", ""));

        result.Errors.ShouldBe(["'productionEnd' needs a 'productionStart'."]);
    }

    [Test]
    public void Posts_outside_watches_carry_no_watch()
    {
        var result = MarkdownPost.Parse(ValidWatch.Replace("area: watches", "area: training"));

        result.Post.ShouldNotBeNull().Watch.ShouldBeNull();
    }

    private static Post Parse(string markdown)
    {
        var result = MarkdownPost.Parse(markdown);
        result.Errors.ShouldBeEmpty();
        return result.Post!;
    }
}
