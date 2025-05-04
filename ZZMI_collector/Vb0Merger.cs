using System.Text;
using System.Text.RegularExpressions;

namespace ZZMI_collector;

public static partial class Vb0Merger
{
	[GeneratedRegex(
		"stride: ([0-9]+)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant
	)]
	private static partial Regex StrideRegex();

	[GeneratedRegex(
		"Format: (.+)",
		RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant
	)]
	private static partial Regex FormatRegex();

	[GeneratedRegex(
		"vb([0-9])",
		RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant
	)]
	private static partial Regex VbRegex();

	[GeneratedRegex(
		@"\+([0-9]{3})",
		RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant
	)]
	private static partial Regex OffsetRegex();

	public static void Merge(
		string target,
		string from,
		string to,
		string position,
		string texcoord,
		string blend
	)
	{
		var filePath = to + $"\\{target}-vb0={position}.txt";
		if (!File.Exists(filePath)) File.Create(filePath).Close();

		using var file = File.OpenWrite(filePath);

		using var vb0 = File.ReadLines(Directory.EnumerateFiles(from, $"*{position}*.txt").First()).GetEnumerator();
		using var vb2 = File.ReadLines(Directory.EnumerateFiles(from, $"*{blend}*.txt").First()).GetEnumerator();
		var vb1 = File.ReadLines(Directory.EnumerateFiles(from, $"*{texcoord}*.txt").First()).GetEnumerator();

		vb0.MoveNext();
		vb1.MoveNext();
		vb2.MoveNext();

		var stride = byte.Parse(StrideRegex().Match(vb0.Current).Groups[1].Value);
		stride += byte.Parse(StrideRegex().Match(vb2.Current).Groups[1].Value);
		stride += byte.Parse(StrideRegex().Match(vb1.Current).Groups[1].Value);

		byte elementsOffset = 0;
		var byteOffsets = new Dictionary<byte, byte>();

		file.Write(Encoding.UTF8.GetBytes($"stride: {stride}\n").AsSpan());

		for (byte i = 0; i < 2; i++)
		{
			vb0.MoveNext();
			vb1.MoveNext();

			file.Write(Encoding.UTF8.GetBytes(vb0.Current + '\n').AsSpan());
		}

		vb0.MoveNext();

		for (byte i = 0; i < 3; i++) vb1.MoveNext();

		file.Write("topology: trianglelist\n"u8.ToArray().AsSpan());

		for (byte i = 0; i < 21; i++)
		{
			vb0.MoveNext();
			vb1.MoveNext();

			if (vb0.Current.StartsWith("  Format"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb0.Current + '\n').AsSpan());

				var offset = GetOffsetFor(vb0.Current);
				byteOffsets[(byte)byteOffsets.Count] = offset;

				vb0.MoveNext();
				vb1.MoveNext();
				file.Write("  InputSlot: 0\n"u8.ToArray().AsSpan());

				continue;
			}

			if (vb0.Current.StartsWith("  AlignedByteOffset"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb0.Current.Split(": ")[0] + $": {elementsOffset}\n").AsSpan());
				elementsOffset += byteOffsets[(byte)(byteOffsets.Count - 1)];

				continue;
			}

