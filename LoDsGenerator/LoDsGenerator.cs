using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

var singleOverrides = true;
singleOverrides = (args.Length > 0 && args[0] != "split") || (Console.ReadLine() != "split");

Directory.SetCurrentDirectory("/Users/oleksandr.fedotov/RiderProjects/ZZMI_tools/LoDsGenerator/dumps");
var dumps = Directory
	.EnumerateDirectories(Directory.GetCurrentDirectory())
	.Select(x => DumpNameRegex.Match(x))
	.Where(x => x.Success)
	.OrderBy(x => byte.Parse(x.Groups["Index"].Value))
	.Select<Match, (Match match, string file)>(x => (x, Directory.EnumerateFiles(x.Groups["0"].Value).AsParallel().First(y => Path.GetFileName(y) == "hash.json")))
	.Select((x) =>
	{
		var components = JsonSerializer.Deserialize(File.ReadAllText(x.file), ComponentFormatContext.Default.ComponentFormatArray)!;
		return new Dump(x.match, components);
	})
	.ToArray();

var resultPath = Path.Join(Directory.GetCurrentDirectory(), "result");
if (!Directory.Exists(resultPath))
	Directory.CreateDirectory(resultPath);
Directory.SetCurrentDirectory(resultPath);

var commandsPath = Path.Join(resultPath, "commands.ini");
if (!File.Exists(commandsPath))
	File.Create(commandsPath).Close();
var resourcesPath = Path.Join(resultPath, "resources.ini");
if (!File.Exists(resourcesPath))
	File.Create(resourcesPath).Close();

var tasks = new List<Task>(5);
var commandsSb = new StringBuilder("namespace = \n", 15 * 1024);
var resourcesSb = new StringBuilder("namespace = \n", 15 * 1024);

for (byte i = 0; i < dumps[0].Components.Length; i++)
{
	ref var component = ref dumps[0].Components[i];
	ProcessClassifications(commandsSb, ref component, ref dumps[0],
		!(string.IsNullOrEmpty(component.Blend) || string.IsNullOrEmpty(component.Texcoord))
			? CommandsTemplates
			: CommandsTemplatesEmptyIb);
	ProcessClassifications(resourcesSb, ref component, ref dumps[0], ResourceTemplates, true);
	resourcesSb.Append("\n\n");
	commandsSb.Append("\n\n");
}

if (singleOverrides)
{
	WriteSingleOverrides();
}
else
{
	WriteMultiOverrides();
}

tasks.Add(File.WriteAllTextAsync(commandsPath, commandsSb.ToString().Trim()));
tasks.Add(File.WriteAllTextAsync(resourcesPath, resourcesSb.ToString().Trim()));
await Task.WhenAll(CollectionsMarshal.AsSpan(tasks));
return;

void WriteMultiOverrides()
{
	var overridesPath = Path.Join(resultPath, "override");
	var inisSbs = Enumerable.Range(0, 3).Select(x => new StringBuilder("namespace = \n", 5 * 1024)).ToArray();
	for (byte i = 0; i < dumps.Length; i++)
	{
		var overridesSb = inisSbs[i];
		ref var dump = ref dumps[i];
		for (byte j = 0; j < dump.Components.Length; j++)
		{
			ref var component = ref dump.Components[j];
			ProcessClassifications(overridesSb, ref component, ref dump, OverridesTemplates);
		}

		overridesSb.Append("\n\n");
	}
	
	tasks.AddRange(inisSbs.Select((x, i) => File.WriteAllTextAsync(overridesPath + $"LoD{i}.ini", x.ToString().Trim())));
}

void WriteSingleOverrides()
{
	var overridesPath = Path.Join(resultPath, "overrides.ini");
	if (!File.Exists(overridesPath))
		File.Create(overridesPath).Close();
	var overridesSb = new StringBuilder("namespace = \n", 10 * 1024);

	for (byte i = 0; i < dumps.Length; i++)
	{
		ref var dump = ref dumps[i];
		for (byte j = 0; j < dump.Components.Length; j++)
		{
			ref var component = ref dump.Components[j];
			ProcessClassifications(overridesSb, ref component, ref dump, OverridesTemplates);
		}

		overridesSb.Append("\n\n");
	}
	tasks.Add(File.WriteAllTextAsync(overridesPath, overridesSb.ToString().Trim()));
}

