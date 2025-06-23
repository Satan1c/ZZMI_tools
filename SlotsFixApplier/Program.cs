using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

const string og = @"Belle.ini";
const string edited = @"Edited.ini";

using var file = File.OpenRead(og);
using var reader = new StreamReader(file, leaveOpen: false);
var data = reader.ReadToEnd();

var ibSectionsRegex = IbSectionsRegex();
var resourcesRegex = ResourcesRegex();
var normalMapsRegex = NormalMapsRegex();

data = normalMapsRegex.Replace(data, string.Empty);

var resources = new Dictionary<string, LinkedList<string>>();
foreach (Match match in resourcesRegex.Matches(data))
{
	var part = match.Groups["Part"].Value;
	var texture = match.Groups["Texture"].Value;
	ref var resource = ref CollectionsMarshal.GetValueRefOrAddDefault(resources, part, out var exists);
	if (!exists)
	{
		resource = [];
	}

	resource!.AddLast(texture);
}

const string SlotFixDiffuse = @"Resource\ZZMI\Diffuse = ref Resource{0}Diffuse";
const string SlotFixLmMm = @"Resource\ZZMI\LightMap = ref Resource{0}LightMap
Resource\ZZMI\MaterialMap = ref Resource{0}MaterialMap";
const string SlotFixNormalMap = @$"{SlotFixDiffuse}
Resource\ZZMI\NormalMap = ref Resource{{0}}NormalMap
{SlotFixLmMm}";
const string SlotFixFlatNormalMap = @$"{SlotFixDiffuse}
Resource\ZZMI\NormalMap = ref Resource\ZZMIv1\FlatNormalMap
{SlotFixLmMm}";

data = ibSectionsRegex.Replace(data, (match) =>
{
	var name = match.Groups["Name"].Value;
	var hash = match.Groups["Hash"].Value;
	var index = match.Groups["Index"].Value;
	var ib = match.Groups["IB"].Value;

	var res = resources[name];

	var replacement = @$"[TextureOverride{name}]
hash = {hash}{index}{ib}
{
	string.Format(res.Count switch
	{
		1 => SlotFixDiffuse,
		3 => SlotFixFlatNormalMap,
		_ => SlotFixNormalMap
	}, name)
}
run = CommandList\ZZMI\SetTextures";
	return replacement;
});

using var editedFile = File.Create(edited);
using var writer = new StreamWriter(editedFile, Encoding.UTF8);
writer.Write(data);

internal partial class Program
{
	[GeneratedRegex(
		@"\[TextureOverride(?<Name>\w+[^IB]{2}[A-Z]{1}|\w+Face[A-Z]?)(?:IB)?\]\s+(?:hash = (?<Hash>\w{8}))(?<Index>\s+match_first_index = (?<IndexValue>\d+))?\s*(?<SkinTexture>run = CommandListSkinTexture)(?<IB>\s+ib = (?<IBvalue>\w+))?(?:\s+(?:Resource\\ZZMI\\NormalMap = ref |ps-t4 = )\w+\s+)?(?<SetTextures>run = CommandList\\ZZMI\\SetTextures)?",
		RegexOptions.Compiled | RegexOptions.Singleline)]
	private static partial Regex IbSectionsRegex();

	[GeneratedRegex(@"\[Resource(?<Part>\w+[A-Z]{1})(?<Texture>Diffuse|NormalMap|LightMap|MaterialMap)\]",
		RegexOptions.Compiled)]
	private static partial Regex ResourcesRegex();

	[GeneratedRegex(@"\s*\[TextureOverride[\w.]+NormalMap(?:\.1024|\.2048)?\]\s*hash = \w{8}\s*this = \w+\s",
		RegexOptions.Compiled)]
	private static partial Regex NormalMapsRegex();
}