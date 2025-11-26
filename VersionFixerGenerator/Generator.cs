namespace VersionFixerGenerator;

public static class Generator
{
	public static void Run()
	{
		var dumpsPath = Console.ReadLine() ?? "/Users/oleksandr.fedotov/Documents/projects/Cs/ZZMI_tools/VersionFixerGenerator/PlayerCharacterData";

		if (dumpsPath.StartsWith("https://"))
			dumpsPath = Path.Combine(Directory.GetCurrentDirectory(), dumpsPath.Split('/')[^1]);
		if (!Directory.Exists(dumpsPath))
			Directory.CreateDirectory(dumpsPath);

		Directory.SetCurrentDirectory(dumpsPath);
	}
}
