Directory.EnumerateFiles("./", "*.bin", SearchOption.AllDirectories).AsParallel().Select(x =>
{
	File.Delete(x);
	return 1;
}).ToArray();
Directory.EnumerateFiles("./", "*regex.dat", SearchOption.AllDirectories).AsParallel().Select(x =>
{
	File.Delete(x);
	return 1;
}).ToArray();