using ZZMI_collector;

if (!Directory.Exists("collected/")) Directory.CreateDirectory("collected/");

//var ibs        = "Nekomata Hair:da11fd85,7ff014ed,ae72fe8c Body:26a487ff,b4e96ddf,50b9c76c LegsAndSwords:74688145,9225e606,123605bc";
var elements = (args is null or { Length: 0 } ? null : args) ?? Console.ReadLine()?.Split(' ') ?? [];

//string[] elements = ["Nekomiya", "aed3d8bd", "37d3154d"];
var target = elements[0];

Processor.Start(target, elements);

Console.ReadLine();