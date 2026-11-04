using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StoneLasley.Domain.Posts;

/// <summary>A watch make the site covers: how it is written, and the other spellings that mean it.</summary>
public sealed record WatchMake(string Name, IReadOnlyList<string> Aliases)
{
    // Equality compares aliases by value, not by list reference.
    public bool Equals(WatchMake? other) => other is not null && Name == other.Name && Aliases.SequenceEqual(other.Aliases);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// The makes Watch Posts may name, declared in the content repo's makes file (ADR 0006). A Post's make matches a
/// name or an alias, ignoring case, and is stored under the name.
/// </summary>
public sealed class WatchMakes
{
    private readonly Dictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase);

    public WatchMakes(IEnumerable<WatchMake> makes)
    {
        foreach (var make in makes)
        {
            foreach (var written in make.Aliases.Prepend(make.Name))
            {
                names.TryAdd(written, make.Name);
            }
        }
    }

    /// <summary>The make's name for how a Post wrote it, or null when no make is declared that way.</summary>
    public string? Resolve(string written) => names.GetValueOrDefault(written.Trim());
}

public sealed record MakesFileResult(IReadOnlyList<WatchMake> Makes, IReadOnlyList<string> Errors);

/// <summary>Reads the content repo's makes file: a YAML list of <c>{ name, aliases }</c>.</summary>
public static class MakesFile
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static MakesFileResult Parse(string yaml)
    {
        List<Entry?>? entries;
        try
        {
            entries = Yaml.Deserialize<List<Entry?>>(yaml);
        }
        catch (YamlException exception)
        {
            return new MakesFileResult([], [$"The makes file is not valid YAML: {exception.Message}"]);
        }

        var makes = new List<WatchMake>();
        var errors = new List<string>();
        // Names and aliases share one namespace, so a spelling can only ever mean one make.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (entry, index) in (entries ?? []).Select((e, i) => (e ?? new Entry(), i + 1)))
        {
            var name = entry.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                errors.Add($"Make {index}: 'name' is required.");
                continue;
            }
            var aliases = (entry.Aliases ?? []).Select(alias => alias?.Trim() ?? "").ToList();
            if (aliases.Any(alias => alias.Length == 0))
            {
                errors.Add($"Make {name}: aliases must not be empty.");
                continue;
            }
            foreach (var written in aliases.Prepend(name).Where(written => !seen.Add(written)))
            {
                errors.Add($"'{written}' is declared more than once.");
            }
            makes.Add(new WatchMake(name, aliases));
        }
        return new MakesFileResult(errors.Count == 0 ? makes : [], errors);
    }

    private sealed class Entry
    {
        public string? Name { get; set; }
        public List<string?>? Aliases { get; set; }
    }
}