public record struct Dump(Match Match, ComponentFormat[] Components);
public struct ComponentFormat
{
	[JsonPropertyName("component_name")]
	public string Name { get; set; }
	[JsonPropertyName("draw_vb")]
	public string Draw { get; set; }
	[JsonPropertyName("blend_vb")]
	public string Blend { get; set; }
	[JsonPropertyName("texcoord_vb")]
	public string Texcoord { get; set; }
	[JsonPropertyName("ib")]
	public string Ib { get; set; }
	
	[JsonPropertyName("object_indexes")]
	public ushort[] Indexes { get; set; }

	[JsonPropertyName("object_index_counts")]
	public ushort[]? IndexCounts { get; set; }
	[JsonPropertyName("object_classifications")]
	public char[] Classifications { get; set; }
	/*[JsonPropertyName("texture_hashes")]
	public string[][][] Textures { get; set; }*/
}

[JsonSerializable(typeof(ComponentFormat[]))]
public partial class ComponentFormatContext : JsonSerializerContext;

public static partial class Program
{
	private const string BlendCommandListTemplate =
		"""
		
		[CommandList.{2}.Blend]
		handling = skip
		vb2 = Resource.{2}.Blend
		if DRAW_TYPE == 1
			vb0 = Resource.{2}.Position
			Draw = {6}, 0
		endif
		""";
	private const string IbCommandListTemplate =
		"""
		
		[CommandList.{2}{4}]
		handling = skip
		ib = Resource.{2}{4}
		
		Resource\ZZMI\Diffuse = ref Resource.{2}{4}.Diffuse
		Resource\ZZMI\NormalMap = ref Resource.{2}{4}.NormalMap
		Resource\ZZMI\LightMap = ref Resource.{2}{4}.LightMap
		Resource\ZZMI\MaterialMap = ref Resource.{2}{4}.MaterialMap
		run = CommandList\ZZMI\SetTextures
		
		DrawIndexed = auto
		""";
	private const string EmptyIbCommandListTemplate =
		"""
		
		[CommandList.{2}{4}]
		Resource\ZZMI\Diffuse = ref Resource.{2}{4}.Diffuse
		Resource\ZZMI\NormalMap = ref Resource.{2}{4}.NormalMap
		Resource\ZZMI\LightMap = ref Resource.{2}{4}.LightMap
		Resource\ZZMI\MaterialMap = ref Resource.{2}{4}.MaterialMap
		run = CommandList\ZZMI\SetTextures
		""";
	
	private const string IbResourceTemplate =
		"""
		
		[Resource.{2}{4}]
		type = Buffer
		format = DXGI_FORMAT_R32_UINT
		filename = Resources\{0}{2}{4}.ib
		""";
	private const string VbResourceTemplate =
		"""
		
		[Resource.{2}.{8}]
		type = Buffer
		stride = {7}
		filename = Resources\{0}{2}{4}{8}.buf
		""";
	private const string TextureResourceTemplate =
		"""
		
		[Resource.{2}{4}.{8}]
		filename = Resources\{0}{2}{4}{8}.dds
		""";
	
	private const string BlendOverrideTemplate =
		"""
		
		[TextureOverride {2} Blend LoD{1}]
		hash = {3}
		run = CommandList.{2}.Blend
		""";
	private const string DrawOverrideTemplate =
		"""
		
		[TextureOverride {2} Draw LoD{1}]
		hash = {3}{9}
		""";
	private const string TexcoordOverrideTemplate =
		"""
		
		[TextureOverride {2} Texcoord LoD{1}]
		hash = {3}
		vb1 = Resource.{2}.Texcoord
		""";
	private const string IbOverrideTemplate =
		"""
		
		[TextureOverride {2}{4} LoD{1}]
		hash = {3}
		{5}
		run = CommandList.{2}{4}
		""";
	
