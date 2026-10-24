using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StoneLasley.Domain.Access;

/// <summary>A coaching Client: a ULID and a display name, declared in the content repo, never in this one.</summary>
public sealed record Client(string Id, string Name);

public sealed record ClientsFileResult(IReadOnlyList<Client> Clients, IReadOnlyList<string> Errors);

/// <summary>Reads the content repo's clients file: a YAML list of <c>{ id, name }</c>.</summary>
public static partial class ClientsFile
{
    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static ClientsFileResult Parse(string yaml)
    {
        List<Entry?>? entries;
        try
        {
            entries = Yaml.Deserialize<List<Entry?>>(yaml);
        }
        catch (YamlException exception)
        {
            return new ClientsFileResult([], [$"The clients file is not valid YAML: {exception.Message}"]);
        }

        var clients = new List<Client>();
        var errors = new List<string>();
        var seen = new HashSet<string>();
        foreach (var (entry, index) in (entries ?? []).Select((e, i) => (e ?? new Entry(), i + 1)))
        {
            var id = entry.Id?.Trim();
            var name = entry.Name?.Trim();
            if (id is null || !Ulid().IsMatch(id))
            {
                errors.Add($"Client {index}: 'id' must be a ULID.");
                continue;
            }
            if (string.IsNullOrEmpty(name))
            {
                errors.Add($"Client {index}: 'name' is required.");
                continue;
            }
            if (!seen.Add(id))
            {
                errors.Add($"Client {id} is declared more than once.");
                continue;
            }
            clients.Add(new Client(id, name));
        }
        return new ClientsFileResult(errors.Count == 0 ? clients : [], errors);
    }

    [GeneratedRegex("^[0-7][0-9A-HJKMNP-TV-Z]{25}$")]
    internal static partial Regex Ulid();

    private sealed class Entry
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
    }
}
