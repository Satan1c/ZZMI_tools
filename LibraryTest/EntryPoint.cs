using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibraryTest;

public static class EntryPoint
{
	[UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = [typeof(CallConvStdcall)])]
	public static bool DllMain(IntPtr hModule, uint ul_reason_for_call, IntPtr lpReserved)
	{
		switch (ul_reason_for_call)
		{
			case 1:
				var p = Process.GetCurrentProcess();
				InitializeHooks(p.ProcessName);
				break;
		}

		return true;
	}

	private static void InitializeHooks(string data = "library initialized")
	{
		using var file = File.Open(@"C:\Users\Satan1c\OneDrive\Games\For games\mods\result.txt", FileMode.Create,
			FileAccess.Write);
		using var writer = new StreamWriter(file);
		writer.WriteLine(data);
	}
}