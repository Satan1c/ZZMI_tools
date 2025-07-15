using System.Text;
using System.Text.RegularExpressions;

namespace SlotsFixApplier.Parser;

internal static partial class TextureOverrideSectionRegex
{
	public static Regex TextureOverrideNameRegex = TextureOverrideNameRegexGenerated();
	public static Regex TextureOverrideHashRegex = TextureOverrideHashRegexGenerated();

	[GeneratedRegex(@"^\[TextureOverride(?<Name>[^\]]+)\]", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex TextureOverrideNameRegexGenerated();
	
	[GeneratedRegex(@"^hash\s*=\s*(?<Hash>[a-z0-9]{8})", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex TextureOverrideHashRegexGenerated();
}

internal struct TextureOverrideSection
{
	public string Name;
	public string Hash;
	public (string name, string value)[] Filters;
	public string[] Resources;

	public string WriteSection()
	{
		var sb = new StringBuilder();
		sb.AppendFormat("[TextureOverride{0}]\n", Name);
		sb.AppendFormat("hash = {0}\n", Hash);
		if (Filters.Length > 0)
		{
			sb.Append(string.Join('\n', Filters.Select(x => $"{x.name} = {x.value}")));
			sb.Append('\n');
		}

		if (Resources.Length > 0)
		{
			sb.Append("resources = ");
			sb.Append(string.Join(", ", Resources));
			sb.Append('\n');
		}

		return sb.ToString();
	}
}