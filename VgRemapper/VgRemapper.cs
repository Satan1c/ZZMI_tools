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
				"3d7e53cf",
				i => i switch
				{
					18 => 20,
					19 => 18,
					20 => 19,
					
					54 => 62,
					55 => 54,
					56 => 55,
					57 => 56,
					58 => 57,
					59 => 58,
					60 => 59,
					61 => 60,
					62 => 61,
					
					69 => 71,
					70 => 72,
					71 => 70,
					72 => 69,
					
					91 => 98,
					92 => 91,
					93 => 92,
					94 => 93,
					95 => 94,
					96 => 95,
					97 => 96,
					98 => 97,
					
					113 => 114,
					114 => 113,
					
					128 => 129,
					129 => 128,
					130 => 132,
					131 => 130,
					132 => 131,
					
					188 => 189,
					189 => 188,
					_ => i
				}
			}
		}.ToFrozenDictionary();

	private static readonly FrozenDictionary<string, string> s_positionToBlend =
		new Dictionary<string, string>
		{
			{ "ff36809b", "3d7e53cf" }
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
