using System.Globalization;

namespace StoneLasley.Domain.Posts;

public static partial class MarkdownPost
{
    private static Watch? ReadWatch(Frontmatter frontmatter, List<string> errors)
    {
        var count = errors.Count;

        // Whether the make is declared is checked at ingest, against the content repo's makes file.
        var make = Required(frontmatter.Make, "make", errors);

        // No reference makes it a Make Post, which carries no specs. A spec field without one is a Reference Post whose
        // reference was forgotten, so it fails rather than publishing as a Make Post.
        if (string.IsNullOrWhiteSpace(frontmatter.Reference))
        {
            foreach (var (field, value) in SpecFields(frontmatter))
            {
                if (value is not null)
                {
                    errors.Add($"'{field}' needs a 'reference'.");
                }
            }
            return errors.Count > count ? null : new Watch(make!, null, null, [], null, null, null, null, null, null);
        }

        var reference = frontmatter.Reference.Trim();
        var modelName = Required(frontmatter.ModelName, "modelName", errors);

        var variants = (frontmatter.Variants ?? []).Select(v => v?.Trim() ?? string.Empty).ToList();
        if (variants.Any(string.IsNullOrEmpty))
        {
            errors.Add("'variants' must not contain empty values.");
        }

        var start = Year(frontmatter.ProductionStart, "productionStart", errors);
        var end = Year(frontmatter.ProductionEnd, "productionEnd", errors);
        if (end is not null && frontmatter.ProductionStart is null)
        {
            errors.Add("'productionEnd' needs a 'productionStart'.");
        }
        else if (start is not null && end is not null && end < start)
        {
            errors.Add("'productionEnd' must not be before 'productionStart'.");
        }

        decimal? diameter = null;
        if (frontmatter.CaseDiameterMm is not null)
        {
            if (decimal.TryParse(frontmatter.CaseDiameterMm.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var mm) && mm > 0)
            {
                diameter = mm;
            }
            else
            {
                errors.Add("'caseDiameterMm' must be a number above 0.");
            }
        }

        var waterResistance = frontmatter.WaterResistanceM is null
            ? null
            : WholeNumber(frontmatter.WaterResistanceM, "waterResistanceM", 1, errors);

        return errors.Count > count
            ? null
            : new Watch(make!, reference, modelName!, variants, start, end, diameter, Blank(frontmatter.Calibre), Blank(frontmatter.CaseMaterial), waterResistance);
    }

    private static IEnumerable<(string Field, object? Value)> SpecFields(Frontmatter frontmatter) =>
    [
        ("modelName", frontmatter.ModelName),
        ("variants", frontmatter.Variants),
        ("productionStart", frontmatter.ProductionStart),
        ("productionEnd", frontmatter.ProductionEnd),
        ("caseDiameterMm", frontmatter.CaseDiameterMm),
        ("calibre", frontmatter.Calibre),
        ("caseMaterial", frontmatter.CaseMaterial),
        ("waterResistanceM", frontmatter.WaterResistanceM),
    ];

    private static string? Required(string? text, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"'{field}' is required.");
            return null;
        }
        return text.Trim();
    }

    private static int? Year(string? text, string field, List<string> errors)
    {
        if (text is null)
        {
            return null;
        }
        if (int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var year) && year is >= 1800 and <= 2200)
        {
            return year;
        }
        errors.Add($"'{field}' must be a year.");
        return null;
    }
}
