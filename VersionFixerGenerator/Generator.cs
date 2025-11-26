using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VersionFixerGenerator;

public static class Generator
{
	public static string SaveTo = null!;

	public static void Run((DateTimeOffset date, string name, string data)[] hashes)
	{
		var parsed = hashes
			.AsParallel()
			.OrderBy(x => x.name).ThenBy(x => x.date)
			.Select(x => (x.date, x.name,
				JsonSerializer.Deserialize<Component[]>(x.data, DumpContext.Default.ComponentArray)!));
		var dict = new Dictionary<string, List<Component[]>>();
		foreach (var (_, name, components) in parsed)
		{
			ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, name, out var exists);

			if (!exists)
				value = [];

			value!.Add(components.OrderBy(x => x.ComponentName).ToArray());
		}

		var changesDict = new Dictionary<string, Dictionary<string, Changes>>();
		foreach (var (key, _) in dict)
		{
			ref var dumpComponents = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _)!;
			if (dumpComponents.Count < 2)
			{
				dumpComponents.Clear();
				dumpComponents.TrimExcess();
				dumpComponents = null!;
				continue;
			}

			dumpComponents.TrimExcess();

			ref var changes = ref CollectionsMarshal.GetValueRefOrAddDefault(changesDict, key, out var exists)!;
			if (!exists)
			{
				changes =
					dumpComponents[0].Select(x =>
					{
						var data = new ChangesData(x);
						return new KeyValuePair<string, Changes>(x.ComponentName,
							new Changes
							{
								Data = data,
								History = []
							}
						);
					}).ToDictionary();
			}

