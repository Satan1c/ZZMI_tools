// #define GENERATOR
// #define GH_GRABBER
//#define LOCAL_GRABBER


using System.Text.Json.Serialization;
#if GENERATOR
using VersionFixerGenerator;
#else
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
#endif


string hashesPath = null;
if (args.Any(x => x is "-p" or "--path"))
{
	var index = args.IndexOf("-p", "--path");
	if (index is not -1) hashesPath = args[index];
}
else
{
	Console.Write("Enter the path to Mods folder\nwhere mods and PlayerCharacterData.json is located\n(leave empty for current directory): ");
	hashesPath = Console.ReadLine();
}

if (string.IsNullOrEmpty(hashesPath)) hashesPath = Directory.GetCurrentDirectory();
if (!Directory.Exists(hashesPath))
{
	Console.WriteLine("Provided path does not exist, exiting.");
	return;
}

Directory.SetCurrentDirectory(hashesPath);

#if GENERATOR
Generator.SaveTo = Path.Combine(hashesPath, "PlayerCharacterData.json");
if (!File.Exists(Generator.SaveTo)) File.Create(Generator.SaveTo).Close();
#if GH_GRABBER
Generator.Run(await GithubGrabber.Run());
#elif LOCAL_GRABBER
Generator.Run(await GithubGrabber.Run());
#endif
#else
if (args.Any(x => x is "-h" or "--help"))
{
	LogOptions();
	Console.WriteLine("Commands:");
	Console.WriteLine("  fix                     Run the fixer (default)");
	Console.WriteLine("  undo                    Revert applied fix");
	return;
}

string action;
if (args.Any(x => x is "fix" or "undo"))
{
	var index = args.IndexOf("fix", "undo");
	action = index is -1 ? "fix" : args[index];
}
else
{
	LogOptions(false);
	Console.Write("\nChose action:\nfix - to run fixer\nundo - to revert applied fix\n(leave empty for fix): ");
	action = Console.ReadLine();
	action = string.IsNullOrEmpty(action) ? "fix" : action;
	var split = action.Split(' ');
	if (split.Length > 1)
	{
		args = split.Skip(1).ToArray();
		action = split[0];
	}
}

if (args.Any(x => x is "-l" or "--logging"))
{
	var index = args.IndexOf("-l", "--logging");
	if (index is not -1)
	{
		var mode = args[index + 1][0];
		if (mode is 'v' or 's' or 'n')
			Logger.LoggingMode = mode switch
			{
				'v' => LogSeverity.Verbose,
				's' => LogSeverity.Standard,
				_ => LogSeverity.None
			};
	}
}

if (args.Any(x => x is "-nd" or "--nodisable"))
{
	_isProcessDisabled = false;
}

switch (action)
{
	case "undo":
	{
		Logger.Log(LogSeverity.Verbose, "Undoing changes...");
		foreach (var path in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "DISABLED_versionfix_*.ini", SearchOption.AllDirectories))
		{
			var match = FixerBackupFilenameRegex.Match(Path.GetFileName(path));
			if (!match.Success)
				continue;
		
			var originalPath = Path.Combine(Path.GetDirectoryName(path)!, match.Groups["name"].Value);
			if (File.Exists(originalPath))
			{
				File.Delete(originalPath);
			}
			File.Move(path, originalPath);
			Logger.Log($"Restored: {originalPath}");
		}
		Logger.Log("Undo complete.");
		break;
	}
	case "fix":
	{
		Logger.Log(LogSeverity.Verbose, "base path");
		Logger.Log(LogSeverity.Verbose, hashesPath);
		Logger.Log(LogSeverity.Verbose);
		
		ReadData(Path.Combine(hashesPath, "PlayerCharacterData.json"));

		if (_data is null)
		{
			Console.WriteLine("No data was loaded, exiting.");
			return;
		}

		Logger.Log();
		foreach (var path in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ini", SearchOption.AllDirectories))
		{
			if (Path.GetFileName(path).StartsWith("DISABLED") && !_isProcessDisabled)
				continue;
			var replacedPathSplit = Path.GetDirectoryName(path)?.Replace('\\', '/').Split('/') ?? [];
			switch (_isProcessDisabled)
			{
				case false when replacedPathSplit.Any(x => x.StartsWith("DISABLED")):
				case true when replacedPathSplit.Any(x => x.StartsWith("DISABLED_versionfix_")):
					continue;
			}


			if (Logger.LoggingMode == LogSeverity.Verbose)
			{
				Logger.Log(LogSeverity.Verbose, "Found ini:");
				Logger.Log(LogSeverity.Verbose, path);
			}

			Run(path);
		}

		Logger.Log("Done");
		Console.ReadKey();
		break;
	}
}

