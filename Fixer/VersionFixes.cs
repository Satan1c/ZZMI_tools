using System.Diagnostics;
using System.Text.Json;

namespace Fixer;

public static class VersionFixes
{
	public static void Run(string[] names)
	{
		foreach (var name in names)
		{
			var path = Path.Combine(Directory.GetCurrentDirectory(), name);

			/*var        usedPath = Path.Combine(path, "used");
			if (File.Exists(usedPath))
			{
				using var used = File.Open(usedPath, FileMode.OpenOrCreate, FileAccess.Read);
				using var usedReader = new StreamReader(used, Encoding.ASCII, leaveOpen: true);
				var       lines      = usedReader.ReadToEnd() ?? string.Empty;
				lines = lines.Trim();
				var timestamp = long.Parse(lines);

				if (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(timestamp) < TimeSpan.FromDays(1)) continue;
			}
			else
			{
				using var s = File.Create(usedPath);
				s.Close();
			}*/

			var files = Directory.GetFiles(path, "*.exe");

			foreach (var file in files)
			{
				var startInfo = new ProcessStartInfo
				{
					FileName = file,
					WorkingDirectory = path,
					CreateNoWindow = false
				};

				Process.Start(startInfo);
			}

			/*using var used2      = File.Open(usedPath, FileMode.Create, FileAccess.Write);
			using var usedWriter = new StreamWriter(used2, Encoding.ASCII, leaveOpen: false);
			usedWriter.Write(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());*/
		}

		Console.ReadLine();
	}

	public static async ValueTask<string[]> DownloadFixers()
	{
		using var httpClient = new HttpClient();
		httpClient.BaseAddress = new Uri("https://gamebanana.com/apiv11/");

		var response = await httpClient.GetStringAsync("Collection/96152/Items?_nPage=1&_nPerpage=4");
		var collectionData =
			(CollectionData)JsonSerializer.Deserialize(response, typeof(CollectionData), FixerContext.Default)!;

		var urls = new LinkedList<(string name, (string url, DateTimeOffset date)[] files)>();

		foreach (var tool in collectionData.records)
		{
			response = await httpClient.GetStringAsync($"Tool/{tool.Id}?_csvProperties=_aFiles");
			var filesData = (FilesData)JsonSerializer.Deserialize(response, typeof(FilesData), FixerContext.Default)!;
			urls.AddLast((tool.Name,
				filesData.Files.Where(x => x.Name.EndsWith(".exe")).Select(x => (x.Url, x.Added)).ToArray()));
		}


		foreach (var data in urls)
		{
			var path = Path.Combine(Directory.GetCurrentDirectory(), data.name);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			var exes = Directory.GetFiles(path, "*.exe").FirstOrDefault() ?? string.Empty;
			byte c = 0;

			foreach (var fileData in data.files)
			{
				var content = await httpClient.GetAsync(fileData.url);

				await using var file = File.Open(Path.Combine(path, $"fix_{c}.exe"), FileMode.Create);
				await content.Content.CopyToAsync(file);
				c++;
			}
		}

		return urls.Select(x => x.name).ToArray();
	}
}