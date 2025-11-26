//#define GENERATOR
#define GH_GRABBER
//#define LOCAL_GRABBER

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VersionFixerGenerator;

var hashesPath = Console.ReadLine();
if (string.IsNullOrEmpty(hashesPath))
	hashesPath = Directory.GetCurrentDirectory();

hashesPath = @"C:\Users\Satan1c\OneDrive\Documents\Development\projects\ZZMI_tools\VersionFixerGenerator\PlayerCharacterData";

if (!Directory.Exists(hashesPath))
	Directory.CreateDirectory(hashesPath);

Directory.SetCurrentDirectory(hashesPath);

Generator.SaveTo = Path.Combine(hashesPath, "PlayerCharacterData.json");

#if GENERATOR
#if GH_GRABBER
Generator.Run(await GithubGrabber.Run());
#elif LOCAL_GRABBER
Generator.Run(await GithubGrabber.Run());
#endif
#endif

var data = JsonSerializer.Deserialize<HashChangeData[]>(File.ReadAllText(Generator.SaveTo), FixerDataCotext.Default.HashChangeDataArray)!;

var iniPath = @"yanagi_yuying.ini";
var backIniPath =
	string.Concat("DISABLED_", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().AsSpan(), "_", iniPath);
File.Move(iniPath, backIniPath);

using var iniFile = File.Open(iniPath, FileMode.Create, FileAccess.Write);
using var iniWriter = new StreamWriter(iniFile, leaveOpen: false);
using var backIniFile = File.Open(backIniPath, FileMode.Open, FileAccess.Read);
using var iniReader = new StreamReader(backIniFile, leaveOpen: false);

while (!iniReader.EndOfStream)
{
	var line = iniReader.ReadLine() ?? string.Empty;
	
	var match = HashRegex.Match(line.TrimStart());
	if (match.Success)
	{
		var hash = match.Groups["hash"].Value;
		ushort index = 0;
		
		while(true)
		{
			var tempHash = data.FirstOrDefault(x => x.From == hash);
			if (tempHash == null)
				break;
			var tempIndex = ushort.Parse(tempHash.Comment.Split(' ')[0]);
			
			if (tempIndex <= index || hash == tempHash.To)
				break;
			
			index = tempIndex;
			hash = tempHash.To;
		}
		
		if (index > 0)
			line = $"{match.Groups["front"].Value}hash = {hash}";
	}
	
	iniWriter.WriteLine(line);
}

Console.WriteLine("Done");

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
	public static partial Regex GetHashRegex();
	public static Regex HashRegex = GetHashRegex();
}