public static partial class Program
{
	[GeneratedRegex(@"^hash\s*=\s*(?<hash>\w{8})$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex GetHashRegex();
	private static readonly Regex HashRegex = GetHashRegex();
	
	[GeneratedRegex(@"DISABLED_versionfix_\d*-(?<name>.+\.ini)", RegexOptions.Compiled)]
	private static partial Regex GetFilenameRegex();
	private static readonly Regex FixerBackupFilenameRegex = GetFilenameRegex();
	[GeneratedRegex(@"^match_first_index\s*=\s*(?<value>\d+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex GetMatchFirstIndexRegex();
	private static readonly Regex MatchFirstIndexRegex = GetMatchFirstIndexRegex();

	[GeneratedRegex(@"^match_index_count\s*=\s*(?<value>\d+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex GetMatchIndexCountRegex();
	private static readonly Regex MatchIndexCountRegex = GetMatchIndexCountRegex();

	private static HashChangeData[]? _data;
	private static bool _isProcessDisabled = true;

	private static void ReadData(string jsonPath)
	{
		if (!File.Exists(jsonPath))
		{
			Console.WriteLine("PlayerCharacterData.json not found!");
			return;
		}
		_data = JsonSerializer.Deserialize(File.ReadAllText(jsonPath), FixerDataCotext.Default.HashChangeDataArray)!;
	}
	
	private static void LogOptions(bool cli = true)
	{
		Console.WriteLine("Options:");
		if (cli)
		{
			Console.WriteLine("  -p, --path \"[path]\"   Change the path to the Mods folder with PlayerCharacterData.json");
			Console.WriteLine("                          Default is current directory");
		}
		Console.WriteLine("  -l, --logging [v|s|n]   Change logging mode: v - verbose, s - standard, n - none");
		Console.WriteLine("                          Default is standard");
		Console.WriteLine("  -nd, --nodisable        Do not process files/folders with disabled prefix");
	}
	
	private static void Run(string iniPath)
	{
		var iniLines = File.ReadLines(iniPath, Encoding.UTF8).ToArray();
		var newIniLines = new string[iniLines.Length];

		var sb = new StringBuilder();
		
		var changed = false;
		string? sectionIdentity = null;
		for (var i = 0; i < iniLines.Length; i++)
		{
			var line = iniLines[i];
			var trimmed = line.TrimStart();
			
			if (trimmed.StartsWith('['))
			{
				sectionIdentity = null;
				newIniLines[i] = line;
				continue;
			}
		
			var match = HashRegex.Match(trimmed);
			if (match.Success)
			{
				var hash = match.Groups["hash"].Value;
				
				var identityEntry = _data.FirstOrDefault(x => x.From == hash || x.To == hash);
				if (identityEntry is not null)
				{
					var parts = identityEntry.Comment.Split(' ');
					if (parts.Length >= 4)
						sectionIdentity = string.Join(' ', parts[1..^1]);
				}
				
				ushort index = 0;

				while(true)
				{
					var tempHash = _data.FirstOrDefault(x => x.From == hash);
					if (tempHash is null)
						break;
					var tempIndex = uint.Parse(tempHash.Comment.Split(' ')[0]);
			
					if (tempIndex <= index || hash == tempHash.To)
						break;
			
					index = (ushort)tempIndex;
					hash = tempHash.To;
				}
		
				if (index > 0 && hash != match.Groups["hash"].Value)
				{
					changed = true;
					line = $"{match.Groups["front"].Value}hash = {hash}";
					sb.Append($"Found hash to change: \n\t{match.Groups["hash"].Value} -> {hash}\n");
					
					// Cross-field update: Check embedded index data in the final IB entry
					var finalEntry = _data.FirstOrDefault(x => x.From == hash || x.To == hash);
					if (finalEntry is not null)
					{
						UpdateEmbeddedIndexFields(finalEntry, sectionIdentity);
					}
				}
			}
			else if (sectionIdentity is not null)
			{
				var firstIndexMatch = MatchFirstIndexRegex.Match(trimmed);
				if (firstIndexMatch.Success)
				{
					var value = firstIndexMatch.Groups["value"].Value;
					var walked = WalkValueWithEmbedded("object_indexes", sectionIdentity, value);
					if (walked != value)
					{
						changed = true;
						line = $"match_first_index = {walked}";
						sb.Append($"Found object_indexes to change: \n\t{value} -> {walked}\n");
					}
				}
				else
				{
					var indexCountMatch = MatchIndexCountRegex.Match(trimmed);
					if (indexCountMatch.Success)
					{
						var value = indexCountMatch.Groups["value"].Value;
						var walked = WalkValueWithEmbedded("object_index_counts", sectionIdentity, value);
						if (walked != value)
						{
							changed = true;
							line = $"match_index_count = {walked}";
							sb.Append($"Found object_index_counts to change: \n\t{value} -> {walked}\n");
						}
					}
				}
			}
		
			newIniLines[i] = line;
		}
		
		if (!changed)
			return;
		if (Logger.LoggingMode == LogSeverity.Standard)
		{
			sb.Append($"Found ini:\n\t{iniPath}");
		}
		Logger.Log(sb.Append('\n').ToString());
		
		var fileName = Path.GetFileName(iniPath);
		var backIniPath =
			string.Concat(iniPath[..^fileName.Length], "DISABLED_versionfix_", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), "-", fileName);
		File.Move(iniPath, backIniPath);
		
		if (!File.Exists(iniPath))
		{
			File.Create(iniPath).Close();
		}
		File.WriteAllLines(iniPath, newIniLines, Encoding.UTF8);
	}
	private static string WalkValue(string kind, string identity, string startValue)
	{
		if (!uint.TryParse(startValue, out var current))
			return startValue;
		var lastCounter = (uint)0;
		while (true)
		{
			HashChangeData? best = null;
			var bestCounter = uint.MaxValue;
			var bestPosition = -1;
			foreach (var entry in _data)
			{
				var parts = entry.Comment.Split(' ');
				if (parts.Length < 4 || parts[^1] != kind)
					continue;
				if (!uint.TryParse(parts[0], out var counter))
					continue;
				if (counter <= lastCounter)
					continue;
				if (string.Join(' ', parts[1..^1]) != identity)
					continue;
				var fromValues = ParseBracketArray(entry.From);
				if (fromValues is null)
					continue;
				var position = Array.IndexOf(fromValues, current);
				if (position == -1)
					continue;
				if (counter >= bestCounter) continue;
				best = entry;
				bestCounter = counter;
				bestPosition = position;
			}

			if (best is null)
				break;

			var toValues = ParseBracketArray(best.To);
			if (toValues is null || toValues.Length <= bestPosition)
				break;

			current = toValues[bestPosition];
			lastCounter = bestCounter;
		}
		return current.ToString();
	}
	
	private static string WalkValueWithEmbedded(string kind, string identity, string startValue)
	{
		if (!uint.TryParse(startValue, out var current))
			return startValue;
		
		var lastCounter = (uint)0;
		while (true)
		{
			HashChangeData? best = null;
			var bestCounter = uint.MaxValue;
			var bestPosition = -1;
			
			// First, try to find separate entries (old format)
			foreach (var entry in _data)
			{
				var parts = entry.Comment.Split(' ');
				if (parts.Length < 4 || parts[^1] != kind)
					continue;
				if (!uint.TryParse(parts[0], out var counter))
					continue;
				if (counter <= lastCounter)
					continue;
				if (string.Join(' ', parts[1..^1]) != identity)
					continue;
				var fromValues = ParseBracketArray(entry.From);
				if (fromValues is null)
					continue;
				var position = Array.IndexOf(fromValues, current);
				if (position == -1)
					continue;
				if (counter < bestCounter)
				{
					best = entry;
					bestCounter = counter;
					bestPosition = position;
				}
			}
			
			// If no separate entries found, try IB entries with embedded data
			if (best is null)
			{
				foreach (var entry in _data)
				{
					if (!entry.Comment.EndsWith(" ib"))
						continue;
					
					var parts = entry.Comment.Split(' ');
					if (parts.Length < 4)
						continue;
					if (!uint.TryParse(parts[0], out var counter))
						continue;
					if (counter <= lastCounter)
						continue;
					var entryIdentity = string.Join(' ', parts[1..^1]);
					if (entryIdentity != identity)
						continue;
					
					string? embeddedFrom = null;
					string? embeddedTo = null;
					
					if (kind == "object_indexes")
					{
						embeddedFrom = entry.FromIndexes;
						embeddedTo = entry.ToIndexes;
					}
					else if (kind == "object_index_counts")
					{
						embeddedFrom = entry.FromIndexCounts;
						embeddedTo = entry.ToIndexCounts;
					}
					
					if (embeddedFrom is null || embeddedTo is null)
						continue;
					
					var fromValues = ParseBracketArray(embeddedFrom);
					if (fromValues is null)
						continue;
					
					var position = Array.IndexOf(fromValues, current);
					if (position == -1)
						continue;
					
					if (counter < bestCounter)
					{
						best = entry;
						bestCounter = counter;
						bestPosition = position;
					}
				}
			}
			
			if (best is null)
				break;
			
			string? toValuesStr = null;
			if (best.Comment.EndsWith(" ib"))
			{
				if (kind == "object_indexes")
					toValuesStr = best.ToIndexes;
				else if (kind == "object_index_counts")
					toValuesStr = best.ToIndexCounts;
			}
			else
			{
				var parts = best.Comment.Split(' ');
				toValuesStr = kind switch
				{
					"object_indexes" => best.To,
					"object_index_counts" => best.To,
					_ => null
				};
			}
			
			if (toValuesStr is null || ParseBracketArray(toValuesStr) is null || 
			    ParseBracketArray(toValuesStr)?.Length <= bestPosition)
				break;
			
			current = ParseBracketArray(toValuesStr)[bestPosition];
			lastCounter = bestCounter;
		}
		
		return current.ToString();
	}
	
	private static string WalkValueWithLinkedIndex(string kind, string identity, string startValue)
	{
		if (!uint.TryParse(startValue, out var current))
			return startValue;
		
		var lastCounter = (uint)0;
		while (true)
		{
			HashChangeData? best = null;
			var bestCounter = uint.MaxValue;
			var bestPosition = -1;
			
			foreach (var entry in _data)
			{
				// Look for IB hash entries that have embedded index data
				if (!entry.Comment.EndsWith(" ib"))
					continue;
				
				if (!uint.TryParse(entry.Comment.Split(' ')[0], out var counter))
					continue;
				if (counter <= lastCounter)
					continue;
				var entryIdentity = string.Join(' ', entry.Comment.Split(' ')[1..^1]);
				if (entryIdentity != identity)
					continue;
				
				// Check if this IB entry has embedded index data for the requested kind
				string? embeddedFrom = null;
				string? embeddedTo = null;
				
				if (kind == "object_indexes")
				{
					embeddedFrom = entry.FromIndexes;
					embeddedTo = entry.ToIndexes;
				}
				else if (kind == "object_index_counts")
				{
					embeddedFrom = entry.FromIndexCounts;
					embeddedTo = entry.ToIndexCounts;
				}
				
				// Skip if no embedded data for this kind
				if (embeddedFrom is null || embeddedTo is null)
					continue;
				
				var fromValues = ParseBracketArray(embeddedFrom);
				if (fromValues is null)
					continue;
				
				var position = Array.IndexOf(fromValues, current);
				if (position == -1)
					continue;
				
				if (counter < bestCounter)
				{
					best = entry;
					bestCounter = counter;
					bestPosition = position;
				}
			}
			
			if (best is null)
				break;
			
			var toValues = ParseBracketArray(best.ToIndexes ?? best.ToIndexCounts);
			if (toValues is null || toValues.Length <= bestPosition)
				break;
			
			current = toValues[bestPosition];
			lastCounter = bestCounter;
		}
		
		return current.ToString();
	}

	private static uint[]? ParseBracketArray(string value)
	{
		if (value.Length < 2 || value[0] != '[' || value[^1] != ']')
			return null;
		var parts = value[1..^1].Split(',');
		var result = new uint[parts.Length];
		return parts.Where((t, i) => !uint.TryParse(t.Trim(), out result[i])).Any() ? null : result;
	}
	
	private static void UpdateEmbeddedIndexFields(HashChangeData ibEntry, string identity)
	{
		// Update match_first_index based on embedded ToIndexes (this IS the final value after all changes)
		if (!string.IsNullOrEmpty(ibEntry.ToIndexes))
		{
			Logger.Log($"Cross-field update object_indexes to: {ibEntry.ToIndexes}");
		}
		
		// Update match_index_count based on embedded ToIndexCounts (this IS the final value after all changes)
		if (!string.IsNullOrEmpty(ibEntry.ToIndexCounts))
		{
			Logger.Log($"Cross-field update object_index_count to: {ibEntry.ToIndexCounts}");
		}
	}
	
	
}

internal static class Logger
{
	internal static LogSeverity LoggingMode = LogSeverity.Standard;

	public static void Log(string? message = null)
	{
		Log(LogSeverity.Standard, message ?? string.Empty);
	}
	
	public static void Log(LogSeverity severity, string? message = null)
	{
		if (LoggingMode.HasFlag(severity))
		{
			Console.WriteLine(message ?? string.Empty);
		}
	}
}

[Flags]
internal enum LogSeverity
{
	None = 1 << 1,
	Standard = 1 << 2,
	Verbose = None | Standard,
}
#endif

public static partial class Program
{
	extension<T>(T[] source)
	{
		private int IndexOf(T option1, T option2) { return source.AsSpan().IndexOf(option1, option2); }
	}

	extension<T>(Span<T> source)
	{
		private int IndexOf(T option1, T option2)
		{
			var index = source.IndexOf(option1);
			return index == -1 ? source.IndexOf(option2) : index;
		}
	}
}

internal sealed class HashChangeData
{
	public string From { get; set; } = null!;
	public string To { get; set; } = null!;
	public string Comment { get; set; } = null!;

	// Embedded index data for cross-field updates (only present in IB hash entries)
	public string? FromIndexes { get; set; }
	public string? ToIndexes { get; set; }
	public string? FromIndexCounts { get; set; }
	public string? ToIndexCounts { get; set; }
}

[JsonSerializable(typeof(HashChangeData))]
[JsonSerializable(typeof(List<HashChangeData>))]
[JsonSerializable(typeof(HashChangeData[]))]
[JsonSourceGenerationOptions(
	WriteIndented = true,
	IndentCharacter = '\t',
	IndentSize = 1,
	IncludeFields = true,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
internal partial class FixerDataCotext : JsonSerializerContext;
