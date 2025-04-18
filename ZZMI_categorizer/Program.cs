using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ZZMI_categorizer;

Searcher.Directories();
Searcher.Grab();
Searcher.Move();

namespace ZZMI_categorizer
{
	internal partial class Searcher
	{
		private static readonly string s_basePath = @".\filtered\";
		private static readonly string s_ddsPath = s_basePath + @"dds\";
		private static readonly string s_txtPath = s_basePath + @"txt\";
		private static readonly string s_jpgPath = s_basePath + @"jpg\";
		private static readonly string s_bufPath = s_basePath + @"buf\";

		private static readonly FrozenDictionary<string, string> s_path = new Dictionary<string, string>
		{
			{ "*.dds", s_ddsPath },
			{ "*.txt", s_txtPath },
			{ "*.buf", s_bufPath },
			{ "*.jpg", s_jpgPath }
		}.ToFrozenDictionary();

		private static readonly FrozenDictionary<string, Regex> s_regex = new Dictionary<string, Regex>
		{
			{ "*.dds", s_other() },
			{ "*.txt", s_txt() },
			{ "*.buf", s_buf() },
			{ "*.jpg", s_other() }
		}.ToFrozenDictionary();

		private static readonly string s_dumpPath = @".\FrameAnalysisDeduped\";

		//private const           string c_dumpPath = @"C:\Users\Satan1c\OneDrive\Games\For games\mods\ZZMI\FrameAnalysisDeduped";

		[GeneratedRegex(@"=([_a-zA-Z0-9]+).*\.txt",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking, -1)]
		private static partial Regex s_txt();

		[GeneratedRegex(@"(.*)\..{3}", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking,
			-1)]
		private static partial Regex s_buf();

		[GeneratedRegex(@"-(.*)\..{3}", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.NonBacktracking,
			-1)]
		private static partial Regex s_other();

		public static void Directories()
		{
			if (!Directory.Exists(s_basePath)) Directory.CreateDirectory(s_basePath);
			if (!Directory.Exists(s_ddsPath)) Directory.CreateDirectory(s_ddsPath);
			if (!Directory.Exists(s_txtPath)) Directory.CreateDirectory(s_txtPath);
			if (!Directory.Exists(s_jpgPath)) Directory.CreateDirectory(s_jpgPath);
			if (!Directory.Exists(s_bufPath)) Directory.CreateDirectory(s_bufPath);
		}

		public static void Grab()
		{
			Parallel.ForEach(Partitioner.Create(s_path), pair => { GrabFiles(pair.Key); });
		}

		public static void GrabFiles(string extension)
		{
			var files = Directory.EnumerateFiles(s_dumpPath, extension);

			Parallel.ForEach(
				Partitioner.Create(files), s =>
				{
					var span = s.AsSpan();
					var name = span[span.LastIndexOf('\\')..];
					var dest = $"{s_path[extension]}{name}";

					try
					{
						File.Move(s, dest, false);
					}
					catch
					{
						File.Delete(s);
					}
				}
			);
		}

		public static void Move()
		{
			Parallel.ForEach(Partitioner.Create(s_path), pair => { MoveFiles(pair.Key); });
		}

		public static void MoveFiles(string extension)
		{
			var files = Directory.EnumerateFiles(s_path[extension]);

			Parallel.ForEach(
				Partitioner.Create(files), s =>
				{
					var name = s[s.AsSpan().LastIndexOf('\\')..];

					var match = s_regex[extension]
						.Match(name)
						.Groups.Values.Skip(1)
						.Select(x => x.Value)
						.ToArray();

					var dir = $"{s_path[extension]}{match[0]}";
					var dest = $"{dir}{name}";

					if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

					try
					{
						File.Move(s, dest, false);
					}
					catch
					{
						File.Delete(s);
					}
				}
			);
		}
	}
}