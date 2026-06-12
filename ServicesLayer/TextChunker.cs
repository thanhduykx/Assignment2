using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ServicesLayer;

public sealed record TextChunk(
    int ChunkIndex,
    string Text,
    string SectionTitle,
    int CharStart,
    int CharEnd);

public sealed record TextChunkingResult(
    IReadOnlyList<TextChunk> Chunks,
    string StrategyName);

public interface ITextChunker
{
    string StrategyName { get; }
    IReadOnlyList<TextChunk> CreateChunks(string text);
    Task<TextChunkingResult> CreateChunkingResultAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class ParagraphAwareTextChunker : ITextChunker
{
    private const int TargetSize = 950;
    private const int MaxSize = 1200;
    private const int Overlap = 0;

    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NumberedHeadingRegex = new(@"^\d+(\.\d+)*[\).:-]?\s+\S+", RegexOptions.Compiled);
    private static readonly Regex NamedHeadingRegex = new(@"^(chapter|section|unit|lesson|week|module|part)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string StrategyName => $"paragraph-aware-{TargetSize}-{Overlap}";

    public Task<TextChunkingResult> CreateChunkingResultAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TextChunkingResult(CreateChunks(text), StrategyName));
    }

    public IReadOnlyList<TextChunk> CreateChunks(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<TextChunk>();
        }

        var blocks = CreateBlocks(normalized);
        if (blocks.Count == 0)
        {
            return Array.Empty<TextChunk>();
        }

        var chunks = new List<TextChunk>();
        var builder = new StringBuilder();
        var chunkStart = 0;
        var chunkEnd = 0;
        var chunkSection = string.Empty;

        foreach (var block in blocks)
        {
            if (builder.Length == 0)
            {
                chunkStart = block.Start;
                chunkSection = block.SectionTitle;
            }

            var separatorLength = builder.Length == 0 ? 0 : Environment.NewLine.Length * 2;
            if (builder.Length > 0 && builder.Length + separatorLength + block.Text.Length > MaxSize)
            {
                AddChunk(chunks, builder.ToString(), chunkSection, chunkStart, chunkEnd);
                builder.Clear();
                chunkStart = block.Start;
                chunkSection = block.SectionTitle;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.Append(block.Text);
            chunkEnd = block.End;
            if (string.IsNullOrWhiteSpace(chunkSection))
            {
                chunkSection = block.SectionTitle;
            }
        }

        AddChunk(chunks, builder.ToString(), chunkSection, chunkStart, chunkEnd);
        return chunks;
    }

    private static void AddChunk(List<TextChunk> chunks, string text, string sectionTitle, int start, int end)
    {
        var chunkText = text.Trim();
        if (string.IsNullOrWhiteSpace(chunkText))
        {
            return;
        }

        chunks.Add(new TextChunk(
            chunks.Count + 1,
            chunkText,
            sectionTitle.Trim(),
            Math.Max(0, start),
            Math.Max(start, end)));
    }

    private static IReadOnlyList<TextBlock> CreateBlocks(string normalized)
    {
        var blocks = new List<TextBlock>();
        var sectionTitle = string.Empty;
        var searchStart = 0;

        foreach (var line in normalized.Split('\n'))
        {
            var blockText = line.Trim();
            if (string.IsNullOrWhiteSpace(blockText))
            {
                searchStart += line.Length + 1;
                continue;
            }

            var start = normalized.IndexOf(blockText, searchStart, StringComparison.Ordinal);
            if (start < 0)
            {
                start = Math.Min(searchStart, normalized.Length);
            }

            var end = Math.Min(normalized.Length, start + blockText.Length);
            searchStart = Math.Min(normalized.Length, end + 1);

            if (IsHeading(blockText))
            {
                sectionTitle = blockText;
            }

            blocks.AddRange(SplitLongBlock(blockText, sectionTitle, start));
        }

        return blocks;
    }

    private static IEnumerable<TextBlock> SplitLongBlock(string text, string sectionTitle, int blockStart)
    {
        var offset = 0;
        while (offset < text.Length)
        {
            var remaining = text.Length - offset;
            var length = Math.Min(TargetSize, remaining);
            var end = offset + length;

            if (end < text.Length)
            {
                var boundary = text.LastIndexOf(' ', end - 1, length);
                if (boundary > offset + TargetSize / 2)
                {
                    end = boundary;
                }
            }

            var slice = text[offset..end].Trim();
            if (!string.IsNullOrWhiteSpace(slice))
            {
                yield return new TextBlock(slice, sectionTitle, blockStart + offset, blockStart + end);
            }

            offset = end;
            while (offset < text.Length && char.IsWhiteSpace(text[offset]))
            {
                offset++;
            }
        }
    }

