//#define GENERATOR
//#define GH_GRABBER
//#define LOCAL_GRABBER

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

if (args[0] is "-h" or "--help")
{
	Console.WriteLine("Options:");
	Console.WriteLine("  -l, --logging [v|s|n]   Set logging mode: v - verbose, s - standard, n - none");
	Console.WriteLine("  -p, --path \"[path]\"   Set the path to the Mods folder");
	Console.WriteLine("Commands:");
	Console.WriteLine("  fix                      Run the fixer (default)");
	Console.WriteLine("  undo                     Revert applied fix");
	return;
}

if (args.Any(x => x is "-l" or "--logging"))
{
	var index = args.IndexOf("-l", "--logging");
	if (index is not -1)
	{
		var mode = args[index + 1][0];
		if (mode is 'v' or 's' or 'n')
			Logger.loggingMode = mode switch
			{
				'v' => LogSeverity.Verbose,
				's' => LogSeverity.Standard,
				_ => LogSeverity.None
			};
	}
}

string hashesPath = null;
if (args.Any(x => x is "-p" or "--path"))
{
	var index = args.IndexOf("-p", "--path");
	if (index is not -1)
	{
		hashesPath = args[index];
	}
}
else
{
	Console.Write("Enter the path to Mods folder\nwhere mods and PlayerCharacterData.json is located\n(leave empty for current directory): ");
	hashesPath = Console.ReadLine();
}
if (string.IsNullOrEmpty(hashesPath))
	hashesPath = Directory.GetCurrentDirectory();

Directory.SetCurrentDirectory(hashesPath);

string action;
if (args.Any(x => x is "fix" or "undo"))
{
	var index = args.IndexOf("fix", "undo");
	if (index is -1)
	{
		Console.WriteLine("No command was specified, exiting.");
		return;
	}
	action = args[index];
}
else
{
	Console.Write("\nChose action:\nfix - to run fixer\nundo - to revert applied fix\n(leave empty for fix): ");
	action = Console.ReadLine();
}
if (action == "undo")
{
	Logger.Log(LogSeverity.Verbose, "Undoing changes...");
	foreach (var path in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "DISABLED_versionfix_*.ini", SearchOption.AllDirectories))
	{
		var match = FilenameRegex.Match(Path.GetFileName(path));
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
	return;
}

Logger.Log(LogSeverity.Verbose, "base path");
Logger.Log(LogSeverity.Verbose, hashesPath);
Logger.Log(LogSeverity.Verbose);

#if GENERATOR
Generator.SaveTo = Path.Combine(hashesPath, "PlayerCharacterData.json");
#if GH_GRABBER
Generator.Run(await GithubGrabber.Run());
#elif LOCAL_GRABBER
Generator.Run(await GithubGrabber.Run());
#endif
ReadData(Generator.SaveTo);
#else
ReadData(Path.Combine(hashesPath, "PlayerCharacterData.json"));
#endif

foreach (var path in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ini", SearchOption.AllDirectories))
{
	if (Path.GetFileName(path).StartsWith("disabled", StringComparison.InvariantCultureIgnoreCase))
		continue;
	if (Path.GetDirectoryName(path)?.Replace('\\', '/').Split('/').Any(x => x.StartsWith("disabled", StringComparison.InvariantCultureIgnoreCase)) ?? false)
		continue;
	
	Logger.Log(LogSeverity.Verbose, "Found ini:");
	Logger.Log(LogSeverity.Verbose, path);
	Run(path);
	Logger.Log(LogSeverity.Verbose);
}

Logger.Log("Done");
Console.ReadKey();

internal sealed class HashChangeData
{
	public string From { get; set; } = null!;
	public string To { get; set; } = null!;
	public string Comment { get; set; } = null!;
}

[JsonSerializable(typeof(HashChangeData))]
[JsonSerializable(typeof(List<HashChangeData>))]
[JsonSerializable(typeof(HashChangeData[]))]
[JsonSourceGenerationOptions(WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class FixerDataCotext : JsonSerializerContext;

public static partial class Program
{
	[GeneratedRegex(@"^hash ?= ?(?<hash>\w{8})$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex GetHashRegex();
	private static readonly Regex HashRegex = GetHashRegex();
	
	[GeneratedRegex(@"DISABLED_versionfix_\d*-(?<name>.+\.ini)", RegexOptions.Compiled)]
	private static partial Regex GetFilenameRegex();
	private static readonly Regex FilenameRegex = GetFilenameRegex();

	private static HashChangeData[] _data = null!;

	private static void ReadData(string jsonPath)
	{
		_data = JsonSerializer.Deserialize(File.ReadAllText(jsonPath), FixerDataCotext.Default.HashChangeDataArray)!;
	}
	
	private static void Run(string iniPath)
	{
		var iniLines = File.ReadLines(iniPath, Encoding.UTF8).ToArray();
		var newIniLines = new string[iniLines.Length];
		
		var changed = false;
		for (var i = 0; i < iniLines.Length; i++)
		{
			var line = iniLines[i];
			var match = HashRegex.Match(line.TrimStart());
			if (match.Success)
			{
				var hash = match.Groups["hash"].Value;
				ushort index = 0;

				while(true)
				{
					var tempHash = _data.FirstOrDefault(x => x.From == hash);
					if (tempHash is null)
						break;
					var tempIndex = ushort.Parse(tempHash.Comment.Split(' ')[0]);
			
					if (tempIndex <= index || hash == tempHash.To)
						break;
			
					index = tempIndex;
					hash = tempHash.To;
				}
		
				if (index > 0 && hash != match.Groups["hash"].Value)
				{
					changed = true;
					line = $"{match.Groups["front"].Value}hash = {hash}";
					Logger.Log("Found hash to change: ");
					Logger.Log($"{match.Groups["hash"].Value} -> {hash}");
				}
			}
	
			newIniLines[i] = line;
		}
		
		if (!changed)
			return;
		
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
	
	private static int IndexOf<T>(this T[] source, T option1, T option2)
	{
		return source.AsSpan().IndexOf(option1, option2);
	}
	private static int IndexOf<T>(this Span<T> source, T option1, T option2)
	{
		var index = source.IndexOf(option1);
		index = index is -1 ? source.IndexOf(option2) : index;
		return index;
	}
}

internal static class Logger
{
	internal static LogSeverity loggingMode = LogSeverity.Verbose;

	public static void Log(string message = null)
	{
		Log(LogSeverity.Standard, message ?? string.Empty);
	}
	
	public static void Log(LogSeverity severity, string message = null)
	{
		if (loggingMode.HasFlag(severity))
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
