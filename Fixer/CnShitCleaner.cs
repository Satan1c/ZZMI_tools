using System.Text;
using System.Text.RegularExpressions;

namespace Fixer;

public static class CnShitCleaner
{
	private static readonly Regex s_checksRegex = new(
		"checktextureoverride = ps-t[3-6]",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
	);

	private static readonly Regex s_backRegex = new(
		"Resource.Bak",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
	);

	private static readonly Regex s_texturesResourceRegex = new(
		@"ps-t[3-6] = (Resource_[\w\d-]+)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
	);

	private static readonly Regex s_texturesRegex = new(
		@"ps-t([3-6]) = Resource_([\w\d]{8})-?([\w\d]{8})?-([\d])-([\w]+)",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline
	);

	public static void Process()
	{
		//const string basePath = @"C:\Users\Satan1c\OneDrive\Desktop\Máscara versión fluorescente";
		var ogInis = Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.ini", SearchOption.AllDirectories)
			.Where(
				x =>
				{
					var name = x.ToLower().Split('\\')[^1];
					return !name.StartsWith("disabled") && !name.StartsWith("clean");
				})
			.ToArray();

		foreach (var og in ogInis) CleanCnShit(og);
	}

	public static void CleanCnShit(string ogIni)
	{
		var dir = string.Join('\\', ogIni.Split('\\')[..^1]);
		var ogInfo = new FileInfo(ogIni);

		using var ogFile = File.Open(ogIni, FileMode.Open, FileAccess.Read);
		using var ogReader = new StreamReader(ogFile, Encoding.UTF8, leaveOpen: false);

		var lines = ogReader.ReadToEnd().Split("\r\n");
		ogFile.Close();
		ogReader.Close();

		using var cleanFile = File.Open(Path.Combine(dir, "cleaned_" + ogInfo.Name), FileMode.Create, FileAccess.Write);
		using var cleanWriter = new StreamWriter(cleanFile, Encoding.UTF8, leaveOpen: false);

		byte slotsCount = 0;
		var texturesLines = new LinkedList<(string name, string ib, string hash, string resource)>();

		foreach (var line in lines)
		{
			if (s_backRegex.IsMatch(line)) continue;

			if (s_checksRegex.IsMatch(line))
			{
				if (slotsCount == 0) cleanWriter.WriteLine("run = CommandListsSkinTexture");

				slotsCount++;
				if (slotsCount == 4) slotsCount = 0;

				continue;
			}

			var texturesResourceMatch = s_texturesResourceRegex.Match(line);

			if (texturesResourceMatch.Success)
			{
				var textureMatch = s_texturesRegex.Match(line);

				var slot = textureMatch.Groups[1].Value;
				var ib = textureMatch.Groups[2].Value;
				var hash = textureMatch.Groups[3].Value;
				var number = textureMatch.Groups[4].Value;
				var name = textureMatch.Groups[5].Value;

				if (string.IsNullOrEmpty(hash))
				{
					cleanWriter.WriteLine(line);
					continue;
				}

				if (slot == "4")
				{
					cleanWriter.WriteLine(line.Split('=')[0] +
					                      $"= Resource_{ib}{(hash.Length > 0 ? '-' + hash : "")}-1-{name}");
					continue;
				}

				if (number == "1")
				{
					name = $"{ib} {name}";

					texturesLines.AddLast((name, ib, hash, texturesResourceMatch.Groups[1].Value));
				}

				continue;
			}

			if (texturesLines.Count > 0 && (line == "\n" || line.StartsWith('[') || line.StartsWith(';')))
			{
				foreach (var texture in texturesLines)
				{
					if (string.IsNullOrEmpty(texture.hash)) continue;

					cleanWriter.WriteLine($"[TextureOverride {texture.name}]");
					cleanWriter.WriteLine($"hash = {texture.hash}");
					cleanWriter.WriteLine($"this = {texture.resource}");
					cleanWriter.WriteLine("");
				}

				texturesLines.Clear();
			}

			cleanWriter.WriteLine(line);
		}

		File.Move(ogIni, Path.Combine(dir, $"DISABLED_{ogInfo.Name}"));
	}
}