			for (var i = 1; i < dumpComponents.Count; i++)
			{
				var current = dumpComponents[i];
				foreach (var component in current)
				{
					ref var comp =
						ref CollectionsMarshal.GetValueRefOrAddDefault(changes, component.ComponentName, out exists)!;
					if (!exists)
					{
						comp = new Changes
						{
							Data = new ChangesData(component),
							History = []
						};

						continue;
					}

					var og = comp.Data;
					var (from, to) = (new ChangesData(), new ChangesData());
					if (og.Blend != component.Blend)
					{
						from.Blend = og.Blend;
						to.Blend = component.Blend;
					}

					if (og.Position != component.Position)
					{
						from.Position = og.Position;
						to.Position = component.Position;
					}

					if (og.Texcoord != component.Texcoord)
					{
						from.Texcoord = og.Texcoord;
						to.Texcoord = component.Texcoord;
					}

					if (og.Draw != component.Draw)
					{
						from.Draw = og.Draw;
						to.Draw = component.Draw;
					}

					if (og.Ib != component.Ib)
					{
						from.Ib = og.Ib;
						to.Ib = component.Ib;
					}

					if (!from.Equals(to))
					{
						comp.History.Add((from, to));
						comp.Data = new ChangesData(component);
					}
				}
			}
		}

		dict.Clear();

		foreach (var (name, changesMap) in changesDict)
		{
			foreach (var (component, changes) in changesMap)
			{
				if (changes.History.Count < 1)
				{
					changes.History.Clear();
					changes.History.TrimExcess();
					changesMap[component] = null!;
					changesMap.Remove(component);
					continue;
				}

				changes.History.TrimExcess();
			}

			if (changesMap.Count == 0)
			{
				changesDict[name].Clear();
				changesDict[name] = null!;
				changesDict.Remove(name);
			}
		}

		var str = JsonSerializer.Serialize(
			changesDict
				.Select(x => new KeyValuePair<string, Dictionary<string, Changes>>(x.Key.Split('/')[^1], x.Value))
				.OrderBy(x => x.Key).ToDictionary(), DumpContext.Default.DictionaryStringDictionaryStringChanges)!;
		str = str.Replace(@"""Item1"": {", @"""From"": {").Replace(@"""Item2"": {", @"""To"": {");
		File.WriteAllText(SaveTo, str, Encoding.UTF8);
	}
}

internal sealed class Component
{
	[JsonPropertyName("component_name")] public string ComponentName { get; set; } = null!;
	[JsonPropertyName("draw_vb")] public string Draw { get; set; } = null!;
	[JsonPropertyName("position_vb")] public string Position { get; set; } = null!;
	[JsonPropertyName("blend_vb")] public string Blend { get; set; } = null!;
	[JsonPropertyName("texcoord_vb")] public string Texcoord { get; set; } = null!;
	[JsonPropertyName("ib")] public string Ib { get; set; } = null!;
}

#if DEBUG
internal class ChangesListDebugView(ChangesList inner)
{
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public ChangeView[] Items =>
		inner.Select(c => new ChangeView(c.from, c.to)).ToArray();
}

[DebuggerDisplay("{Description,nq}")]
internal class ChangeView(ChangesData from, ChangesData to)
{
	private ChangesData From { get; } = from;
	private ChangesData To { get; } = to;

	public string Description
	{
		get
		{
			var count = 0;
			var sb = new StringBuilder(33);
			
			if (From.Ib != To.Ib)
			{
				sb.AppendFormat("ib: {0} -> {1}, ", From.Ib, To.Ib);
				count++;
			}
			if (From.Blend != To.Blend)
			{
				sb.AppendFormat("blend: {0} -> {1}, ", From.Blend, To.Blend);
				count++;
			}
			if (From.Position != To.Position)
			{
				sb.AppendFormat("position: {0} -> {1}, ", From.Position, To.Position);
				count++;
			}
			if (From.Texcoord != To.Texcoord)
			{
				sb.AppendFormat("texcoord: {0} -> {1}, ", From.Texcoord, To.Texcoord);
				count++;
			}
			if (From.Draw != To.Draw)
			{
				sb.AppendFormat("draw: {0} -> {1}, ", From.Draw, To.Draw);
				count++;
			}
			
			return count == 5 ? "all" : (count > 3 ? "most" : sb.ToString());
		}
	}
}

[DebuggerTypeProxy(typeof(ChangesListDebugView))]
internal class ChangesList : List<(ChangesData from, ChangesData to)> { }

[DebuggerDisplay("{DebuggerDisplay,nq}")]
#endif
internal sealed class Changes
{
	public ChangesData Data { get; set; } = null!;
	
	
#if  DEBUG
	public ChangesList History { get; set; } = null!;
	private string DebuggerDisplay => $"{History.Count}";
#else
	public List<(ChangesData? from, ChangesData? to)> History { get; set; } = null!;
#endif
}

internal sealed class ChangesData
{
	public string Draw { get; set; } = null;
	public string Position { get; set; } = null;
	public string Blend { get; set; } = null;
	public string Texcoord { get; set; } = null;
	public string Ib { get; set; } = null;

	public ChangesData(){}
	public ChangesData(Component component)
	{
		Draw = component.Draw;
		Position = component.Position;
		Blend = component.Blend;
		Texcoord = component.Texcoord;
		Ib = component.Ib;
	}

	public bool Equals(ChangesData other)
	{
		if (ReferenceEquals(this, other)) return true;
		
		if (Draw != other.Draw) return false;
		if (Position != other.Position) return false;
		if (Blend != other.Blend) return false;
		if (Texcoord != other.Texcoord) return false;
		if (Ib != other.Ib) return false;
		
		return true;
	}
}

[JsonSerializable(typeof(Component))]
[JsonSerializable(typeof(Component[]))]
[JsonSerializable(typeof(Changes))]
[JsonSerializable(typeof(ChangesData))]
[JsonSerializable(typeof(ChangesData[]))]
[JsonSerializable(typeof(Dictionary<string, Dictionary<string, Changes>>))]
[JsonSourceGenerationOptions(WriteIndented = true, IndentCharacter = '\t', IndentSize = 1, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class DumpContext : JsonSerializerContext;

