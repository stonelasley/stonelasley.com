namespace StoneLasley.Domain.Posts;

/// <summary>
/// Who may read a Post. Only Public exists until Access Codes land; anything else is rejected at ingest
/// so a Private Post can never be served publicly by mistake.
/// </summary>
public enum Audience
{
    Public,
}
