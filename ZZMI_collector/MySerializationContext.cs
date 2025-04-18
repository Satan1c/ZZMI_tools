using System.Text.Json.Serialization;

namespace ZZMI_collector;

[JsonSourceGenerationOptions(WriteIndented = true, IndentCharacter = '\t', IndentSize = 1,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	GenerationMode = JsonSourceGenerationMode.Serialization)]

//[JsonSerializable(typeof(ResultData[][]), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(Result[]), GenerationMode = JsonSourceGenerationMode.Serialization)]
public partial class MySerializationContext : JsonSerializerContext
{
}