using System.Text;
using System.Text.RegularExpressions;
using SlotsFixApplier.Parser.Extensions;

namespace SlotsFixApplier.Parser;

internal static partial class ResourceSectionRegex
{
	public static Regex ResourceNameRegex = ResourceNameRegexGenerated();
	public static Regex ResourceFileNameRegex = ResourceFileNameRegexGenerated();
	public static Regex ResourceFormatRegex = ResourceFormatRegexGenerated();
	public static Regex ResourceStrideRegex = ResourceStrideGenerated();
	public static Regex ResourceTypeBufferRegex = ResourceTypeBufferRegexGenerated();

	[GeneratedRegex(
		@"^\[Resource(?<Name>[^\]]+(?:Blend|Position|Texcoord|IB|Diffuse(?:Map)?|(?:Normal|Light|Material|HighLight)Map))\]",
		RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceNameRegexGenerated();

	[GeneratedRegex(@"^filename\s*=\s*(?<FileName>.+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceFileNameRegexGenerated();

	[GeneratedRegex(@"^format\s*=\s*DXGI_FORMAT_(?<Format>\w+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceFormatRegexGenerated();

	[GeneratedRegex(@"^stride\s*=\s*(?<Stride>\d+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceStrideGenerated();

	[GeneratedRegex(@"^type\s*=\s*(?<Type>\w+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceTypeBufferRegexGenerated();
}

internal struct ResourceSection
{
	public ResourceType Type;
	public string Name;
	public string FileName;
	public byte? Stride;
	public ResourceFormat? Format;

	public string WriteSection()
	{
		var sb = new StringBuilder();
		sb.AppendFormat("[Resource{0}]\n", Name);
		if (Type == ResourceType.Buffer)
		{
			sb.Append("type = Buffer\n");
			if (Format.HasValue)
				sb.AppendFormat("format = DXGI_FORMAT_{0}\n", Format.Value.ResourceFormatToString());
			if (Stride.HasValue)
				sb.AppendFormat("stride = {0}\n", Stride.Value.ToString());
		}

		sb.AppendFormat("filename = {0}\n", FileName);
		return sb.ToString();
	}

	public static (bool success, ResourceSection section, string? line) ParseResourceSection(string? line,
		StreamReader reader)
	{
		if (string.IsNullOrEmpty(line))
			return (false, default, line);
		var match = ResourceSectionRegex.ResourceNameRegex.Match(line);
		if (!match.Success)
			return (false, default, line);

		var section = new ResourceSection
		{
			Name = match.Groups["Name"].Value
		};

		do
		{
			line = reader.ReadLine();
			if (string.IsNullOrEmpty(line))
				break;

			if (line.StartsWith("filename"))
			{
				match = ResourceSectionRegex.ResourceFileNameRegex.Match(line);
				if (match.Success) section.FileName = match.Groups["FileName"].Value;
			}
			else if (line.StartsWith("type"))
			{
				match = ResourceSectionRegex.ResourceTypeBufferRegex.Match(line);
				if (match.Success && match.Groups["Type"].Value.TryParseWithAlias<ResourceType>(out var type))
					section.Type = type;
			}
			else if (line.StartsWith("format"))
			{
				match = ResourceSectionRegex.ResourceFormatRegex.Match(line);
				if (match.Success && match.Groups["Format"].Value.TryParseWithAlias<ResourceFormat>(out var format))
					section.Format = format;
			}
			else if (line.StartsWith("stride"))
			{
				match = ResourceSectionRegex.ResourceStrideRegex.Match(line);
				if (match.Success && int.TryParse(match.Groups["Stride"].Value, out var stride))
					section.Stride = (byte)stride;
			}
		} while (!line.StartsWith('['));

		return (true, section, line);
	}
}

internal enum ResourceFormat
{
	[EnumAlias("R16_UINT")] [EnumAlias("DXGI_FORMAT_R16_UINT")]
	R16_UINT,

	[EnumAlias("R32_UINT")] [EnumAlias("DXGI_FORMAT_R32_UINT")]
	R32_UINT
}

internal enum ResourceType
{
	None,
	[EnumAlias("Buffer")] Buffer
}