			file.Write(Encoding.UTF8.GetBytes(vb0.Current + '\n').AsSpan());
		}

		while (vb2.Current != "  SemanticName: BLENDWEIGHTS" && vb2.MoveNext())
		{
		}

		file.Write("element[3]:\n"u8.ToArray().AsSpan());
		file.Write(Encoding.UTF8.GetBytes(vb2.Current + '\n').AsSpan());

		for (byte i = 0; i < 5; i++)
		{
			vb2.MoveNext();

			if (vb2.Current.StartsWith("  Format"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb2.Current + '\n').AsSpan());

				var offset = GetOffsetFor(vb2.Current);
				byteOffsets[(byte)byteOffsets.Count] = offset;

				vb2.MoveNext();
				file.Write("  InputSlot: 0\n"u8.ToArray().AsSpan());

				continue;
			}

			if (vb2.Current.StartsWith("  AlignedByteOffset"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb2.Current.Split(": ")[0] + $": {elementsOffset}\n").AsSpan());
				elementsOffset += byteOffsets[(byte)(byteOffsets.Count - 1)];
				continue;
			}

			file.Write(Encoding.UTF8.GetBytes(vb2.Current + '\n').AsSpan());
		}

		vb2.MoveNext();
		file.Write("element[4]:\n"u8.ToArray().AsSpan());

		for (byte i = 0; i < 6; i++)
		{
			vb2.MoveNext();

			if (vb2.Current.StartsWith("  Format"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb2.Current + '\n').AsSpan());

				var offset = GetOffsetFor(vb2.Current);
				byteOffsets[(byte)byteOffsets.Count] = offset;

				vb2.MoveNext();
				file.Write("  InputSlot: 0\n"u8.ToArray().AsSpan());

				continue;
			}

			if (vb2.Current.StartsWith("  AlignedByteOffset"))
			{
				file.Write(Encoding.UTF8.GetBytes(vb2.Current.Split(": ")[0] + $": {elementsOffset}\n").AsSpan());
				elementsOffset += byteOffsets[(byte)(byteOffsets.Count - 1)];
				continue;
			}

			file.Write(Encoding.UTF8.GetBytes(vb2.Current + '\n').AsSpan());
		}

		byte vb1Counter = 0;

		while (vb1.Current.Trim() != "vertex-data:" && vb1.MoveNext())
		{
		}

		vb1.MoveNext();
		vb1.MoveNext();

		while (vb1.Current.Trim() != "" && vb1.MoveNext()) vb1Counter++;

		vb1.Dispose();
		vb1 = File.ReadLines(Directory.EnumerateFiles(from, $"*{texcoord}*.txt").First()).GetEnumerator();
		vb1.MoveNext();

		while (vb1.Current.Trim() != "element[2]:" && vb1.MoveNext())
		{
		}

		for (byte i = 0; i < 7; i++) vb1.MoveNext();

		vb1.MoveNext();

		byte vb1ElementIndex = 5;

		for (byte i = 0; i < vb1Counter; i++)
		{
			file.Write(Encoding.UTF8.GetBytes($"element[{vb1ElementIndex}]:\n").AsSpan());

			vb1ElementIndex++;

			for (byte j = 0; j < 6; j++)
			{
				vb1.MoveNext();

				if (vb1.Current.Trim().StartsWith("Format"))
				{
					file.Write(Encoding.UTF8.GetBytes(vb1.Current + '\n').AsSpan());

					var offset = GetOffsetFor(vb1.Current);
					byteOffsets[(byte)byteOffsets.Count] = offset;

					vb1.MoveNext();
					file.Write("  InputSlot: 0\n"u8.ToArray().AsSpan());

					continue;
				}

				if (vb1.Current.Trim().StartsWith("AlignedByteOffset"))
				{
					file.Write(Encoding.UTF8.GetBytes(vb1.Current.Split(": ")[0] + $": {elementsOffset}\n").AsSpan());
					elementsOffset += byteOffsets[(byte)(byteOffsets.Count - 1)];
					continue;
				}

				file.Write(Encoding.UTF8.GetBytes(vb1.Current + '\n').AsSpan());
			}

			vb1.MoveNext();
		}

		byteOffsets = new Dictionary<byte, byte>(byteOffsets.OrderBy(x => x.Key));

		while (vb0.Current.Trim() != "vertex-data:" && vb0.MoveNext())
		{
		}

		while (vb1.Current.Trim() != "vertex-data:" && vb1.MoveNext())
		{
		}

		while (vb2.Current.Trim() != "vertex-data:" && vb2.MoveNext())
		{
		}

		file.Write("\nvertex-data:\n\n"u8.ToArray().AsSpan());

		vb0.MoveNext();
		vb1.MoveNext();
		vb1.MoveNext();
		vb2.MoveNext();

		var index = 0;

		while (vb0.MoveNext() && vb0.Current != null && vb1.Current != null && vb2.Current != null)
		{
			byte lastOffset = 0;
			byte offsetCounter = 0;

			for (byte i = 0; i < 3; i++)
			{
				file.Write(Encoding.UTF8.GetBytes(vb0.Current + '\n').AsSpan());

				vb0.MoveNext();
				lastOffset += byteOffsets[offsetCounter];
				offsetCounter++;
			}

			while (vb0.Current.Trim() != "" && vb0.MoveNext())
			{
			}

			var vb2Count = 0;
			var blends = new string[2];

			while (vb2.MoveNext() && vb2.Current.Trim() != "")
			{
				blends[vb2Count] = VbRegex().Replace(vb2.Current, "vb0") + '\n';
				vb2Count++;
			}

			if (vb2Count < 2)
			{
				blends[1] = blends[0];

				blends[0] = VbRegex().Replace($"vb0[{index}]+044 BLENDWEIGHTS: 1", "vb0") + '\n';
			}

			foreach (var blendLine in blends)
			{
				var line = OffsetRegex().Replace(blendLine, OffsetCounterToString(lastOffset));

				file.Write(Encoding.UTF8.GetBytes(line).AsSpan());

				lastOffset += byteOffsets[offsetCounter];
				offsetCounter++;
			}

			for (byte i = 0; i < vb1Counter; i++)
			{
				var line = VbRegex().Replace(vb1.Current, "vb0");
				line = OffsetRegex().Replace(line, OffsetCounterToString(lastOffset));

				file.Write(Encoding.UTF8.GetBytes(line + '\n').AsSpan());

				vb1.MoveNext();
				lastOffset += byteOffsets[offsetCounter];
				offsetCounter++;
			}

			while (vb1.Current is not null && vb1.Current.Trim() != "" && vb1.MoveNext())
			{
			}

			vb1.MoveNext();

			file.Write("\n"u8.ToArray().AsSpan());

			index++;
		}

		vb1.Dispose();
	}

	private static string OffsetCounterToString(byte offset)
	{
		var str = offset.ToString();
		return str.Length > 2 ? $"+{str}" : str.Length > 1 ? $"+0{str}" : str.Length > 0 ? $"+00{str}" : "+000";
	}

	private static byte GetOffsetFor(string format)
	{
		return FormatRegex().Match(format).Groups[1].Value switch
		{
			"R16G16_FLOAT" or "R8G8B8A8_UNORM" or "R32_UINT" => 4,
			"R32G32_FLOAT" or "R32G32_UINT" => 8,
			"R32G32B32_FLOAT" => 12,
			"R32G32B32A32_FLOAT" or "R32G32B32A32_UINT" => 16
		};
	}
}