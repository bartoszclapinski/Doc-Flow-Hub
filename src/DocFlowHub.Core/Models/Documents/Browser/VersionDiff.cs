namespace DocFlowHub.Core.Models.Documents.Browser;

/// <summary>What the Compare page can show for two versions (Sprint 21).</summary>
public enum VersionDiffStatus
{
    /// <summary>The texts differ; <see cref="VersionDiff.Sections"/> holds the diff.</summary>
    Changed,

    /// <summary>Both versions have the same text (their layout may still differ).</summary>
    Identical,

    /// <summary>A version has no text to compare (an image, a .doc, a scanned PDF).</summary>
    NoText,

    /// <summary>A version has more text or lines than the page compares.</summary>
    TooLarge,

    /// <summary>A file could not be read from storage.</summary>
    Unreadable,
}

public enum DiffLineKind
{
    /// <summary>The same line in both versions.</summary>
    Context,

    /// <summary>A line only the older version has.</summary>
    Removed,

    /// <summary>A line only the newer version has.</summary>
    Added,
}

public enum DiffSectionKind
{
    /// <summary>Unchanged lines shown around a change.</summary>
    Context,

    /// <summary>One change: removed lines, added lines, or both.</summary>
    Change,

    /// <summary>Unchanged lines that are folded away; only <see cref="DiffSection.FoldedLines"/> is set.</summary>
    Fold,
}

/// <summary>A piece of a changed line; <see cref="Changed"/> marks the words that differ.</summary>
public record DiffPiece(string Text, bool Changed);

/// <summary>
/// One line of the diff. A context line has both numbers and both texts (they can differ in whitespace
/// when it is ignored). In a change, a removed line and the added line with the same <see cref="Row"/>
/// sit next to each other in the side-by-side view.
/// </summary>
public class DiffLine
{
    public DiffLineKind Kind { get; set; }
    public int? OldNumber { get; set; }
    public int? NewNumber { get; set; }
    public string? OldText { get; set; }
    public string? NewText { get; set; }

    /// <summary>Row inside the section, from 1.</summary>
    public int Row { get; set; }

    /// <summary>The line split into unchanged and changed words; null when the whole line counts as changed.</summary>
    public IReadOnlyList<DiffPiece>? Pieces { get; set; }
}

public class DiffSection
{
    public DiffSectionKind Kind { get; set; }

    /// <summary>Number of the change, from 1 (Change sections only).</summary>
    public int ChangeNumber { get; set; }

    public int FoldedLines { get; set; }

    /// <summary>Context lines, or the removed lines followed by the added lines of one change.</summary>
    public List<DiffLine> Lines { get; set; } = new();
}

public class VersionDiffOptions
{
    /// <summary>Leading, trailing and repeated whitespace does not count as a change.</summary>
    public bool IgnoreWhitespace { get; set; }

    /// <summary>Show every unchanged line instead of folding them.</summary>
    public bool ShowAll { get; set; }
}

/// <summary>The text diff of two versions of a document. Built by <c>IVersionDiffService</c>.</summary>
public class VersionDiff
{
    public VersionDiffStatus Status { get; set; }

    /// <summary>Why there is no diff (every status except Changed).</summary>
    public string? Note { get; set; }

    public List<DiffSection> Sections { get; set; } = new();

    public int AddedLines { get; set; }
    public int RemovedLines { get; set; }
    public int Changes { get; set; }
    public int FoldedLines { get; set; }

    /// <summary>Each side is read up to this many bytes of text.</summary>
    public const int TextLimitBytes = DocumentPreview.TextLimitBytes;

    /// <summary>Each side may have this many lines.</summary>
    public const int LineLimit = 5000;

    /// <summary>Unchanged lines kept around a change.</summary>
    public const int ContextLines = 3;
}