    private static bool IsHeading(string line)
    {
        if (line.Length > 120)
        {
            return false;
        }

        if (NamedHeadingRegex.IsMatch(line) || NumberedHeadingRegex.IsMatch(line) || line.EndsWith(':'))
        {
            return true;
        }

        var letters = line.Where(char.IsLetter).ToList();
        if (letters.Count < 4)
        {
            return false;
        }

        var upperRatio = letters.Count(char.IsUpper) / (double)letters.Count;
        return upperRatio >= 0.65;
    }

    private static string Normalize(string text)
    {
        var lines = (text ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => SpaceRegex.Replace(line.Trim(), " "));

        return string.Join('\n', lines).Trim();
    }

    private sealed record TextBlock(string Text, string SectionTitle, int Start, int End);
}

public sealed class FlmSyllabusAwareTextChunker : ITextChunker
{
    private const int MaxSemanticChunkLength = 1800;
    private static readonly Regex HeadingRegex = new(@"^={5,}\s*(?<title>.+?)\s*={5,}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex CourseCodeRegex = new(@"(?im)^\s*Mã môn:\s*(?<code>[A-Z]{2,}\d{2,})\b");
    private static readonly Regex SourceRegex = new(@"(?im)^\s*Nguồn:\s*(?<source>.+)$");
    private static readonly Regex CloRegex = new(@"(?im)^\s*CLO\s*(?<number>\d+)\s*:\s*$");
    private static readonly Regex AssessmentItemRegex = new(@"(?im)^\s*(?<number>\d+)\.\s*(?<name>[^:\r\n]+):\s*$");
    private static readonly Regex SessionRegex = new(@"(?im)^\s*Session\s+(?<number>\d+)\s*:\s*$");
    private static readonly Regex StageRegex = new(@"(?im)^\s*Giai\s+đoạn\s+(?<number>\d+)\s*[-–]\s*(?<name>.+):\s*$");
    private static readonly Regex NumberedItemRegex = new(@"(?im)^\s*(?<number>\d+)\.\s*(?<name>.+):\s*$");
    private static readonly Regex LabelHeadingRegex = new(@"(?im)^(?<label>[^\r\n:]{3,80}):\s*$");
    private static readonly Regex SpaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly ParagraphAwareTextChunker _fallback = new();

    public string StrategyName => "flm-syllabus-aware-v1";

    public Task<TextChunkingResult> CreateChunkingResultAsync(string text, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TextChunkingResult(CreateChunks(text), StrategyName));
    }

    public IReadOnlyList<TextChunk> CreateChunks(string text)
    {
        var normalized = Normalize(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<TextChunk>();
        }

        var headingMatches = HeadingRegex.Matches(normalized);
        if (headingMatches.Count == 0)
        {
            return _fallback.CreateChunks(normalized);
        }

        var sections = ParseSections(normalized, headingMatches);
        if (sections.Count == 0)
        {
            return _fallback.CreateChunks(normalized);
        }

        var metadata = ExtractMetadata(normalized);
        var chunks = new List<TextChunk>();
        foreach (var section in sections)
        {
            AddSectionChunks(chunks, metadata, section);
        }

        return chunks.Count == 0 ? _fallback.CreateChunks(normalized) : chunks;
    }

    private static IReadOnlyList<FlmSection> ParseSections(string normalized, MatchCollection headingMatches)
    {
        var sections = new List<FlmSection>();
        for (var index = 0; index < headingMatches.Count; index++)
        {
            var heading = headingMatches[index];
            var bodyStart = heading.Index + heading.Length;
            var bodyEnd = index + 1 < headingMatches.Count ? headingMatches[index + 1].Index : normalized.Length;
            var body = normalized[bodyStart..bodyEnd].Trim();
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            sections.Add(new FlmSection(
                heading.Groups["title"].Value.Trim(),
                body,
                heading.Index,
                bodyEnd));
        }

        return sections;
    }

    private static FlmMetadata ExtractMetadata(string normalized)
    {
        var source = SourceRegex.Match(normalized).Groups["source"].Value.Trim();
        var courseCode = CourseCodeRegex.Match(normalized).Groups["code"].Value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(source))
        {
            source = "FLM Syllabus";
        }

