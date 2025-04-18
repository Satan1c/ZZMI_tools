using System.Text.Json.Serialization;

namespace Fixer;

public struct CollectionData
{
	[JsonPropertyName("_aRecords")] public Record[] records { get; set; }
}

public struct Record
{
	[JsonPropertyName("_sName")] public string Name { get; set; }

	[JsonPropertyName("_idRow")] public ushort Id { get; set; }

	[JsonPropertyName("_sProfileUrl")] public string Url { get; set; }
}

public struct FilesData
{
	[JsonPropertyName("_aFiles")] public FileData[] Files { get; set; }
}

public struct FileData
{
	[JsonPropertyName("_sFile")] public string Name { get; set; }

	[JsonPropertyName("_sDownloadUrl")] public string Url { get; set; }

	[JsonPropertyName("_tsDateAdded")] public long Timestamp { get; set; }

	[JsonIgnore] public DateTimeOffset Added => DateTimeOffset.FromUnixTimeSeconds(Timestamp);
}