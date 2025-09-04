using System.Text;
using System.Text.RegularExpressions;

var inis = Directory.EnumerateFiles("*.ini").Select(x => new FileInfo(x)).ToArray();

var linesTasks = new Task<string[]>[inis.Length];
for (var i = 0; i < inis.Length; i++)
{
	linesTasks[i] = GetIniLines(inis[i]);
}
await Task.WhenAll(linesTasks);

var lines = linesTasks.AsParallel().Select(x => x.Result).Select(x => x.AsParallel().Where(y => !string.IsNullOrEmpty(y.Trim())).ToArray()).ToArray();
linesTasks = null;

var resourceNames = new string[inis.Length][];
for (var i = 0; i < inis.Length; i++)
{
	resourceNames[i] = GetResourceNames(lines[i]);
}

var resourceFiles = new FileInfo[resourceNames.Length][];
for (var i = 0; i < resourceNames.Length; i++)
{
	resourceFiles[i] = GetResourceFiles(resourceNames[i]);
}

foreach (var files in resourceFiles)
{
	foreach (var file in files)
	{
		Remap(file);
	}
}

public static partial class Program
{
	private const string BlendHash = "";
	private const string PositionHash = "";
	
    [GeneratedRegex(@"^[ \t]*hash\s*=\s*(?<Hash>\w{8})", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
    private static readonly Regex s_hashRegex = MyRegex();
    
    [GeneratedRegex(@"^[ \t]*vb2\s*=\s*(?:Resource(?<Name>.*))", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex1();
    private static readonly Regex s_blendResourceNameRegex = MyRegex1();
    
    [GeneratedRegex(@"^[ \t]*\[Resource(?<Name>.*)\]", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex2();
    private static readonly Regex s_blendResourceRegex = MyRegex2();
    
    [GeneratedRegex(@"^[ \t]*filename\s*=\s*(?<File>.+)", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex3();
    private static readonly Regex s_fileNameRegex = MyRegex3();

    private static async Task<string[]> GetIniLines(FileInfo file)
    {
	    await using var readStream = file.OpenRead();
	    using var reader = new StreamReader(readStream, leaveOpen: false);
	    var lines = await reader.ReadToEndAsync();
	    return lines.Split(Environment.NewLine);
    }
    
    private static string[] GetResourceNames(string[] lines)
    {
	    var resourceNames = new LinkedList<string>();
	    foreach (var line in lines)
	    {
		    var hashMatch = s_hashRegex.Match(line);
		    if (!hashMatch.Success || hashMatch.Groups["Hash"].Value is not BlendHash or not PositionHash) continue;
		    if (line.TrimStart().StartsWith('[')) continue;
		
		    
		    foreach (var resourceLine in lines)
		    {
			    var bledResourceNameMatch = s_blendResourceNameRegex.Match(resourceLine);
			    if (!bledResourceNameMatch.Success) continue;
			    resourceNames.AddLast(bledResourceNameMatch.Groups["Name"].Value);
		    }
	    }

	    return resourceNames.ToArray();
    }
    
    private static FileInfo[] GetResourceFiles(string[] names)
    {
	    var resourceFiles = new LinkedList<FileInfo>();
	    foreach (var line in names)
	    {
		    var blendResourceMatch = s_blendResourceRegex.Match(line);
		    if (!blendResourceMatch.Success && names.Contains(blendResourceMatch.Groups["Name"].Value)) continue;
		    var fileNameMatch = s_fileNameRegex.Match(line);
		    if (!fileNameMatch.Success) continue;
		    var path = Path.Join(Directory.GetCurrentDirectory(), fileNameMatch.Groups["File"].Value);
		    resourceFiles.AddLast(new FileInfo(path));
	    }

	    return resourceFiles.ToArray();
    }
    
    private static byte[][] ReadBuffer(FileInfo file)
    {
	    using var readStream = file.OpenRead();
	    using var reader = new BinaryReader(readStream, Encoding.ASCII, leaveOpen: false);
	    var buffer = new byte[][file.Length / 32];
	    for (var i = 0; i < buffer.Length; i++)
	    {
		    buffer[i] = reader.ReadBytes(32);
	    }

	    return buffer;
    }

    private static readonly Index[] s_oldIndexes = [];
    private static readonly Index[] s_newIndexes = [];
    private static void Remap(FileInfo file)
	{
		var buffer = ReadBuffer(file).AsSpan();
		var remappedBuffer = new byte[buffer.Length][].AsSpan();
		buffer.CopyTo(remappedBuffer);
		for (var i = 0; i < buffer.Length; i++)
		{
			remappedBuffer[s_oldIndexes[i]] = buffer[s_newIndexes[i]];
		}
		
		var name = file.Name;
		var directory = file.Directory!.FullName;
		file.MoveTo(Path.Join(directory, $"./{name}_remap_backup_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"));

	    using var writeStream = File.Create(Path.Join(directory, name));
	    using var writer = new BinaryWriter(writeStream, Encoding.ASCII, leaveOpen: false);

	    foreach (var t in remappedBuffer)
	    {
		    writer.Write(t);
	    }
	}
}
