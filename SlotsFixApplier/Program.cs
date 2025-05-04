using SlotsFixApplier;

const string modFolder = @"C:\Users\Satan1c\OneDrive\Games\For games\mods\XXMI Launcher\ZZMI\Mods\Test\Pulchra";

IAppliers[] appliers =
[
	new Slotted()
];

var applier = appliers[0];

foreach (var ini in Directory.EnumerateFiles(modFolder, "*.ini", SearchOption.AllDirectories))
{
	var fileInfo = new FileInfo(ini);
	var name = fileInfo.Name;
	if (name.Trim().StartsWith("DISABLED")) continue;

	var path = Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty;

	var resultPath = Path.Combine(path, "result.ini");
	applier.Execute(ini, resultPath);

	File.Move(ini, Path.Combine(path, "DISABLED SlotFixApplied " + name));
	File.Move(resultPath, Path.Combine(path, name));
}