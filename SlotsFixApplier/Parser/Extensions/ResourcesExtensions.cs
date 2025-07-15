namespace SlotsFixApplier.Parser.Extensions;

internal static class ResourcesExtensions
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