        if (string.IsNullOrWhiteSpace(courseCode))
        {
            var sourceCode = Regex.Match(source, @"\b[A-Z]{2,}\d{2,}\b");
            courseCode = sourceCode.Success ? sourceCode.Value.ToUpperInvariant() : "UNKNOWN";
        }

        return new FlmMetadata(courseCode, source);
    }

    private static void AddSectionChunks(List<TextChunk> chunks, FlmMetadata metadata, FlmSection section)
    {
        var key = NormalizeKey(section.Title);
        if (key.Contains("tong quan mon hoc", StringComparison.Ordinal))
        {
            AddSemanticChunk(chunks, metadata, section, "Overview", null, ExtractOverviewFacts(section.Body));
            return;
        }

        if (key.Contains("chuan dau ra clo", StringComparison.Ordinal))
        {
            AddRegexBlocks(chunks, metadata, section, CloRegex, match => $"CLO{match.Groups["number"].Value}", null);
            return;
        }

        if (key.Contains("danh gia", StringComparison.Ordinal) && key.Contains("ty le diem", StringComparison.Ordinal))
        {
            AddAssessmentChunks(chunks, metadata, section);
            return;
        }

        if (key.Contains("tra loi nhanh", StringComparison.Ordinal) || key.Contains("cau hoi thuong gap", StringComparison.Ordinal))
        {
            AddFaqChunks(chunks, metadata, section);
            return;
        }

        if (key.Contains("lich hoc chi tiet", StringComparison.Ordinal))
        {
            AddRegexBlocks(chunks, metadata, section, SessionRegex, match => $"Session {match.Groups["number"].Value}", match => match.Groups["number"].Value);
            return;
        }

        if (key.Contains("lich hoc tom tat", StringComparison.Ordinal) || key.Contains("giai doan", StringComparison.Ordinal))
        {
            AddRegexBlocks(chunks, metadata, section, StageRegex, match => $"Giai đoạn {match.Groups["number"].Value} - {match.Groups["name"].Value.Trim()}", null);
            return;
        }

        if (key.Contains("tai lieu", StringComparison.Ordinal) || key.Contains("nguon hoc", StringComparison.Ordinal))
        {
            if (AddRegexBlocks(chunks, metadata, section, NumberedItemRegex, match => $"{match.Groups["number"].Value}. {match.Groups["name"].Value.Trim()}", null))
            {
                return;
            }
        }

        if (key.Contains("mo ta mon hoc", StringComparison.Ordinal)
            || key.Contains("sinh vien can lam gi", StringComparison.Ordinal)
            || key.Contains("dung cu", StringComparison.Ordinal)
            || key.Contains("phan mem", StringComparison.Ordinal)
            || key.Contains("ky nang mem", StringComparison.Ordinal))
        {
            if (AddRegexBlocks(chunks, metadata, section, LabelHeadingRegex, match => match.Groups["label"].Value.Trim(), null))
            {
                return;
            }
        }

        AddSemanticChunk(chunks, metadata, section, section.Title, null, section.Body);
    }

    private static string ExtractOverviewFacts(string body)
    {
        var importantPrefixes = new[]
        {
            "Mã môn:",
            "Tên môn:",
            "Tên tiếng Anh:",
            "Số tín chỉ:",
            "Trình độ:",
            "Thời lượng:",
            "Tổng thời lượng học tập:",
            "Phân bổ thời gian:",
            "Hình thức học:",
            "Môn tiên quyết:",
            "Điểm đạt môn tối thiểu:",
            "Điểm trung bình tối thiểu",
            "Thang điểm:",
            "Tình trạng:"
        };

        var selected = body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => importantPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var summaryParagraph = body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => Regex.IsMatch(line, @"^[A-Z]{2,}\d{2,}\s+là\s+môn", RegexOptions.IgnoreCase));
        if (!string.IsNullOrWhiteSpace(summaryParagraph))
        {
            selected.Add(summaryParagraph);
        }

        return selected.Count == 0 ? body : string.Join('\n', selected);
    }

    private static void AddAssessmentChunks(List<TextChunk> chunks, FlmMetadata metadata, FlmSection section)
    {
        var matches = AssessmentItemRegex.Matches(section.Body);
        if (matches.Count == 0)
        {
            AddSemanticChunk(chunks, metadata, section, section.Title, null, section.Body);
            return;
        }

        var summary = section.Body[..matches[0].Index].Trim();
        if (!string.IsNullOrWhiteSpace(summary))
        {
            AddSemanticChunk(chunks, metadata, section, "Assessment summary", null, summary);
        }

        AddRegexBlocks(chunks, metadata, section, AssessmentItemRegex, match => match.Groups["name"].Value.Trim(), null);
    }

    private static bool AddRegexBlocks(
        List<TextChunk> chunks,
        FlmMetadata metadata,
        FlmSection section,
        Regex markerRegex,
        Func<Match, string> itemFactory,
        Func<Match, string?>? sessionFactory)
    {
        var matches = markerRegex.Matches(section.Body);
        if (matches.Count == 0)
        {
            return false;
        }

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var blockEnd = index + 1 < matches.Count ? matches[index + 1].Index : section.Body.Length;
            var block = section.Body[match.Index..blockEnd].Trim();
            AddSemanticChunk(chunks, metadata, section, itemFactory(match), sessionFactory?.Invoke(match), block);
        }

        return true;
    }

    private static void AddFaqChunks(List<TextChunk> chunks, FlmMetadata metadata, FlmSection section)
    {
        var lines = section.Body.Split('\n', StringSplitOptions.TrimEntries);
        string? question = null;
        var answer = new StringBuilder();

        void Flush()
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return;
            }

            var body = answer.Length == 0 ? question : $"{question}\n{answer.ToString().Trim()}";
            AddSemanticChunk(chunks, metadata, section, question.Trim(), null, body);
            question = null;
            answer.Clear();
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.TrimEnd().EndsWith("?", StringComparison.Ordinal))
            {
                Flush();
                question = line.Trim();
                continue;
            }

            if (question is null)
            {
                answer.AppendLine(line);
            }
            else
            {
                answer.AppendLine(line);
            }
        }

        Flush();
        if (chunks.All(chunk => chunk.SectionTitle != section.Title))
        {
            AddSemanticChunk(chunks, metadata, section, section.Title, null, section.Body);
        }
    }

    private static void AddSemanticChunk(
        List<TextChunk> chunks,
        FlmMetadata metadata,
        FlmSection section,
        string item,
        string? session,
        string body)
    {
        var compactBody = NormalizeBlock(body);
        if (string.IsNullOrWhiteSpace(compactBody))
        {
            return;
        }

        var parts = SplitBody(compactBody).ToList();
        for (var index = 0; index < parts.Count; index++)
        {
            var itemLabel = parts.Count == 1 ? item : $"{item} (part {index + 1})";
            var text = BuildChunkText(metadata, section.Title, itemLabel, session, parts[index]);
            chunks.Add(new TextChunk(
                chunks.Count + 1,
                text,
                section.Title,
                section.Start,
                section.End));
        }
    }

    private static string BuildChunkText(FlmMetadata metadata, string section, string item, string? session, string body)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Course: {metadata.CourseCode}");
        builder.AppendLine($"Section: {section}");
        if (!string.IsNullOrWhiteSpace(item))
        {
            builder.AppendLine($"Item: {item.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(session))
        {
            builder.AppendLine($"Session: {session.Trim()}");
        }

        builder.AppendLine($"Source: {metadata.Source}");
        builder.AppendLine();
        builder.AppendLine(body.Trim());
        return builder.ToString().Trim();
    }

    private static IEnumerable<string> SplitBody(string body)
    {
        if (body.Length <= MaxSemanticChunkLength)
        {
            yield return body;
            yield break;
        }

        var builder = new StringBuilder();
        foreach (var line in body.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (builder.Length > 0 && builder.Length + line.Length + 1 > MaxSemanticChunkLength)
            {
                yield return builder.ToString().Trim();
                builder.Clear();
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString().Trim();
        }
    }

    private static string Normalize(string text)
    {
        var repaired = TextEncodingHelper.NormalizeForIndexing(text);
        var lines = repaired
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => SpaceRegex.Replace(line.Trim(), " "));

        return string.Join('\n', lines).Trim();
    }

    private static string NormalizeBlock(string text)
    {
        return string.Join(
            '\n',
            (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => SpaceRegex.Replace(line.Trim(), " "))
                .Where(line => !string.IsNullOrWhiteSpace(line)));
    }

    private static string NormalizeKey(string value)
    {
        var normalized = TextEncodingHelper.NormalizeForIndexing(value)
            .Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (character is 'đ' or 'Đ')
            {
                builder.Append('d');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC), @"\s+", " ").Trim();
    }

    private sealed record FlmMetadata(string CourseCode, string Source);
    private sealed record FlmSection(string Title, string Body, int Start, int End);
}
