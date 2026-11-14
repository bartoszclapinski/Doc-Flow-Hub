using System.Globalization;
using DocFlowHub.Core.Models.Documents.Browser;
using Microsoft.AspNetCore.Http;

namespace DocFlowHub.Web.Ui;

/// <summary>
/// The Documents list's URL state (decision S11-2). Every link on the page — tree, filters, tokens,
/// sort headers, pager — is this record with one value changed, so no link can drop a filter.
/// </summary>
public sealed record DocumentListState
{
    public const int PageSize = 25;

    public string? Q { get; init; }
    public string? Type { get; init; }

    /// <summary>"me", "others" or null (anyone).</summary>
    public string? Owner { get; init; }
    public int? Category { get; init; }
    public int? Project { get; init; }
    public int? Folder { get; init; }
    public int? Team { get; init; }

    /// <summary>"name", "size" or null (modified, the default).</summary>
    public string? Sort { get; init; }

    /// <summary>"asc", "desc" or null (the column's natural direction).</summary>
    public string? Dir { get; init; }
    public int Page { get; init; } = 1;

    /// <summary>Cleans raw query-string values: unknown words are dropped, ids must be positive.</summary>
    public static DocumentListState From(string? q, string? type, string? owner, int? category, int? project,
        int? folder, int? team, string? sort, string? dir, int? page)
    {
        static string? Word(string? value, params string[] allowed)
        {
            var v = value?.Trim().ToLowerInvariant();
            return v != null && allowed.Contains(v) ? v : null;
        }
        static int? Id(int? value) => value > 0 ? value : null;

        var cleanType = type?.Trim().TrimStart('.').ToLowerInvariant();
        return new DocumentListState
        {
            Q = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Type = string.IsNullOrEmpty(cleanType) || cleanType.Length > 10 ? null : cleanType,
            Owner = Word(owner, "me", "others"),
            Category = Id(category),
            Project = Id(project),
            Folder = Id(folder),
            Team = Id(team),
            Sort = Word(sort, "name", "size"),
            Dir = Word(dir, "asc", "desc"),
            Page = page > 1 ? page.Value : 1,
        };
    }

    public DocumentSort SortColumn => Sort switch
    {
        "name" => DocumentSort.Name,
        "size" => DocumentSort.Size,
        _ => DocumentSort.Modified,
    };

    /// <summary>Names sort A→Z first; sizes and dates largest/newest first.</summary>
    public bool Descending => Dir switch
    {
        "asc" => false,
        "desc" => true,
        _ => SortColumn != DocumentSort.Name,
    };

    public DocumentOwnerFilter OwnerFilter => Owner switch
    {
        "me" => DocumentOwnerFilter.Me,
        "others" => DocumentOwnerFilter.Others,
        _ => DocumentOwnerFilter.Anyone,
    };

    /// <summary>Search, type, owner or category — the filters shown as tokens.</summary>
    public bool HasFilters => Q != null || Type != null || Owner != null || Category != null;

    /// <summary>A project, folder or team is selected in the tree.</summary>
    public bool HasScope => Project != null || Folder != null || Team != null;

    public DocumentListQuery ToQuery() => new()
    {
        Search = Q,
        FileType = Type,
        Owner = OwnerFilter,
        CategoryId = Category,
        ProjectId = Folder == null ? Project : null,
        FolderId = Folder,
        TeamId = Team,
        Sort = SortColumn,
        Descending = Descending,
        Page = Page,
        PageSize = PageSize,
    };

    /// <summary>Another tree node: keeps the filters, resets the page.</summary>
    public DocumentListState Scoped(int? project = null, int? folder = null, int? team = null) =>
        this with { Project = project, Folder = folder, Team = team, Page = 1 };

    /// <summary>Sort by a column; the active column flips direction.</summary>
    public DocumentListState SortedBy(string? column)
    {
        var target = column is "name" or "size" ? column : null;
        if (target == Sort)
        {
            return this with { Dir = Descending ? "asc" : "desc", Page = 1 };
        }
        return this with { Sort = target, Dir = null, Page = 1 };
    }

    public DocumentListState WithoutFilters() => this with { Q = null, Type = null, Owner = null, Category = null, Page = 1 };

    /// <summary>Relative URL of this state, e.g. "/Documents?folder=4&amp;type=pdf".</summary>
    public string Href
    {
        get
        {
            var query = QueryString.Empty;
            void Add(string name, string? value)
            {
                if (!string.IsNullOrEmpty(value)) query = query.Add(name, value);
            }
            static string? Num(int? value) => value?.ToString(CultureInfo.InvariantCulture);

            Add("project", Num(Project));
            Add("folder", Num(Folder));
            Add("team", Num(Team));
            Add("q", Q);
            Add("type", Type);
            Add("owner", Owner);
            Add("category", Num(Category));
            Add("sort", Sort);
            Add("dir", Dir);
            Add("page", Page > 1 ? Num(Page) : null);
            return "/Documents" + query.ToUriComponent();
        }
    }
}
