using System.Text.RegularExpressions;

namespace SlotsFixApplier;

public partial class Slotted : IAppliers
{
	public void Execute(string modPath, string resultPath)
	{
		var slotsRegex = SlotsRegex();

		using var _iniFile = File.OpenRead(modPath);
		using var _reader = new StreamReader(_iniFile);

		using var resultFile = File.Open(resultPath, FileMode.Create);
		using var writer = new StreamWriter(resultFile);


		foreach (var lines in ReadSlots(_reader)) writer.WriteLine(lines);

		return;

		IEnumerable<string> ReadSlots(StreamReader reader)
		{
			byte c = 0;

			do
			{
				var line = reader.ReadLine() ?? string.Empty;
				var lineTrimmed = line.Trim();

				if (lineTrimmed.StartsWith('[') || lineTrimmed.StartsWith(';') || !lineTrimmed.StartsWith("ps-t"))
				{
					yield return line;
					continue;
				}

				if (!slotsRegex.IsMatch(lineTrimmed)) continue;

				yield return slotsRegex.Replace(line, match =>
				{
					return match.Groups["beginning"].Value + @"Resource\ZZMI\" + match.Groups["slot"].Value switch
					{
						"ps-t3" => "D",
						"ps-t4" => "N",
						"ps-t5" => "L",
						"ps-t6" => "M",
						_ => string.Empty
					} + " = " + match.Groups["resource"].Value;
				});

				c++;

				if (c != 4 && (!lineTrimmed.StartsWith("ps-t4") || c >= 2)) continue;

				yield return @"run = CommandList\ZZMI\SetTex";
				c = 0;
			} while (!reader.EndOfStream);
		}
	}

	[GeneratedRegex("(?<beginning>.*)(?<slot>ps-t[3-6]) = (?<resource>.*)",
		RegexOptions.Compiled | RegexOptions.Singleline)]
	private static partial Regex SlotsRegex();
}