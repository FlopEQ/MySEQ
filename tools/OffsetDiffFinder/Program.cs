using System.Globalization;
using System.Security.Cryptography;
using System.Text;

const int MaxMatchesToTrack = 4;
int[] radii = [96, 80, 64, 48, 32, 24, 16];

var options = Args.Parse(args);
if (!options.IsValid)
{
    Console.WriteLine("""
MySEQ Offset Diff Finder

Usage:
  offset-diff-finder --old-exe C:\old\eqgame.exe --new-exe C:\new\eqgame.exe --old-ini C:\old\myseqserver.ini --out C:\new\myseqserver.candidates.ini --report C:\new\offset-diff-report.txt

The tool compares references around known old offsets in an old executable against a new executable.
It writes a candidate ini plus a human-readable confidence report.
""");
    return 2;
}

byte[] oldBytes = File.ReadAllBytes(options.OldExe);
byte[] newBytes = File.ReadAllBytes(options.NewExe);
var oldImageBase = PeInfo.TryReadImageBase(oldBytes);
var newImageBase = PeInfo.TryReadImageBase(newBytes);
var ini = IniDocument.Load(options.OldIni);

var results = new List<OffsetResult>();
foreach (var entry in ini.Entries.Where(OffsetEntry.ShouldScan))
{
    var result = FindOffset(entry, oldBytes, newBytes, oldImageBase, newImageBase, radii);
    results.Add(result);

    if (result.CandidateValue.HasValue)
    {
        ini.Set(entry.Section, entry.Key, Hex(result.CandidateValue.Value));
    }
}

ini.Set("File Info", "PatchDate", DateTime.Now.ToShortDateString());
ini.Set("File Info", "ClientHash", Sha1(options.NewExe));
ini.Save(options.OutputIni);
File.WriteAllText(options.ReportPath, BuildReport(options, oldImageBase, newImageBase, results), Encoding.UTF8);

Console.WriteLine($"Wrote candidate ini: {options.OutputIni}");
Console.WriteLine($"Wrote report:        {options.ReportPath}");
Console.WriteLine($"High confidence:     {results.Count(r => r.Confidence >= 80)}/{results.Count}");
Console.WriteLine($"Needs review:        {results.Count(r => r.Confidence < 80)}/{results.Count}");
return results.Any(r => r.CandidateValue is null) ? 1 : 0;

OffsetResult FindOffset(OffsetEntry entry, byte[] oldBytes, byte[] newBytes, ulong? oldImageBase, ulong? newImageBase, int[] radii)
{
    var encodings = OffsetEncoding.Create(entry, oldImageBase);
    var attempts = new List<MatchAttempt>();

    foreach (var encoding in encodings)
    {
        foreach (var oldRef in BinarySearch.FindAll(oldBytes, encoding.Bytes))
        {
            foreach (var radius in radii)
            {
                var signature = Signature.Create(oldBytes, oldRef, encoding.Bytes.Length, radius);
                if (signature == null)
                {
                    continue;
                }

                var matches = Signature.Search(newBytes, signature.Value, MaxMatchesToTrack).ToList();
                attempts.Add(new MatchAttempt(encoding, oldRef, radius, matches.Count));
                if (matches.Count == 1)
                {
                    var newRef = matches[0] + (oldRef - signature.Value.Start);
                    ulong rawCandidate = ReadUnsigned(newBytes, newRef, encoding.Bytes.Length);
                    ulong normalized = entry.IsPrimary
                        ? NormalizePrimary(rawCandidate, encoding, entry.Value, oldImageBase, newImageBase)
                        : rawCandidate;

                    int confidence = Math.Min(98, 72 + (radius / 4) + encoding.Bytes.Length * 2);
                    return new OffsetResult(entry, normalized, confidence, oldRef, newRef, encoding.Description, $"unique {signature.Value.Length}-byte context");
                }
            }
        }
    }

    var best = attempts
        .OrderBy(a => a.MatchCount == 0 ? 99 : a.MatchCount)
        .ThenByDescending(a => a.Radius)
        .FirstOrDefault();

    if (best != null)
    {
        return new OffsetResult(entry, null, 25, best.OldReference, null, best.Encoding.Description, $"no unique match; closest new match count={best.MatchCount}");
    }

    return new OffsetResult(entry, null, 0, null, null, "", "old value was not referenced in old executable");
}

static ulong NormalizePrimary(ulong rawCandidate, OffsetEncoding encoding, ulong oldValue, ulong? oldImageBase, ulong? newImageBase)
{
    if (encoding.Bytes.Length == 8 || newImageBase is null)
    {
        return rawCandidate;
    }

    uint newBaseLow = (uint)(newImageBase.Value & 0xffffffff);
    if (rawCandidate >= newBaseLow)
    {
        return newImageBase.Value + (rawCandidate - newBaseLow);
    }

    if (oldImageBase.HasValue && oldValue >= oldImageBase.Value)
    {
        return newImageBase.Value + rawCandidate;
    }

    return rawCandidate;
}

