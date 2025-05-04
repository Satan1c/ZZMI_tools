using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ObjectData = (string frame, long index, string[] textures, string[] vbs);

namespace ZZMI_collector;

public static partial class Processor
{
	private static readonly string s_baseDirectory = "./";
	private static readonly string s_filteredDirectory = s_baseDirectory + @"filtered\";

	private static readonly string s_filteredDdsDirectory = s_filteredDirectory + @"dds\";

	//private static readonly string s_filteredJbgDirectory = s_filteredDirectory + @"jpg\";

	private static readonly string[] s_lodDirs =
		Directory.EnumerateDirectories(s_baseDirectory, "FrameAnalysis-*").OrderBy(x => x).ToArray();

	private static readonly Dictionary<string, LinkedList<ObjectData>>[] s_lodArray
		= Enumerable.Range(0, s_lodDirs.Length).Select(_ => new Dictionary<string, LinkedList<ObjectData>>()).ToArray();

	[GeneratedRegex("([0-9]{6})-", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
	private static partial Regex FrameRegex();

	[GeneratedRegex(@"-(ps-t[1-6])=(.{8}).*\.dds",
		RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
	private static partial Regex PsRegex();

	[GeneratedRegex("[0-9]{6}-(ib|vb[012])?=(.{8})",
		RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
	private static partial Regex IbVbRegex();

	[GeneratedRegex("-vs=(.{16})", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
	private static partial Regex VsRegex();

	[GeneratedRegex(@"first index: (\d+)",
		RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
	private static partial Regex IndexRegex();

	public static void Start(string target, string[] elements)
	{
		foreach (var textureHash in elements.Skip(1)) ProcessHash(textureHash);

		foreach (var lod in s_lodArray)
		foreach (var (ib, data) in lod)
			lod[ib] = new LinkedList<ObjectData>(data.OrderBy(x => x.index));

		var lod0 = s_lodArray[0];
		var componentsList = new Result[lod0.Count];

		var result = Enumerable.Range(0, s_lodArray.First().Count)
			.Select(
				x => s_lodArray.Select(y => y.ToArray()[x])
					.Skip(1)
					.Select(y => ProcessResultData(y.Key, y.Value))
					.ToArray()
			)
			.ToArray();

		for (byte i = 0; i < componentsList.Length;)
			foreach (var (ib, dataList) in lod0)
			{
				componentsList[i] = new Result(ProcessResultData(ib, dataList), result[i]);
				i++;
			}

		//LogResult(componentsList);

		var serialized = JsonSerializer.Serialize(componentsList, typeof(Result[]), MySerializationContext.Default);

		var dir = s_baseDirectory + $"collected\\{target}";
		if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
		using var json = File.Open(dir + "\\hash.json", FileMode.Create);
		json.Write(Encoding.UTF8.GetBytes(serialized));
		json.Close();

		CopyAssets(target, dir, componentsList);
		Console.WriteLine("done");
	}

	private static void CopyAssets(string target, string dir, Result[] assets)
	{
		var lodDir = s_lodDirs[0];

		foreach (var res in assets)
		{
			using var ibFile = Directory.EnumerateFiles(lodDir, $"*{res.Ib}*.txt").GetEnumerator();

			foreach (var subpartIndex in Enumerable.Range(0, res.Classifications.Length))
			{
				var classification = res.Classifications[subpartIndex].ToString();
				var fullTarget = $"{target}{res.Ib}{classification}";
				ibFile.MoveNext();
				File.Copy(ibFile.Current, dir + $"\\{fullTarget}-ib={res.Ib}.txt", true);

				foreach (var textures in res.Textures)
				foreach (var textureArr in textures)
				{
					var texture = textureArr[^1];
					texture = texture == string.Empty ? textureArr[^2] : texture;

					var textureFile = Directory.EnumerateFiles(lodDir, $"*{texture}*.dds").First();
					File.Copy(textureFile, dir + "\\" + fullTarget + textureArr[0] + ".dds", true);
				}

				Vb0Merger.Merge(fullTarget, lodDir, dir, res.Position, res.Textcoord, res.Blend);
			}
		}
	}

	private static bool TryGetQuality(string path, out uint quality)
	{
		using var ddsImage = File.OpenRead(path);
		using var reader = new BinaryReader(ddsImage);

		return TryGetQuality(reader, out quality);
	}

	private static bool TryGetQuality(BinaryReader reader, out uint quality)
	{
		quality = 0;
		var signature = reader.ReadBytes(4);
		if (!(signature[0] == 'D' && signature[1] == 'D' && signature[2] == 'S' && signature[3] == ' ')) return false;

		reader.ReadUInt32();
		reader.ReadUInt32();

		var height = reader.ReadUInt32();
		var width = reader.ReadUInt32();

		quality = uint.Min(height, width);
		return true;
	}

	private static void LogResult(Result[] componentsList)
	{
		for (byte i = 0; i < componentsList.Length; i++)
		{
			var obj = componentsList[i];
			Console.WriteLine($"-\tobj {i}\n");

			LogLod0Data(obj);

			Console.WriteLine("-\t\ttextures:\n");

			foreach (var t in obj.Textures)
			{
				Console.WriteLine($"-\t\t\t\t{string.Join("\n-\t\t\t\t", t.Select(x => string.Join(',', x)))}");
				Console.WriteLine();
			}

			Console.WriteLine();

			for (byte j = 0; j < obj.Lods.Length; j++) LogLodData(obj.Lods[j]);

			Console.WriteLine($"\n{new string('-', 20)}\n");
		}
	}

	private static void LogLodData(ResultData lodData)
	{
		Console.WriteLine($"-\t\tib\t\t{lodData.Ib}");
		Console.WriteLine($"-\t\tindexes\t\t{string.Join(',', lodData.Indexes)}");

		Console.WriteLine($"-\t\tdraw:\t\t{lodData.Draw}");

		if (lodData.Textcoord != string.Empty) Console.WriteLine($"-\t\ttexcoord:\t{lodData.Textcoord}");

		Console.WriteLine($"-\t\tposition:\t{lodData.Position}");

		if (lodData.Blend != string.Empty) Console.WriteLine($"-\t\tblend:\t\t{lodData.Blend}");

		Console.WriteLine("\n");
	}

	private static void LogLod0Data(Result lodData)
	{
		Console.WriteLine($"-\t\tib\t\t{lodData.Ib}");
		Console.WriteLine($"-\t\tindexes\t\t{string.Join(',', lodData.Indexes)}");

		Console.WriteLine($"-\t\tdraw:\t\t{lodData.Draw}");

		if (lodData.Textcoord != string.Empty) Console.WriteLine($"-\t\ttexcoord:\t{lodData.Textcoord}");

		Console.WriteLine($"-\t\tposition:\t{lodData.Position}");

		if (lodData.Blend != string.Empty) Console.WriteLine($"-\t\tblend:\t\t{lodData.Blend}");

		Console.WriteLine("\n");
	}

	private static void ProcessHash(string textureHash)
	{
		ProcessLod(0, textureHash);

		for (byte lodIndex = 1; lodIndex < s_lodDirs.Length; lodIndex++) ProcessLod(lodIndex, textureHash);
	}

	private static void ProcessLod(byte lodIndex, string textureHash)
	{
		Console.WriteLine($"{lodIndex}: {s_lodDirs.Length}");
		var lodDir = s_lodDirs[lodIndex];

		var matches = Directory.EnumerateFiles(lodDir, $"*ps-t3={textureHash}*")
			.Select(x => FrameRegex().Match(x).Groups["1"].Value)
			.Select(x => Directory.EnumerateFiles(lodDir, $"{x}*ps-t*").Where(y => PsRegex().IsMatch(y)).ToArray());

		var frames = matches.Where(x => x is { Length: 3 or 5 })
			.Select(texturePaths => (FrameRegex().Match(texturePaths.First()).Groups["1"].Value, x: texturePaths))
			.Where(
				frameNumber =>
				{
					if (Directory.EnumerateFiles(lodDir, $"{frameNumber.Value}*.txt").Count() < 3)
						return frameNumber.x.Length < 5;

					return true;
				}
			)
			.Select(x => x.Value)
			.ToArray();

		foreach (var frame in frames) ProcessFrame(frame, lodDir, lodIndex);
	}

	private static void ProcessFrame(string frame, string lodDir, byte lodIndex)
	{
		var obj = new ObjectData { frame = frame, index = -1 };

		var first = Directory.EnumerateFiles(lodDir, $"{frame}*ib*txt").First();
		var ib = IbVbRegex().Match(first).Groups["2"].Value;

		obj.index = GrabIndex(first);

		obj.textures = Directory.EnumerateFiles(lodDir, $"{frame}*ps-t*")
			.Select(x => PsRegex().Match(x))
			.Where(x => x.Success)
			.Select(x => x.Groups["2"].Value)
			.TakeLast(4)
			.ToArray();

		var vbs = new LinkedList<string>(
			Directory.EnumerateFiles(lodDir, $"{frame}*vb*txt")
				.Select(x => IbVbRegex().Match(x))
				.Where(x => x.Success)
				.Select(x => x.Groups["2"].Value)
		);

		first = Directory.EnumerateFiles(lodDir, $"*vb*{vbs.Last!.Value}*txt").OrderBy(x => x).First();
		var vs = VsRegex().Match(first).Groups["1"].Value;
		var files = Directory.GetFiles(lodDir, $"{FrameRegex().Match(first).Groups["1"].Value}*vb*{vs}*txt");

		vbs.AddLast(IbVbRegex().Match(files[0]).Groups["2"].Value);
		if (vbs.Count > 1) vbs.AddLast(IbVbRegex().Match(files[2]).Groups["2"].Value);

		obj.vbs = vbs.ToArray();

		if (!s_lodArray[lodIndex].ContainsKey(ib)) s_lodArray[lodIndex][ib] = [];

		if (obj.vbs.Any(string.IsNullOrEmpty)) return;

		if (!s_lodArray.Any(lod =>
			    lod.TryGetValue(ib, out var xList) &&
			    xList.Any(y => y.vbs.SequenceEqual(obj.vbs) && y.index == obj.index)))
			s_lodArray[lodIndex][ib].AddLast(obj);
	}

	private static long GrabIndex(string filePath)
	{
		using var file = File.OpenText(filePath);

		for (byte j = 0; j < 5; j++)
		{
			var line = file.ReadLine();
			if (line is null) break;

			var match = IndexRegex().Match(line);

			if (!match.Success) continue;

			return long.Parse(match.Groups["1"].Value);
		}

		file.Close();

		return -1;
	}

	private static ResultData ProcessResultData(string ib, LinkedList<ObjectData> dataList)
	{
		if (dataList.First is null) return default;

		var vbs = new VBs(dataList.First.Value.vbs);

		var textures = dataList.OrderBy(z => z.index)
			.Select(
				(z, i) => z.textures.Select(
						(w, j) =>
						{
							var is2K = false;

							if (TryGetQuality(GetTextureAsset(w, i), out var quality))
								is2K = quality > 1024;

							return new[]
							{
								j switch
								{
									0 => "Diffuse",
									1 => "NormalMap",
									2 => z.textures.Length > 3 ? "LightMap" : "StockingMap",
									3 => "MaterialMap",
									_ => string.Empty
								},
								".dds", is2K ? string.Empty : w, is2K ? w : string.Empty
							};
						}
					)
					.ToArray()
			)
			.ToArray();

		var indexes = dataList.Select(z => z.index).OrderBy(z => z).ToArray();

		return new ResultData(ib, vbs, textures, indexes);
	}

	private static string GetTextureAsset(string hash, int i)
	{
		return Directory.GetFiles(s_filteredDdsDirectory, $"*{hash}*", SearchOption.AllDirectories).FirstOrDefault()

		       //?? Directory.GetFiles(s_filteredJbgDirectory, $"*{hash}*", SearchOption.AllDirectories).FirstOrDefault()
		       ?? Directory.GetFiles(s_lodDirs[i], $"*{hash}*", SearchOption.AllDirectories).First();
	}
}