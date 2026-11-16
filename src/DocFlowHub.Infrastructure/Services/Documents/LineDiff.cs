using System.Text;
using DiffPlex;
using DiffPlex.Chunkers;
using DocFlowHub.Core.Models.Documents.Browser;

namespace DocFlowHub.Infrastructure.Services.Documents;

/// <summary>
/// Turns two texts into the sections the Compare page shows (Sprint 21): changes with their removed and
/// added lines, the context around them and folds for the rest. DiffPlex finds the changes; the layout
/// (pairing, folding, word pieces) is ours.
/// </summary>
public static class LineDiff
{
    /// <summary>Lines longer than this are not compared word by word.</summary>
    private const int WordDiffMaxLength = 2000;

    /// <summary>A changed line keeps its word marks only when at least this share of it is unchanged.</summary>
    private const double WordDiffMinCommon = 0.3;

    /// <summary>The lines of a text: "\n" separated, without the empty line after a final newline.</summary>
    public static string[] Lines(string? text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        return lines[^1].Length == 0 ? lines[..^1] : lines;
    }

    public static VersionDiff Compare(string[] oldLines, string[] newLines, VersionDiffOptions? options = null)
    {
        options ??= new VersionDiffOptions();
        var blocks = Blocks(oldLines, newLines, options.IgnoreWhitespace);
        var diff = new VersionDiff { Status = blocks.Count == 0 ? VersionDiffStatus.Identical : VersionDiffStatus.Changed };
        if (blocks.Count == 0) return diff;

        var context = VersionDiff.ContextLines;
        var oldAt = 0;   // next unchanged line of the old text
        var newAt = 0;
        for (var b = 0; b < blocks.Count; b++)
        {
            var block = blocks[b];
            AddUnchanged(diff, oldLines, newLines, oldAt, newAt, block.OldStart - oldAt,
                head: b == 0 ? 0 : context, tail: context, options.ShowAll);

            var section = new DiffSection { Kind = DiffSectionKind.Change, ChangeNumber = b + 1 };
            var paired = Math.Min(block.OldCount, block.NewCount);
            var pieces = new (IReadOnlyList<DiffPiece>? Old, IReadOnlyList<DiffPiece>? New)[paired];
            for (var i = 0; i < paired; i++)
            {
                pieces[i] = WordPieces(oldLines[block.OldStart + i], newLines[block.NewStart + i]);
            }
            for (var i = 0; i < block.OldCount; i++)
            {
                section.Lines.Add(new DiffLine
                {
                    Kind = DiffLineKind.Removed,
                    OldNumber = block.OldStart + i + 1,
                    OldText = oldLines[block.OldStart + i],
                    Row = i + 1,
                    Pieces = i < paired ? pieces[i].Old : null,
                });
            }
            for (var i = 0; i < block.NewCount; i++)
            {
                section.Lines.Add(new DiffLine
                {
                    Kind = DiffLineKind.Added,
                    NewNumber = block.NewStart + i + 1,
                    NewText = newLines[block.NewStart + i],
                    Row = i + 1,
                    Pieces = i < paired ? pieces[i].New : null,
                });
            }
            diff.Sections.Add(section);
            diff.RemovedLines += block.OldCount;
            diff.AddedLines += block.NewCount;

            oldAt = block.OldStart + block.OldCount;
            newAt = block.NewStart + block.NewCount;
        }
        AddUnchanged(diff, oldLines, newLines, oldAt, newAt, oldLines.Length - oldAt, head: context, tail: 0, options.ShowAll);

        diff.Changes = blocks.Count;
        return diff;
    }

    private sealed record Block(int OldStart, int OldCount, int NewStart, int NewCount);

    private static List<Block> Blocks(string[] oldLines, string[] newLines, bool ignoreWhitespace)
    {
        // DiffPlex reads "" as one empty line, so the empty sides are handled here.
        if (oldLines.Length == 0 && newLines.Length == 0) return new List<Block>();
        if (oldLines.Length == 0) return new List<Block> { new(0, 0, 0, newLines.Length) };
        if (newLines.Length == 0) return new List<Block> { new(0, oldLines.Length, 0, 0) };

        var oldKeys = ignoreWhitespace ? oldLines.Select(Squash).ToArray() : oldLines;
        var newKeys = ignoreWhitespace ? newLines.Select(Squash).ToArray() : newLines;
        var result = Differ.Instance.CreateDiffs(
            string.Join('\n', oldKeys), string.Join('\n', newKeys), ignoreWhiteSpace: false, ignoreCase: false, NewlineChunker.Instance);
        return result.DiffBlocks
            .Select(b => new Block(b.DeleteStartA, b.DeleteCountA, b.InsertStartB, b.InsertCountB))
            .ToList();
    }