static ulong ReadUnsigned(byte[] bytes, int offset, int length)
{
    return length switch
    {
        1 => bytes[offset],
        2 => BitConverter.ToUInt16(bytes, offset),
        4 => BitConverter.ToUInt32(bytes, offset),
        8 => BitConverter.ToUInt64(bytes, offset),
        _ => throw new ArgumentOutOfRangeException(nameof(length))
    };
}

static string BuildReport(Args options, ulong? oldImageBase, ulong? newImageBase, List<OffsetResult> results)
{
    var sb = new StringBuilder();
    sb.AppendLine("MySEQ Offset Diff Finder Report");
    sb.AppendLine("===============================");
    sb.AppendLine($"Old exe:       {options.OldExe}");
    sb.AppendLine($"New exe:       {options.NewExe}");
    sb.AppendLine($"Old ini:       {options.OldIni}");
    sb.AppendLine($"Candidate ini: {options.OutputIni}");
    sb.AppendLine($"Old imagebase: {(oldImageBase.HasValue ? Hex(oldImageBase.Value) : "unknown")}");
    sb.AppendLine($"New imagebase: {(newImageBase.HasValue ? Hex(newImageBase.Value) : "unknown")}");
    sb.AppendLine();
    sb.AppendLine("Confidence guide: 80+ is likely usable; below 80 should be checked in the debugger.");
    sb.AppendLine();

    foreach (var group in results.GroupBy(r => r.Entry.Section))
    {
        sb.AppendLine($"[{group.Key}]");
        foreach (var result in group)
        {
            string oldValue = Hex(result.Entry.Value);
            string candidate = result.CandidateValue.HasValue ? Hex(result.CandidateValue.Value) : "NOT FOUND";
            string oldRef = result.OldReference.HasValue ? Hex((ulong)result.OldReference.Value) : "-";
            string newRef = result.NewReference.HasValue ? Hex((ulong)result.NewReference.Value) : "-";
            sb.AppendLine($"{result.Entry.Key,-22} old={oldValue,-12} new={candidate,-12} confidence={result.Confidence,3} oldRef={oldRef,-10} newRef={newRef,-10} {result.Note}");
        }
        sb.AppendLine();
    }

    return sb.ToString();
}

static string Hex(ulong value) => $"0x{value:x}";

static string Sha1(string path)
{
    using var stream = File.OpenRead(path);
    return Convert.ToHexString(SHA1.HashData(stream)).ToLowerInvariant();
}

sealed record Args(string OldExe, string NewExe, string OldIni, string OutputIni, string ReportPath)
{
    public bool IsValid => File.Exists(OldExe) && File.Exists(NewExe) && File.Exists(OldIni)
        && !string.IsNullOrWhiteSpace(OutputIni) && !string.IsNullOrWhiteSpace(ReportPath);

    public static Args Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                values[args[i]] = args[++i];
            }
        }

        string oldIni = values.GetValueOrDefault("--old-ini", "");
        string output = values.GetValueOrDefault("--out", Path.Combine(Path.GetDirectoryName(oldIni) ?? ".", "myseqserver.candidates.ini"));
        string report = values.GetValueOrDefault("--report", Path.ChangeExtension(output, ".report.txt"));
        return new Args(
            values.GetValueOrDefault("--old-exe", ""),
            values.GetValueOrDefault("--new-exe", ""),
            oldIni,
            output,
            report);
    }
}

sealed record OffsetEntry(string Section, string Key, ulong Value)
{
    public bool IsPrimary => Section.Equals("Memory Offsets", StringComparison.OrdinalIgnoreCase);

    public static bool ShouldScan(OffsetEntry entry)
    {
        if (entry.Value == 0)
        {
            return false;
        }

        return entry.Section.Equals("Memory Offsets", StringComparison.OrdinalIgnoreCase)
            || entry.Section.EndsWith("Offsets", StringComparison.OrdinalIgnoreCase);
    }
}

sealed class IniDocument
{
    private readonly List<string> lines;
    private readonly List<OffsetEntry> entries;

    private IniDocument(List<string> lines, List<OffsetEntry> entries)
    {
        this.lines = lines;
        this.entries = entries;
    }

    public IReadOnlyList<OffsetEntry> Entries => entries;

    public static IniDocument Load(string path)
    {
        var lines = File.ReadAllLines(path).ToList();
        var entries = new List<OffsetEntry>();
        string section = "";

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                section = trimmed.Trim('[', ']');
                continue;
            }

            int equal = trimmed.IndexOf('=');
            if (equal <= 0)
            {
                continue;
            }

