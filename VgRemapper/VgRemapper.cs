using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

var current = Directory.GetCurrentDirectory();
var total = Stopwatch.GetTimestamp();
var inis = Directory
	.EnumerateFiles(current, "*.ini", SearchOption.AllDirectories)
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

var resourceNames = new Dictionary<string, string[]>[lines.Length];
for (var i = 0; i < lines.Length; i++) resourceNames[i] = GetResourceNames(lines[i].separateLines);

if (!resourceNames.Any(x => x.Values.Any(y => y.Length > 0)))
{
	Console.WriteLine("No resource names found.");
	goto end;
}

var resourceFiles = new Dictionary<string, FileInfo[]>[resourceNames.Length];
for (var i = 0; i < resourceNames.Length; i++)
{
	Directory.SetCurrentDirectory(inis[i].DirectoryName ?? current);
	resourceFiles[i] = GetResourceFiles(resourceNames[i], lines[i].lines);
}

Directory.SetCurrentDirectory(current);

if (!resourceFiles.Any(x => x.Values.Any(y => y.Length > 0)))
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
				"e42171df",
				i => i switch
				{
					26 => 4,
					27 => 5,
					28 => 6,
					29 => 7,
					30 => 8,
					31 => 9,
					32 => 10,
					33 => 11,
					34 => 12,
					35 => 13,
					36 => 14,
					37 => 15,
					38 => 16,
					39 => 17,
					40 => 19,
					41 => 20,
					42 => 18,
					43 => 22,
					44 => 23,
					45 => 24,
					46 => 25,
					47 => 26,
					48 => 27,
					49 => 28,
					50 => 21,
					51 => 29,
					52 => 30,
					53 => 31,

					90 => 33,
					91 => 34,
					92 => 32,
					93 => 36,
					94 => 37,
					95 => 35,
					96 => 38,
					97 => 39,
					98 => 40,
					99 => 41,
					100 => 42,
					101 => 44,
					102 => 45,
					103 => 46,
					104 => 48,
					105 => 47,
					106 => 43,
					107 => 49,
					108 => 52,
					109 => 53,
					110 => 54,
					111 => 55,
					112 => 50,
					113 => 51,
					114 => 56,
					115 => 57,
					116 => 58,
					117 => 59,
					118 => 60,
					119 => 61,
					120 => 62,
					121 => 63,
					122 => 64,
					123 => 65,
					124 => 66,
					125 => 67,
					126 => 68,
					_ => i
				}
			},
			{
				"d06a9206",
				i => i switch
				{
					4 => 0,
					5 => 1,
					6 => 2,
					7 => 3,
					8 => 4,
					9 => 5,
					10 => 6,
					11 => 7,
					12 => 8,
					13 => 9,
					14 => 10,
					15 => 11,
					16 => 12,
					17 => 13,
					18 => 14,
					19 => 15,
					20 => 16,
					21 => 17,
					22 => 18,
					23 => 19,
					24 => 20,
					25 => 21,
					
					54 => 22,
					55 => 23,
					56 => 24,
					57 => 25,
					58 => 26,
					59 => 27,
					60 => 28,
					61 => 29,
					62 => 30,
					63 => 31,
					64 => 32,
					65 => 33,
					66 => 34,
					67 => 35,
					68 => 36,
					69 => 37,
					70 => 38,
					71 => 39,
					72 => 40,
					73 => 41,
					74 => 42,
					75 => 43,
					76 => 44,
					77 => 45,
					78 => 46,
					79 => 47,
					80 => 48,
					81 => 49,
					82 => 50,
					83 => 51,
					84 => 52,
					85 => 53,
					86 => 54,
					87 => 55,
					88 => 56,
					89 => 57,
					
					127 => 58,
					128 => 59,
					129 => 60,
					_ => i
				}
			}
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, string> s_positionToBlend =
		new Dictionary<string, string>
		{
			{ "33a09cfe", "e42171df" },
			{ "82e7c056", "d06a9206" }
		}.ToFrozenDictionary();

	[GeneratedRegex(@"^hash ?= ?(?<Hash>\w{8})$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex();
	private static readonly Regex s_hashRegex = MyRegex();

	[GeneratedRegex(@"^vb2 ?= ?(?:Resource(?<Name>.+))$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex1();
	private static readonly Regex s_blendResourceNameRegex = MyRegex1();

	[GeneratedRegex(@"^\[Resource(?<Name>.+)\]$\s^type ?= ?Buffer$\s^stride ?= ?32$\s^filename ?= ?(?<File>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
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

	private static Dictionary<string, string[]> GetResourceNames(string[] lines)
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
			
			var resourceNames = new List<string>(1);
			var j = i;
			for (; j < lines.Length; j++, i++)
			{
				var nextLine = lines[j];
				if (nextLine.AsSpan().StartsWith('[')) break;
				
				var bledResourceNameMatch = s_blendResourceNameRegex.Match(nextLine);
				if (!bledResourceNameMatch.Success) continue;
				
				resourceNames.Add(bledResourceNameMatch.Groups["Name"].Value);
				
				break;
			}
			
			if (resourceNames.Count > 0)
				resourceNamesMap[hash] = CollectionsMarshal.AsSpan(resourceNames).ToArray();
		}

		return resourceNamesMap;
	}

	private static Dictionary<string, FileInfo[]> GetResourceFiles(Dictionary<string, string[]> namesMap, string lines)
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
		
		return resourceFilesMap;
	}

	private static byte[] _bytes = [];
	private static void Remap(string hash, FileInfo file, long timestamp)
	{
		_bytes = File.ReadAllBytes(file.FullName);
		var data = Enumerable.Range(0, (int)(file.Length / Stride)).AsParallel().SelectMany(x =>
		{
			var group = x * Stride;
			var weights = Enumerable.Range(0, 4).SelectMany(y =>
			{
				var i = group + y * 4;
				var w = BitConverter.ToSingle(_bytes.AsSpan()[i..(i+4)]);
				return BitConverter.GetBytes(w);
			});
			var indices = Enumerable.Range(0, 4).SelectMany(y =>
			{
				var i = group + y * 4 + 16;
				var index = BitConverter.ToUInt32(_bytes.AsSpan()[i..(i+4)]);
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
		_bytes = [];
	}
}