    /// <summary>Splits on "\n" only, so the pieces are exactly our lines.</summary>
    private sealed class NewlineChunker : IChunker
    {
        public static readonly NewlineChunker Instance = new();
        public IReadOnlyList<string> Chunk(string text) => text.Split('\n');
    }

    /// <summary>A line without leading, trailing and repeated whitespace.</summary>
    private static string Squash(string line)
    {
        var builder = new StringBuilder(line.Length);
        var space = false;
        foreach (var c in line)
        {
            if (char.IsWhiteSpace(c))
            {
                space = builder.Length > 0;
                continue;
            }
            if (space) builder.Append(' ');
            space = false;
            builder.Append(c);
        }
        return builder.ToString();
    }

    private static void AddUnchanged(
        VersionDiff diff, string[] oldLines, string[] newLines, int oldStart, int newStart, int count, int head, int tail, bool showAll)
    {
        if (count <= 0) return;

        // Folding fewer than two lines would hide less than the fold row takes.
        var folded = showAll ? 0 : count - head - tail;
        if (folded < 2)
        {
            diff.Sections.Add(Context(oldLines, newLines, oldStart, newStart, count));
            return;
        }

        if (head > 0) diff.Sections.Add(Context(oldLines, newLines, oldStart, newStart, head));
        diff.Sections.Add(new DiffSection { Kind = DiffSectionKind.Fold, FoldedLines = folded });
        diff.FoldedLines += folded;
        if (tail > 0) diff.Sections.Add(Context(oldLines, newLines, oldStart + head + folded, newStart + head + folded, tail));
    }

    private static DiffSection Context(string[] oldLines, string[] newLines, int oldStart, int newStart, int count)
    {
        var section = new DiffSection { Kind = DiffSectionKind.Context };
        for (var i = 0; i < count; i++)
        {
            section.Lines.Add(new DiffLine
            {
                Kind = DiffLineKind.Context,
                OldNumber = oldStart + i + 1,
                NewNumber = newStart + i + 1,
                OldText = oldLines[oldStart + i],
                NewText = newLines[newStart + i],
                Row = i + 1,
            });
        }
        return section;
    }

    /// <summary>
    /// The changed words of a line pair. Null for both when the lines have too little in common: marking
    /// almost every word says less than the red and green lines already do.
    /// </summary>
    private static (IReadOnlyList<DiffPiece>? Old, IReadOnlyList<DiffPiece>? New) WordPieces(string oldLine, string newLine)
    {
        if (oldLine.Length == 0 || newLine.Length == 0 || oldLine.Length > WordDiffMaxLength || newLine.Length > WordDiffMaxLength)
        {
            return (null, null);
        }

        var result = Differ.Instance.CreateDiffs(oldLine, newLine, ignoreWhiteSpace: false, ignoreCase: false, WordChunker.Instance);
        var oldChanged = new bool[result.PiecesOld.Count];
        var newChanged = new bool[result.PiecesNew.Count];
        foreach (var block in result.DiffBlocks)
        {
            for (var i = 0; i < block.DeleteCountA; i++) oldChanged[block.DeleteStartA + i] = true;
            for (var i = 0; i < block.InsertCountB; i++) newChanged[block.InsertStartB + i] = true;
        }

        var common = 0;
        for (var i = 0; i < oldChanged.Length; i++)
        {
            if (!oldChanged[i]) common += result.PiecesOld[i].Trim().Length;
        }
        if (common < WordDiffMinCommon * Math.Max(oldLine.Trim().Length, newLine.Trim().Length))
        {
            return (null, null);
        }
        return (Merge(result.PiecesOld, oldChanged), Merge(result.PiecesNew, newChanged));
    }

    /// <summary>Joins neighbouring pieces with the same state; whitespace between two changed words is changed too.</summary>
    private static List<DiffPiece> Merge(IReadOnlyList<string> pieces, bool[] changed)
    {
        for (var i = 1; i < pieces.Count - 1; i++)
        {
            if (!changed[i] && changed[i - 1] && changed[i + 1] && string.IsNullOrWhiteSpace(pieces[i])) changed[i] = true;
        }

        var merged = new List<DiffPiece>();
        var text = new StringBuilder();
        for (var i = 0; i < pieces.Count; i++)
        {
            text.Append(pieces[i]);
            if (i == pieces.Count - 1 || changed[i + 1] != changed[i])
            {
                merged.Add(new DiffPiece(text.ToString(), changed[i]));
                text.Clear();
            }
        }
        return merged;
    }
}
