#define GENERATOR
//#define GH_GRABBER
//#define LOCAL_GRABBER

using VersionFixerGenerator;

var hashesPath = Console.ReadLine();
if (string.IsNullOrEmpty(hashesPath))
	hashesPath = Directory.GetCurrentDirectory();

hashesPath = "/Users/oleksandr.fedotov/Documents/projects/Cs/ZZMI_tools/VersionFixerGenerator/PlayerCharacterData/";

if (!Directory.Exists(hashesPath))
	Directory.CreateDirectory(hashesPath);

Directory.SetCurrentDirectory(hashesPath);

Generator.SaveTo = Path.Combine(hashesPath, "PlayerCharacterData.json");

#if GENERATOR
#if GH_GRABBER
Generator.Run(await GithubGrabber.Run());
#elif LOCAL_GRABBER
Generator.Run(await GithubGrabber.Run());
#endif
#endif



Console.WriteLine("Done");
