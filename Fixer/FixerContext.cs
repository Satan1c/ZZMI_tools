using System.Text.Json.Serialization;

namespace Fixer;

[JsonSerializable(typeof(CollectionData))]
[JsonSerializable(typeof(Record))]
[JsonSerializable(typeof(FilesData))]
[JsonSerializable(typeof(FileData))]
public partial class FixerContext : JsonSerializerContext
{
}