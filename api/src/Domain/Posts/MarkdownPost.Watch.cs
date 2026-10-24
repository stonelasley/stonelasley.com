using System.Globalization;

namespace StoneLasley.Domain.Posts;

public static partial class MarkdownPost
{
    private static Watch? ReadWatch(Frontmatter frontmatter, List<string> errors)
    {
        var count = errors.Count;

        if (!WatchMakes.TryParse(frontmatter.Make?.Trim(), out var make))
        {
            errors.Add($"'make' must be one of: {string.Join(", ", WatchMakes.All)}.");
        }
        var reference = Required(frontmatter.Reference, "reference", errors);
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
            : new Watch(make, reference!, modelName!, variants, start, end, diameter, Blank(frontmatter.Calibre), Blank(frontmatter.CaseMaterial), waterResistance);
    }

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
