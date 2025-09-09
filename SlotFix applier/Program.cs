using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

await using var readStream = new FileInfo(@"/Users/oleksandr.fedotov/RiderProjects/ZZMI_tools/SlotFix applier/Nico Hashed.ini").OpenRead();
//await using var readStream = new FileInfo(@"/Users/oleksandr.fedotov/RiderProjects/ZZMI_tools/SlotFix applier/Nico Slotted.ini").OpenRead();
using var reader = new StreamReader(readStream, leaveOpen: false);
var lines = await reader.ReadToEndAsync();
lines = lines.Replace("\r\n", "\n");



if (!TryProcessHashed(lines, out var matches))
{
	Console.WriteLine("No matches found");
	return;
}

matches = slottedRegex.Matches(lines);

var output = matches.ReplaceGroup(lines, "Slot", ReplaceTextures);
Console.WriteLine(output);

partial class Program
{
	private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> textures = [];
	
	[GeneratedRegex(@"^[ \t]*\[TextureOverride(?<Component>\w*)(?<Classification>[A-Z])\]$\s^[ \t]*hash = \w{8}$\s^[ \t]*match_first_index = \d+$\s^[ \t]*(?<SkinTexture>run = CommandListSkinTexture)$\s^[ \t]*ib = Resource\w+$", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex();
	private static readonly Regex hashedRegex = MyRegex();
    
	[GeneratedRegex(@"^[ \t]*\[TextureOverride(?<Component>\w*)(?<Classification>[A-Z])(?<Texture>Diffuse|NormalMap|LightMap|MaterialMap)\]$\s^[ \t]*hash = \w{8}$\s^[ \t]*this = (?<Resource>Resource\w+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex1();
	private static readonly Regex hashedTextures = MyRegex1();
    
	[GeneratedRegex(@"^[ \t]*\[TextureOverride(?<Component>\w*)(?<Classification>[A-Z])\]$\s^[ \t]*hash = \w{8}$\s^[ \t]*match_first_index = \d+$(?:\s^[ \t]*run = CommandListSkinTexture$)?\s^[ \t]*ib = Resource\w+$(?:\s^[ \t]*ps-t(?<Slot>[3456]) = (?<Texture>Resource\w*)$){1,4}", RegexOptions.Multiline | RegexOptions.Compiled)]
	private static partial Regex MyRegex2();
	private static readonly Regex slottedRegex = MyRegex2();

	private static bool TryProcessHashed(in string lines, out MatchCollection outMatches)
	{
		var matches = hashedTextures.Matches(lines);
		if (matches.Count == 0)
		{
			outMatches = null;
			return false;
		}
		foreach (Match match in matches)
		{
			ref var component = ref CollectionsMarshal.GetValueRefOrAddDefault(textures, match.Groups["Component"].Value, out var exists);
			if (!exists)
				component = [];
			ref var classification = ref CollectionsMarshal.GetValueRefOrAddDefault(component, match.Groups["Classification"].Value, out exists);
			if (!exists)
				classification = [];
			classification[match.Groups["Texture"].Value] = match.Groups["Resource"].Value;
		}
		
		outMatches = hashedRegex.Matches(lines);
		return true;
	}
	
	private static bool TryProcessSlotted(in string lines, out MatchCollection outMatches)
	{
		var matches = hashedTextures.Matches(lines);
		if (matches.Count == 0)
		{
			outMatches = null;
			return false;
		}
		foreach (Match match in matches)
		{
			ref var component = ref CollectionsMarshal.GetValueRefOrAddDefault(textures, match.Groups["Component"].Value, out var exists);
			if (!exists)
				component = [];
			ref var classification = ref CollectionsMarshal.GetValueRefOrAddDefault(component, match.Groups["Classification"].Value, out exists);
			if (!exists)
				classification = [];
			classification[match.Groups["Texture"].Value] = match.Groups["Resource"].Value;
		}
		
		outMatches = hashedRegex.Matches(lines);
		return true;
	}
	
	private static void ReplaceTextures(Match m, Group _, Capture c, StringBuilder sb)
	{
		var classification = m.Groups["Classification"].Value;
		var component = textures[m.Groups["Component"].Value];
		if (!component.TryGetValue(classification, out var value))
		{
			if (component.Count != 1)
			{
				sb.Append(c.ValueSpan);
				return;
			}
		
			value = component[component.Keys.First()];
		}

		foreach (var (texture, resource) in value)
		{
			sb.Append(@$"Resource\ZZMI\{texture} = ref {resource}".AsSpan());
			sb.Append('\n');
		}
		sb.Append(@"run = Commandlist\ZZMI\SetTextures".AsSpan());
		sb.Append('\n');
	}
}

internal static class RegexReplace
{
	public static string ReplaceGroup(
		string input,
		Regex regex,
		string groupName,
		Action<Match, Group, Capture, StringBuilder> writeGroupReplacement)
	{
		return ReplaceGroup(input, regex.Matches(input), groupName, writeGroupReplacement);
	}
	public static string ReplaceGroup(
		this Regex regex,
		string input,
		string groupName,
		Action<Match, Group, Capture, StringBuilder> writeGroupReplacement)
	{
		return ReplaceGroup(input, regex, groupName, writeGroupReplacement);
	}

	public static string ReplaceGroup(this MatchCollection matches,
		string input,
		string groupName,
		Action<Match, Group, Capture, StringBuilder> writeGroupReplacement)
	{
		return ReplaceGroup(input, matches, groupName, writeGroupReplacement);
	}
	public static string ReplaceGroup(
		string input,
		MatchCollection matches,
		string groupName,
		Action<Match, Group, Capture, StringBuilder> writeGroupReplacement)
	{
		var span = input.AsSpan();
		var sb = new StringBuilder(input.Length);
		var pos = 0;

		foreach (Match m in matches)
		{
			sb.Append(span.Slice(pos, m.Index - pos));
			var cur = m.Index;

			var g = m.Groups[groupName];
			if (!g.Success) continue;
			
			foreach (Capture capture in g.Captures)
			{
				sb.Append(span.Slice(cur, capture.Index - cur));
				writeGroupReplacement(m, g, capture, sb);
				cur = capture.Index + capture.Length;


				sb.Append(span.Slice(cur, m.Index + m.Length - cur));
				pos = m.Index + m.Length;
			}
		}

		sb.Append(span[pos..]);
		return sb.ToString();
	}
}