	private static string[] OverridesTemplates = [
		BlendOverrideTemplate,
		TexcoordOverrideTemplate,
		IbOverrideTemplate,
		DrawOverrideTemplate
	];
	private static string[] CommandsTemplates = [
		BlendCommandListTemplate,
		string.Empty,
		IbCommandListTemplate,
		string.Empty
	];
	private static string[] CommandsTemplatesEmptyIb = [
		BlendCommandListTemplate,
		string.Empty,
		EmptyIbCommandListTemplate,
		string.Empty
	];
	private static string[] ResourceTemplates = [
		VbResourceTemplate,
		VbResourceTemplate,
		IbResourceTemplate,
		TextureResourceTemplate
	];
	
	private static string[] TextureNames = [
		"Diffuse",
		"NormalMap",
		"LightMap",
		"MaterialMap"
	];

	[GeneratedRegex(@"(?<Name>\w+)LoD(?<Index>\d)", RegexOptions.Compiled)]
	private static partial Regex GetDumpNameRegex();
	private static Regex DumpNameRegex => GetDumpNameRegex();
	
	private static object[] GetFormatStrings(
		ref Dump dump,
		ref ComponentFormat component,
		string hash = "",
		char classification = '\0',
		ulong firstIndex = 0,
		ulong indexCount = 0,
		ulong size = 0,
		ulong stride = 0,
		ulong count_override = 0,
		ulong stride_override = 0,
		string semanticName = ""
		)
	{
		var indexes = new StringBuilder(127);
		indexes.Append("match_first_index = ")
			.Append(firstIndex);
		if (indexCount != 0)
		{
			indexes
				.Append('\n')
				.Append("match_index_count = ")
				.Append(indexCount)
				.Append('\n');
		}

		var overrides = new StringBuilder(127);
		if (count_override != 0 && stride_override != 0)
		{
			overrides
				.Append('\n')
				.Append("override_byte_stride = ")
				.Append(stride_override)
				.Append('\n')
				.Append("override_vertex_count = ")
				.Append(count_override)
				.Append('\n');
		}
		return [
			dump.Match.Groups["Name"].Value,
			dump.Match.Groups["Index"].Value,
			component.Name,
			hash,
			classification.ToString(),
			indexes.ToString(),
			size.ToString(),
			stride.ToString(),
			semanticName,
			overrides.ToString()
		];
	}
	
	private static void ProcessClassifications(StringBuilder builder, ref ComponentFormat component, ref Dump dump, string[] templates, bool textures = false)
	{
		for (byte k = 0; k < component.Indexes.Length; k++)
		{
			var p = GetFormatStrings(
				ref dump,
				ref component,
				component.Blend,
				component.Classifications[k],
				component.Indexes[k],
				component.IndexCounts?[k] ?? 0
			);
			if (!string.IsNullOrEmpty(templates[0]) && !string.IsNullOrEmpty(component.Blend))
			{
				p[8] = "Blend";
				builder
					.AppendFormat(templates[0], p)
					.Append('\n');
			}
			
			if (!string.IsNullOrEmpty(templates[1]) && !string.IsNullOrEmpty(component.Texcoord))
			{
				p[8] = "Texcoord";
				p[3] = component.Texcoord;
				builder
					.AppendFormat(templates[1], p)
					.Append('\n');
			}

			if (!string.IsNullOrEmpty(templates[3]) && !string.IsNullOrEmpty(component.Draw))
			{
				p[8] = "Draw";
				p[3] = component.Draw;
				builder
					.AppendFormat(templates[3], p)
					.Append('\n');
			}

			if (string.IsNullOrEmpty(templates[2])) continue;

			if (!textures || !(string.IsNullOrEmpty(component.Blend) && string.IsNullOrEmpty(component.Texcoord)))
			{
				p[3] = component.Ib;
				builder.AppendFormat(templates[2], p);
				if (textures)
				{
					p[8] = "Position";
					builder
						.Append('\n')
						.AppendFormat(templates[1], p);
					builder.Append('\n');
				}
			}
			
			if (textures)
			{
				foreach (var t in TextureNames)
				{
					p[8] = t;
					builder.AppendFormat(templates[3], p);
				}
			}

			builder.Append('\n');
		}
	}
}
