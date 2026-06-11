using System.Text;
using System.Text.RegularExpressions;
using ServicesLayer;

namespace ServicesLayer.Tests;

public sealed class FlmSyllabusAwareTextChunkerTests
{
    [Fact]
    public async Task CreateChunkingResultAsync_Dba103_SplitsSemanticSyllabusBlocks()
    {
        var text = await ReadFlmFixtureAsync("FLM-Syllabus-11835-DBA103.txt");
        var result = await new FlmSyllabusAwareTextChunker().CreateChunkingResultAsync(text);
        var chunks = result.Chunks;

        Assert.Equal("flm-syllabus-aware-v1", result.StrategyName);
        Assert.Contains(chunks, chunk => chunk.Text.Contains("Course: DBA103", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Text.Contains("Số tín chỉ: 3", StringComparison.Ordinal));
        Assert.Equal(2, CountItemChunks(chunks, "CLO"));
        Assert.Equal(3, CountAssessmentChunks(chunks));
        Assert.Equal(30, CountSessionChunks(chunks));
        AssertNoMojibake(chunks);
        AssertNoDetailedChunkMixesDifferentSessions(chunks);

        var finalExamChunk = chunks.First(chunk =>
            chunk.Text.Contains("Section: ĐÁNH GIÁ", StringComparison.Ordinal)
            && chunk.Text.Contains("Item: Thi cuối kỳ", StringComparison.Ordinal));
        Assert.DoesNotContain("Participation", finalExamChunk.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateChunkingResultAsync_Iot102_SplitsCloAssessmentAndSessions()
    {
        var text = await ReadFlmFixtureAsync("FLM-Syllabus-12400-IOT102.txt");
        var result = await new FlmSyllabusAwareTextChunker().CreateChunkingResultAsync(text);
        var chunks = result.Chunks;

        Assert.Equal("flm-syllabus-aware-v1", result.StrategyName);
        Assert.Contains(chunks, chunk => chunk.Text.Contains("Course: IOT102", StringComparison.Ordinal));
        Assert.Contains(chunks, chunk => chunk.Text.Contains("Số tín chỉ: 3", StringComparison.Ordinal));
        Assert.Equal(13, CountItemChunks(chunks, "CLO"));
        Assert.Equal(6, CountAssessmentChunks(chunks));
        Assert.Equal(60, CountSessionChunks(chunks));
        AssertNoMojibake(chunks);
        AssertNoDetailedChunkMixesDifferentSessions(chunks);

        var finalExamChunk = chunks.First(chunk =>
            chunk.Text.Contains("Section: ĐÁNH GIÁ", StringComparison.Ordinal)
            && chunk.Text.Contains("Item: Final exam", StringComparison.Ordinal));
        Assert.DoesNotContain("Active learning", finalExamChunk.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DocumentTextExtractor_RepairsFlmMojibakeBeforeIndexing()
    {
        var text = await ReadFlmFixtureAsync("FLM-Syllabus-12400-IOT102.txt");
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var extracted = await new DocumentTextExtractor().ExtractAsync(stream, "iot102.txt");

        Assert.Contains("Mã môn: IOT102", extracted, StringComparison.Ordinal);
        Assert.Contains("TÀI LIỆU", extracted, StringComparison.OrdinalIgnoreCase);
        AssertNoMojibake(extracted);
        Assert.Equal(extracted.Normalize(NormalizationForm.FormC), extracted);
    }

    private static int CountItemChunks(IReadOnlyList<TextChunk> chunks, string prefix)
    {
        return chunks.Count(chunk => Regex.IsMatch(chunk.Text, $@"(?m)^Item:\s+{Regex.Escape(prefix)}\d+\b"));
    }

    private static int CountAssessmentChunks(IReadOnlyList<TextChunk> chunks)
    {
        return chunks.Count(chunk =>
            chunk.Text.Contains("Section: ĐÁNH GIÁ", StringComparison.Ordinal)
            && chunk.Text.Contains("Tỷ trọng:", StringComparison.Ordinal));
    }

    private static int CountSessionChunks(IReadOnlyList<TextChunk> chunks)
    {
        return chunks.Count(chunk => Regex.IsMatch(chunk.Text, @"(?m)^Session:\s+\d+\b"));
    }

    private static void AssertNoDetailedChunkMixesDifferentSessions(IReadOnlyList<TextChunk> chunks)
    {
        foreach (var chunk in chunks.Where(chunk => chunk.Text.Contains("Section: LỊCH HỌC CHI TIẾT", StringComparison.Ordinal)))
        {
            var sessionNumbers = Regex.Matches(chunk.Text, @"Session\s+(\d+)\s*:", RegexOptions.IgnoreCase)
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .ToList();

            Assert.True(sessionNumbers.Count <= 1, $"Chunk mixes sessions: {chunk.Text}");
        }
    }

    private static void AssertNoMojibake(IReadOnlyList<TextChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            AssertNoMojibake(chunk.Text);
        }
    }

    private static void AssertNoMojibake(string text)
    {
        Assert.DoesNotContain("MÃ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ä", text, StringComparison.Ordinal);
        Assert.DoesNotContain("áº", text, StringComparison.Ordinal);
        Assert.DoesNotContain("á»", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ï¿½", text, StringComparison.Ordinal);
    }

    private static async Task<string> ReadFlmFixtureAsync(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "PresentationLayer",
                "App_Data",
                "flm-imports",
                fileName);
            if (File.Exists(candidate))
            {
                return await File.ReadAllTextAsync(candidate, Encoding.UTF8);
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find FLM fixture {fileName}.");
    }
}
