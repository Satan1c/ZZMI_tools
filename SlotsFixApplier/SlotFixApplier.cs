using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

const string og = @"Belle.ini";
using var file = File.OpenRead(og);
using var reader = new StreamReader(file, leaveOpen: false);
string? line = null;

ParseIni();
return;

void ParseIni()
{
	while (!reader.EndOfStream)
	{
		if (string.IsNullOrEmpty(line))
		{
			line = reader.ReadLine();
			if (string.IsNullOrEmpty(line))
				return;
		}

		if (line.StartsWith("[Resource"))
		{
			var (success, resource) = ParseResourceSection();
			if (!success)
				continue;
			Console.WriteLine(resource.WriteSection());
		}
		else if (!reader.EndOfStream)
		{
			line = reader.ReadLine();
		}
	}
}

(bool success, ResourceSection section) ParseResourceSection()
{
	var match = ResourceSectionRegex.ResourceNameRegex.Match(line);
	if (!match.Success)
		return (false, default);

	var section = new ResourceSection
	{
		Name = match.Groups["Name"].Value,
	};

	do
	{
		line = reader.ReadLine();
		if (string.IsNullOrEmpty(line))
			break;

		if (line.StartsWith("filename"))
		{
			match = ResourceSectionRegex.ResourceFileNameRegex.Match(line);
			if (match.Success)
			{
				section.FileName = match.Groups["FileName"].Value;
			}
		}
		else if (line.StartsWith("type"))
		{
			match = ResourceSectionRegex.ResourceTypeBufferRegex.Match(line);
			if (match.Success && match.Groups["Type"].Value.TryParseWithAlias<ResourceType>(out var type))
			{
				section.Type = type;
			}
		}
		else if (line.StartsWith("format"))
		{
			match = ResourceSectionRegex.ResourceFormatRegex.Match(line);
			if (match.Success && match.Groups["Format"].Value.TryParseWithAlias<ResourceFormat>(out var format))
			{
				section.Format = format;
			}
		}
		else if (line.StartsWith("stride"))
		{
			match = ResourceSectionRegex.ResourceStrideRegex.Match(line);
			if (match.Success && int.TryParse(match.Groups["Stride"].Value, out var stride))
			{
				section.Stride = (byte)stride;
			}
		}
	} while (!line.StartsWith('['));

	return (true, section);
}

internal static partial class ResourceSectionRegex
{
	[GeneratedRegex(@"^\[Resource(?<Name>[^\]]+(?:Blend|Position|Texcoord|IB|Diffuse(?:Map)?|(?:Normal|Light|Material|HighLight)Map))\]", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceNameRegexGenerated();
	public static Regex ResourceNameRegex = ResourceNameRegexGenerated();
	
	[GeneratedRegex(@"^filename\s*=\s*(?<FileName>.+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceFileNameRegexGenerated();
	public static Regex ResourceFileNameRegex = ResourceFileNameRegexGenerated();
	
	[GeneratedRegex(@"^format\s*=\s*DXGI_FORMAT_(?<Format>\w+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceFormatRegexGenerated();
	public static Regex ResourceFormatRegex = ResourceFormatRegexGenerated();

	[GeneratedRegex(@"^stride\s*=\s*(?<Stride>\d+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceStrideGenerated();
	public static Regex ResourceStrideRegex = ResourceStrideGenerated();
	
	[GeneratedRegex(@"^type\s*=\s*(?<Type>\w+)", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex ResourceTypeBufferRegexGenerated();
	public static Regex ResourceTypeBufferRegex = ResourceTypeBufferRegexGenerated();
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
}

internal enum ResourceFormat
{
	[EnumAlias("R16_UINT")]
	[EnumAlias("DXGI_FORMAT_R16_UINT")]
	R16_UINT,
	
	[EnumAlias("R32_UINT")]
	[EnumAlias("DXGI_FORMAT_R32_UINT")]
	R32_UINT
}
internal enum ResourceType
{
	None,
	[EnumAlias("Buffer")]
	Buffer
}

internal static class Extensions
{
	public static string ResourceFormatToString(this ResourceFormat format)
	{
		return format switch
		{
			ResourceFormat.R16_UINT => nameof(ResourceFormat.R16_UINT),
			ResourceFormat.R32_UINT => nameof(ResourceFormat.R32_UINT),
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
		};
	}
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class EnumAliasAttribute : Attribute
{
	public string Alias { get; }

	public EnumAliasAttribute(string alias) => Alias = alias;
}
public static class EnumExtensions
{
	public static bool TryParseWithAlias<T>(this string input, out T? enumValue) where T : Enum
	{
		var type = typeof(T);
		
		foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			var attributes = field.GetCustomAttributes<EnumAliasAttribute>(false);
			if (!attributes.Any(attr => string.Equals(attr.Alias, input, StringComparison.OrdinalIgnoreCase))) continue;

			var value = (T)field.GetValue(null);
			if (value is null)
			{
				continue;
			}

			enumValue = value;
			return true;
		}

		enumValue = default;
		return false;
	}
}