using System.Text;
using System.Text.RegularExpressions;

namespace ServicesLayer;

internal static class TextEncodingHelper
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Regex MojibakeSignalRegex = new(@"(?:Ã.|Â.|Ä.|Æ.|á[º»].|�)", RegexOptions.Compiled);

    static TextEncodingHelper()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string Decode(ReadOnlySpan<byte> bytes, string? declaredCharset = null)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var decoded = DecodeWithBom(bytes)
                      ?? DecodeWithDeclaredCharset(bytes, declaredCharset)
                      ?? DecodeUtf8(bytes)
                      ?? DecodeWithCodePage(bytes, 1258)
                      ?? Encoding.Latin1.GetString(bytes);

        return RepairMojibakeIfLikely(decoded).Normalize(NormalizationForm.FormC);
    }

    public static string NormalizeForIndexing(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return RepairMojibakeIfLikely(text).Normalize(NormalizationForm.FormC);
    }

    public static string Decode(Stream stream, string? declaredCharset = null)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return Decode(memory.ToArray(), declaredCharset);
    }

    private static string? DecodeWithBom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            return Encoding.UTF32.GetString(bytes[4..]);
        }

        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            return Encoding.GetEncoding("utf-32BE").GetString(bytes[4..]);
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return StrictUtf8.GetString(bytes[3..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes[2..]);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        }

        return null;
    }

    private static string? DecodeWithDeclaredCharset(ReadOnlySpan<byte> bytes, string? declaredCharset)
    {
        if (string.IsNullOrWhiteSpace(declaredCharset))
        {
            return null;
        }

        var charset = declaredCharset.Trim().Trim('"', '\'');
        try
        {
            var encoding = Encoding.GetEncoding(
                charset,
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
            return encoding.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? DecodeUtf8(ReadOnlySpan<byte> bytes)
    {
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static string? DecodeWithCodePage(ReadOnlySpan<byte> bytes, int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage).GetString(bytes);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string RepairMojibakeIfLikely(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || MojibakeSignalRegex.Matches(text).Count < 2)
        {
            return text;
        }

        try
        {
            if (!TryGetMojibakeBytes(text, out var mojibakeBytes))
            {
                return text;
            }

            var recovered = StrictUtf8.GetString(mojibakeBytes);
            var originalMojibakeSignals = MojibakeSignalRegex.Matches(text).Count;
            var recoveredMojibakeSignals = MojibakeSignalRegex.Matches(recovered).Count;
            if (!recovered.Contains((char)0xFFFD) && recoveredMojibakeSignals < originalMojibakeSignals)
            {
                return recovered;
            }

            return TextQualityScore(recovered) > TextQualityScore(text) ? recovered : text;
        }
        catch (EncoderFallbackException)
        {
            return text;
        }
        catch (DecoderFallbackException)
        {
            return text;
        }
    }

    private static bool TryGetMojibakeBytes(string text, out byte[] bytes)
    {
        var buffer = new byte[text.Length];
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character <= (char)0x00FF)
            {
                buffer[index] = (byte)character;
                continue;
            }

            if (!TryMapWindows1252Character(character, out var mappedByte))
            {
                bytes = Array.Empty<byte>();
                return false;
            }

            buffer[index] = mappedByte;
        }

        bytes = buffer;
        return true;
    }

    private static bool TryMapWindows1252Character(char character, out byte value)
    {
        value = character switch
        {
            (char)0x20AC => 0x80,
            (char)0x201A => 0x82,
            (char)0x0192 => 0x83,
            (char)0x201E => 0x84,
            (char)0x2026 => 0x85,
            (char)0x2020 => 0x86,
            (char)0x2021 => 0x87,
            (char)0x02C6 => 0x88,
            (char)0x2030 => 0x89,
            (char)0x0160 => 0x8A,
            (char)0x2039 => 0x8B,
            (char)0x0152 => 0x8C,
            (char)0x017D => 0x8E,
            (char)0x2018 => 0x91,
            (char)0x2019 => 0x92,
            (char)0x201C => 0x93,
            (char)0x201D => 0x94,
            (char)0x2022 => 0x95,
            (char)0x2013 => 0x96,
            (char)0x2014 => 0x97,
            (char)0x02DC => 0x98,
            (char)0x2122 => 0x99,
            (char)0x0161 => 0x9A,
            (char)0x203A => 0x9B,
            (char)0x0153 => 0x9C,
            (char)0x017E => 0x9E,
            (char)0x0178 => 0x9F,
            _ => 0
        };

        return value != 0;
    }

    private static int TextQualityScore(string text)
    {
        var replacementCharacters = text.Count(character => character == (char)0xFFFD);
        var mojibakeSignals = MojibakeSignalRegex.Matches(text).Count;
        var vietnameseCharacters = text.Count(IsVietnameseCharacter);
        return (vietnameseCharacters * 3) - (mojibakeSignals * 8) - (replacementCharacters * 12);
    }

    private static bool IsVietnameseCharacter(char character)
    {
        const string vietnameseCharacters =
            "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ"
            + "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ";
        return vietnameseCharacters.Contains(character);
    }
}
