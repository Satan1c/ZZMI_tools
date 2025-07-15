using System.Reflection;

namespace SlotsFixApplier.Parser.Extensions;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class EnumAliasAttribute : Attribute
{
	public EnumAliasAttribute(string alias)
	{
		Alias = alias;
	}

	public string Alias { get; }
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
			if (value is null) continue;

			enumValue = value;
			return true;
		}

		enumValue = default;
		return false;
	}
}