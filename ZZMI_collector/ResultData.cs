using System.Text.Json.Serialization;

namespace ZZMI_collector;

public struct Result(ResultData lod0, ResultData[] lods)
{
	[JsonPropertyName("component_name")] public string Name { get; set; } = string.Empty;

	[JsonPropertyName("ib")] public string Ib { get; set; } = lod0.Ib;

	[JsonPropertyName("draw_vb")] public string Draw { get; set; } = lod0.Draw;

	[JsonPropertyName("texcoord_vb")] public string Textcoord { get; set; } = lod0.Textcoord;

	[JsonPropertyName("position_vb")] public string Position { get; set; } = lod0.Position;

	[JsonPropertyName("blend_vb")] public string Blend { get; set; } = lod0.Blend;

	[JsonPropertyName("object_indexes")] public long[] Indexes { get; set; } = lod0.Indexes ?? [];

	[JsonPropertyName("object_classifications")]
	public char[] Classifications { get; set; } = lod0.Indexes?.Select((_, i) => (char)('A' + i % 26)).ToArray() ?? [];

	[JsonPropertyName("texture_hashes")] public string[][][] Textures { get; set; } = lod0.Textures ?? [[[]]];

	[JsonPropertyName("lods")] public ResultData[] Lods { get; set; } = lods;
}

public struct ResultData(
	string ib,
	VBs vbs,
	string[][][] textures,
	long[] indexes
)
{
	[JsonPropertyName("ib")] public string Ib { get; set; } = ib;

	[JsonPropertyName("draw_vb")] public string Draw { get; set; } = vbs.Draw;

	[JsonPropertyName("texcoord_vb")] public string Textcoord { get; set; } = vbs.Textcoord;

	[JsonPropertyName("position_vb")] public string Position { get; set; } = vbs.Position;

	[JsonPropertyName("blend_vb")] public string Blend { get; set; } = vbs.Blend;

	[JsonPropertyName("object_indexes")] public long[] Indexes { get; set; } = indexes;

	[JsonPropertyName("texture_hashes")] public string[][][] Textures { get; set; } = textures;
}

public struct VBs(string[] vbs)
{
	public string Draw { get; set; } = vbs[0];

	public string Textcoord { get; set; } = vbs.Length > 1 ? vbs[1] : string.Empty;

	public string Position { get; set; } = vbs[vbs.Length > 1 ? 2 : 0];

	public string Blend { get; set; } = vbs.Length > 1 ? vbs[3] : string.Empty;
}