//#define GENERATOR
//#define GH_GRABBER
//#define LOCAL_GRABBER

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var hashesPath = Console.ReadLine();
if (string.IsNullOrEmpty(hashesPath))
	hashesPath = Directory.GetCurrentDirectory();

Directory.SetCurrentDirectory(hashesPath);

Console.WriteLine("base path");
Console.WriteLine(hashesPath);
Console.WriteLine();

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
	Console.WriteLine("Found ini:");
	Console.WriteLine(path);
	Run(path);
	Console.WriteLine();
}

Console.WriteLine("Done");
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
	[GeneratedRegex(@"^hash ?= ?(?<hash>\w{8})$", RegexOptions.Compiled)]
	private static partial Regex GetHashRegex();
	private static readonly Regex HashRegex = GetHashRegex();

	private static HashChangeData[] _data;

	private static void ReadData(string jsonPath)
	{
		_data = JsonSerializer.Deserialize<HashChangeData[]>(jsonPath, FixerDataCotext.Default.HashChangeDataArray)!;
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
		
				if (index > 0)
				{
					changed = true;
					line = $"{match.Groups["front"].Value}hash = {hash}";
					Console.Write("Found hash to change: ");
					Console.WriteLine($"{match.Groups["hash"].Value} -> {hash}");
				}
			}
	
			newIniLines[i] = line;
		}
		
		if (!changed)
			return;
		
		var fileName = Path.GetFileName(iniPath);
		var backIniPath =
			string.Concat(iniPath[..^fileName.Length], "DISABLED_", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(), "_", fileName);
		File.Move(iniPath, backIniPath);
		
		if (!File.Exists(iniPath))
		{
			File.Create(iniPath).Close();
		}
		File.WriteAllLines(iniPath, newIniLines, Encoding.UTF8);
	}
}
