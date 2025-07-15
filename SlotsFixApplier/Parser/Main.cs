namespace SlotsFixApplier.Parser;

public static class Main
{
	public static void ParseIni()
	{
		const string og = @"Belle.ini";
		using var file = File.OpenRead(og);
		using var reader = new StreamReader(file, leaveOpen: false);
		string? line = null;

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
				var (success, resource, newLine) = ResourceSection.ParseResourceSection(line, reader);
				line = newLine;
				if (!success)
					continue;
				Console.WriteLine(resource.WriteSection());
			}
			else if (line.StartsWith("[TextureOverride"))
			{
			}
			else if (!reader.EndOfStream)
			{
				line = reader.ReadLine();
			}
		}
	}
}