            string key = trimmed[..equal].Trim();
            string valueText = trimmed[(equal + 1)..].Trim();
            if (TryParseNumber(valueText, out var value))
            {
                entries.Add(new OffsetEntry(section, key, value));
            }
        }

        return new IniDocument(lines, entries);
    }

    public void Set(string section, string key, string value)
    {
        string currentSection = "";
        for (int i = 0; i < lines.Count; i++)
        {
            string trimmed = lines[i].Trim();
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed.Trim('[', ']');
                continue;
            }

            if (!currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int equal = trimmed.IndexOf('=');
            if (equal > 0 && trimmed[..equal].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = $"{key}={value}";
                return;
            }
        }

        lines.Add("");
        lines.Add($"[{section}]");
        lines.Add($"{key}={value}");
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllLines(path, lines, Encoding.UTF8);
    }

    private static bool TryParseNumber(string text, out ulong value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return ulong.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}

sealed record OffsetEncoding(byte[] Bytes, string Description)
{
    public static List<OffsetEncoding> Create(OffsetEntry entry, ulong? oldImageBase)
    {
        var encodings = new List<OffsetEncoding>();
        if (entry.IsPrimary)
        {
            AddUnique(encodings, BitConverter.GetBytes(entry.Value), "absolute 64-bit value");
            AddUnique(encodings, BitConverter.GetBytes((uint)(entry.Value & 0xffffffff)), "low 32-bit value");
            if (oldImageBase.HasValue && entry.Value >= oldImageBase.Value)
            {
                AddUnique(encodings, BitConverter.GetBytes((uint)(entry.Value - oldImageBase.Value)), "32-bit RVA");
            }
        }
        else
        {
            AddUnique(encodings, BitConverter.GetBytes((uint)entry.Value), "32-bit structure offset");
            if (entry.Value <= ushort.MaxValue)
            {
                AddUnique(encodings, BitConverter.GetBytes((ushort)entry.Value), "16-bit structure offset");
            }

            if (entry.Value <= byte.MaxValue)
            {
                AddUnique(encodings, [(byte)entry.Value], "8-bit structure offset");
            }
        }

        return encodings;
    }

    private static void AddUnique(List<OffsetEncoding> encodings, byte[] bytes, string description)
    {
        if (!encodings.Any(e => e.Bytes.SequenceEqual(bytes)))
        {
            encodings.Add(new OffsetEncoding(bytes, description));
        }
    }
}

static class BinarySearch
{
    public static IEnumerable<int> FindAll(byte[] data, byte[] needle)
    {
        if (needle.Length == 0 || data.Length < needle.Length)
        {
            yield break;
        }

        for (int i = 0; i <= data.Length - needle.Length; i++)
        {
            bool matched = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                yield return i;
            }
        }
    }
}

readonly record struct Signature(byte[] Bytes, bool[] Wildcards, int Start, int Length)
{
    public static Signature? Create(byte[] data, int reference, int referenceLength, int radius)
    {
        int start = Math.Max(0, reference - radius);
        int end = Math.Min(data.Length, reference + referenceLength + radius);
        int length = end - start;
        if (length <= referenceLength + 8)
        {
            return null;
        }

        var bytes = new byte[length];
        var wildcards = new bool[length];
        Array.Copy(data, start, bytes, 0, length);
        for (int i = reference - start; i < reference - start + referenceLength; i++)
        {
            wildcards[i] = true;
        }

        return new Signature(bytes, wildcards, start, length);
    }

    public static IEnumerable<int> Search(byte[] data, Signature signature, int maxMatches)
    {
        int found = 0;
        for (int i = 0; i <= data.Length - signature.Length; i++)
        {
            bool matched = true;
            for (int j = 0; j < signature.Length; j++)
            {
                if (!signature.Wildcards[j] && data[i + j] != signature.Bytes[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                yield return i;
                found++;
                if (found >= maxMatches)
                {
                    yield break;
                }
            }
        }
    }
}

static class PeInfo
{
    public static ulong? TryReadImageBase(byte[] bytes)
    {
        try
        {
            if (bytes.Length < 0x100 || bytes[0] != 'M' || bytes[1] != 'Z')
            {
                return null;
            }

            int peOffset = BitConverter.ToInt32(bytes, 0x3c);
            if (peOffset <= 0 || peOffset + 0x40 >= bytes.Length)
            {
                return null;
            }

            ushort magic = BitConverter.ToUInt16(bytes, peOffset + 24);
            return magic switch
            {
                0x20b => BitConverter.ToUInt64(bytes, peOffset + 24 + 24),
                0x10b => BitConverter.ToUInt32(bytes, peOffset + 24 + 28),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}

sealed record MatchAttempt(OffsetEncoding Encoding, int OldReference, int Radius, int MatchCount);

sealed record OffsetResult(
    OffsetEntry Entry,
    ulong? CandidateValue,
    int Confidence,
    int? OldReference,
    int? NewReference,
    string Encoding,
    string Note);
