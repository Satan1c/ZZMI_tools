using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

var current = Directory.GetCurrentDirectory();
var total = Stopwatch.GetTimestamp();
var inis = Directory
	.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ini", SearchOption.AllDirectories)
	.Select(x => new FileInfo(x))
	.Where(x => !x.Name.StartsWith("DISABLED", StringComparison.InvariantCultureIgnoreCase))
	.ToArray();

if (inis.Length < 1)
{
	Console.WriteLine("No ini files found.");
	goto end;
}

var linesTasks = new Task<(string lines, string[] separateLines)>[inis.Length];
for (var i = 0; i < inis.Length; i++) linesTasks[i] = GetIniLines(inis[i]);
await Task.WhenAll(linesTasks);

var lines = linesTasks
	.Select(t => t.Result)
	.ToArray();

total = Stopwatch.GetTimestamp();

var resourceNames = new ReadOnlyDictionary<string, string[]>[lines.Length];
for (var i = 0; i < lines.Length; i++) resourceNames[i] = GetResourceNames(lines[i].separateLines);

if (resourceNames.Length < 1)
{
	Console.WriteLine("No resource names found.");
	goto end;
}

var resourceFiles = new ReadOnlyDictionary<string, FileInfo[]>[resourceNames.Length];
for (var i = 0; i < resourceNames.Length; i++)
{
	Directory.SetCurrentDirectory(inis[i].DirectoryName ?? current);
	resourceFiles[i] = GetResourceFiles(resourceNames[i], lines[i].lines);
}

Directory.SetCurrentDirectory(current);

if (!resourceFiles.Any(x => x.Values?.Any(y => y.Length > 0) ?? false))
{
	Console.WriteLine("No resource files found.");
	goto end;
}

var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
foreach (var (hash, files) in resourceFiles.SelectMany(x => x.Select(y => (y.Key, y.Value))))
{
	foreach (var file in files)
	{
		Remap(hash, file, timestamp);
	}
}

end:
Console.WriteLine($"Total {Stopwatch.GetElapsedTime(total)}");
GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
Console.ReadKey();

public static partial class Program
{
	private const byte Stride = 32;

	private static readonly FrozenDictionary<string, Func<uint, uint>> s_blendMappings =
		new Dictionary<string, Func<uint, uint>>()
		{
			{
				"4f3ddd5c",
				i => i switch
				{
					9 => 10,
					10 => 9,
					_ => i
				}
			},
			{
				"0139f7e8",
				i => i switch
				{
					5 => 9,
					6 => 5,
					9 => 10,
					10 => 11,
					11 => 12,
					12 => 13,
					13 => 14,
					14 => 15,
					15 => 6,
					_ => i
				}
			}
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, string> s_positionToBlend =
		new Dictionary<string, string>
		{
			{ "537d9b9b", "4f3ddd5c" },
			{ "324d9d21", "0139f7e8" }
		}.ToFrozenDictionary();

	[GeneratedRegex(@"^[ \t]*hash\s*=\s*(?<Hash>\w{8})", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex();
	private static readonly Regex s_hashRegex = MyRegex();

	[GeneratedRegex(@"^[ \t]*vb2\s*=\s*(?:Resource(?<Name>.*))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex1();
	private static readonly Regex s_blendResourceNameRegex = MyRegex1();

	[GeneratedRegex(@"^[ \t]*\[Resource(?<Name>.*)\]$\s^[ \t]*type\s*=\s*Buffer$\s^[ \t]*stride\s*=\s*32$\s^[ \t]*filename\s*=\s*(?<File>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex2();
	private static readonly Regex s_blendResourceRegex = MyRegex2();

	private static async Task<(string lines, string[] separateLines)> GetIniLines(FileInfo file)
	{
		await using var readStream = file.OpenRead();
		using var reader = new StreamReader(readStream, leaveOpen: false);
		
		var lines = await reader.ReadToEndAsync();
		var separatedLines = lines
			.Replace("\r\n", "\n")
			.Split('\n')
			.AsParallel()
			.Select(x => x.Trim())
			.Where(x => !string.IsNullOrEmpty(x) && !x.StartsWith(';'))
			.ToArray();
		lines = string.Join('\n', separatedLines);
		return (lines, separatedLines);
	}

	private static ReadOnlyDictionary<string, string[]> GetResourceNames(string[] lines)
	{
		var resourceNamesMap = new Dictionary<string, string[]>();

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (line.AsSpan().StartsWith('[')) continue;
			
			var hashMatch = s_hashRegex.Match(line);
			if (!hashMatch.Success) continue;

			var hash = hashMatch.Groups["Hash"].Value;
			if (!s_blendMappings.ContainsKey(hash) && !s_positionToBlend.TryGetValue(hash, out hash)) continue;
			
			var resourceNames = new LinkedList<string>();
			var j = i;
			for (; j < lines.Length; j++, i++)
			{
				var nextLine = lines[j];
				if (nextLine.AsSpan().StartsWith('[')) break;
				
				var bledResourceNameMatch = s_blendResourceNameRegex.Match(nextLine);
				if (!bledResourceNameMatch.Success) continue;
				
				resourceNames.AddLast(bledResourceNameMatch.Groups["Name"].Value);
				
				break;
			}

			resourceNamesMap[hash] = resourceNames.ToArray();
		}

		return resourceNamesMap.AsReadOnly();
	}

	private static ReadOnlyDictionary<string, FileInfo[]> GetResourceFiles(
		IReadOnlyDictionary<string, string[]> namesMap,
		string lines)
	{
		var resourceFilesMap = new Dictionary<string, FileInfo[]>();
		foreach (var (hash, names) in namesMap)
		{
			var resourceFiles = new LinkedList<FileInfo>();
			foreach (Match match in s_blendResourceRegex.Matches(lines))
			{
				if (!names.Contains(match.Groups["Name"].Value)) continue;
				var path = Path.Combine(Directory.GetCurrentDirectory(), match.Groups["File"].Value);
				resourceFiles.AddLast(new FileInfo(path));
			}

			resourceFilesMap[hash] = resourceFiles.Where(x => x.Exists).ToArray();
		}
		
		return resourceFilesMap.AsReadOnly();
	}

	private static byte[] bytes = [];
	private static void Remap(string hash, FileInfo file, long timestamp)
	{
		bytes = File.ReadAllBytes(file.FullName);
		var data = Enumerable.Range(0, (int)(file.Length / Stride)).AsParallel().SelectMany(x =>
		{
			var group = x * Stride;
			var weights = Enumerable.Range(0, 4).SelectMany(y =>
			{
				var i = group + y * 4;
				var w = BitConverter.ToSingle(bytes.AsSpan()[new Range(i, i + 4)]);
				return BitConverter.GetBytes(w);
			});
			var indices = Enumerable.Range(0, 4).SelectMany(y =>
			{
				var i = group + y * 4 + 16;
				var index = BitConverter.ToUInt32(bytes.AsSpan()[new Range(i, i + 4)]);
				index = s_blendMappings[hash](index);
				return BitConverter.GetBytes(index);
			});
			return weights.Concat(indices).ToArray();
		}).ToArray().AsSpan();

		var name = file.Name;
		var path = file.FullName;
		var directory = file.Directory!.FullName;
		file.CopyTo(Path.Combine(directory, $"remap_backup_{name}_{timestamp}"));
		File.WriteAllBytes(path, data);
		bytes = [];
	}
}
