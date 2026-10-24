namespace StoneLasley.Application.Search;

/// <summary>
/// Ranking parameters, changed only by a PR that passes the search eval (#43).
/// </summary>
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>Which leg answers searches: hybrid (fused), keyword or vector. v1 ships with whichever the eval picks.</summary>
    public SearchMode Mode { get; set; } = SearchMode.Hybrid;

    public RrfOptions Rrf { get; set; } = new();

    public sealed class RrfOptions
    {
        public int K { get; set; } = 60;
        public double KeywordWeight { get; set; } = 1;
        public double VectorWeight { get; set; } = 1;
    }

    /// <summary>bm25 column weights for the keyword leg. Higher means a match there counts for more.</summary>
    public Bm25Weights Bm25 { get; set; } = new();

    public int Limit { get; set; } = 50;

    public VectorOptions Vector { get; set; } = new();

    public sealed class VectorOptions
    {
        /// <summary>
        /// The active index version (embedding model + chunker). Bumping it means building a new index first; see
        /// docs/deploy.md § Re-indexing.
        /// </summary>
        public string IndexVersion { get; set; } = "v1";

        /// <summary>Chunks fetched per query before they're rolled up into Posts.</summary>
        public int TopK { get; set; } = 50;

        /// <summary>
        /// Chunks scoring below this cosine similarity are dropped, so off-topic queries return nothing. 0.7 is the
        /// search eval's pick (ADR 0004): at 0.6 the leg adds enough weak matches to drag hybrid below keyword.
        /// </summary>
        public double CosineFloor { get; set; } = 0.7;

        /// <summary>Partition of the index this environment writes and reads. Tests use one per run.</summary>
        public string? Namespace { get; set; }

        /// <summary>Use the in-process fake embedder and index (fork PRs and E2E, which have no Cloudflare access).</summary>
        public bool Fake { get; set; }
    }

    public sealed class Bm25Weights
    {
        public double Title { get; set; } = 10;
        public double Structured { get; set; } = 5;
        public double Tags { get; set; } = 3;
        public double Body { get; set; } = 1;
    }
}

public enum SearchMode
{
    Hybrid,
    Keyword,
    Vector,
}
