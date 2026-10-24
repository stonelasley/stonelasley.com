namespace StoneLasley.Application.Search;

/// <summary>
/// Ranking parameters, changed only by a PR that passes the search eval (#43).
/// </summary>
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>bm25 column weights for the keyword leg. Higher means a match there counts for more.</summary>
    public Bm25Weights Bm25 { get; set; } = new();

    public int Limit { get; set; } = 50;

    public sealed class Bm25Weights
    {
        public double Title { get; set; } = 10;
        public double Structured { get; set; } = 5;
        public double Tags { get; set; } = 3;
        public double Body { get; set; } = 1;
    }
}
