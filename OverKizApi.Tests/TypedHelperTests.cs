// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class TypedHelperTests
	{
	[TestCase (DataType.Integer)]
	[TestCase (DataType.Float)]
	[TestCase (DataType.Boolean)]
	[TestCase (DataType.String)]
	[TestCase (DataType.JsonArray)]
	[TestCase (DataType.JsonObject)]
	public void TypedGetters_PreserveValuesAndFallbackOrder (DataType type)
		{
		object value = type switch
			{
			DataType.Integer => 0,
			DataType.Float => 0d,
			DataType.Boolean => false,
			DataType.String => "",
			DataType.JsonArray => new List<object?> (),
			_ => new Dictionary<string, object?> ()
			};
		var states = new States ([new State { Name = "null", Type = type }, new State { Name = "value", Type = type, Value = value }]);
		Assert.That (Get (states, "value", type), Is.EqualTo (value));
		Assert.That (First (states, ["missing", "null", "value"], type), Is.EqualTo (value));
		Assert.That (Get (states, "missing", type), Is.Null);
		Assert.That (Get (states, "null", type), Is.Null);
		Assert.That (First (states, ["missing", "null"], type), Is.Null);
		Assert.That (First (states, [], type), Is.Null);
		}

	[TestCase (DataType.Integer)]
	[TestCase (DataType.Float)]
	[TestCase (DataType.Boolean)]
	[TestCase (DataType.String)]
	[TestCase (DataType.JsonArray)]
	[TestCase (DataType.JsonObject)]
	public void WrongType_IsNotSilentlySkipped (DataType type)
		{
		var states = new States ([new State { Name = "wrong", Type = type == DataType.String ? DataType.Integer : DataType.String, Value = "wrong" }]);
		Assert.That (() => Get (states, "wrong", type), Throws.TypeOf<InvalidCastException> ());
		Assert.That (() => First (states, ["missing", "wrong", "later"], type), Throws.TypeOf<InvalidCastException> ());
		}

	[Test]
	public void IntegerPromotionAndOrderedFallback_AreExplicit ()
		{
		var states = new States ([new State { Name = "first", Type = DataType.Integer, Value = 12L }, new State { Name = "second", Type = DataType.Integer, Value = 34L }]);
		Assert.That (states.GetValueAsFloat ("first"), Is.EqualTo (12d));
		Assert.That (states.FirstValueAsFloat (["second", "first"]), Is.EqualTo (34d));
		Assert.That (states.FirstValueAsInt (["first", "second"]), Is.EqualTo (12));
		Assert.That (() => states.GetValueAsInt (null!), Throws.ArgumentNullException);
		Assert.That (() => states.FirstValueAsInt (null!), Throws.ArgumentNullException);
		}

	[Test]
	public void StructuredStates_UseClrCollectionsWithoutComputedWireFields ()
		{
		const string json = """[{"name":"array","type":10,"value":[1,{"nested":false}]},{"name":"object","type":11,"value":{"count":2,"items":["a"]}}]""";
		var states = JsonSerializer.Deserialize<States> (json)!;
		Assert.That (states.GetValueAsList ("array")![0], Is.EqualTo (1L));
		Assert.That (states.GetValueAsDict ("object")!["count"], Is.EqualTo (2L));
		Assert.That (states.FirstValueAsList (["absent", "array"]), Is.SameAs (states["array"]!.Value));
		Assert.That (states.FirstValueAsDict (["absent", "object"]), Is.SameAs (states["object"]!.Value));
		using var document = JsonDocument.Parse (JsonSerializer.Serialize (states));
		Assert.That (document.RootElement[0].EnumerateObject ().Select (p => p.Name), Is.EquivalentTo (new[] { "name", "type", "value" }));
		Assert.That (JsonSerializer.Deserialize<States> (JsonSerializer.Serialize (states))!.GetValueAsList ("array")![0], Is.EqualTo (1L));
		Assert.That (new State { Type = DataType.None }.ValueAsList, Is.Null);
		Assert.That (new State { Type = DataType.JsonObject }.ValueAsDict, Is.Null);
		Assert.That (() => new State { Type = DataType.JsonObject, Value = "wrong" }.ValueAsDict, Throws.TypeOf<InvalidCastException> ());
		Assert.That (() => new State { Type = DataType.JsonArray, Value = "wrong" }.ValueAsList, Throws.TypeOf<InvalidCastException> ());
		}

	[TestCase ("{}")]
	[TestCase ("{\"attributes\":[{\"name\":\"core:SupportedAliases\",\"type\":11,\"value\":{}}]}")]
	public void MissingOrNonListAliases_AreEmpty (string json)
		{
		var device = JsonSerializer.Deserialize<Device> (json)!;
		Assert.That (device.GetSupportedAliases (), Is.Empty);
		Assert.That (device.GetMostFeaturedAliases (), Is.Empty);
		}

	[Test]
	public void Aliases_PreserveSlotsAndResolveMostFeaturedWithStableTies ()
		{
		var device = DeviceWithAliases ("""[{"id":"a","type":"favorite1"},{"id":2,"type":"favorite1","features":["position","tilt"]},{"id":"c","type":"favorite1","features":["position","tilt"]},{"id":"d","type":"vendor:new","features":[]}]""");
		var all = device.GetSupportedAliases ();
		Assert.That (all.Select (a => a.Id), Is.EqualTo (new[] { "a", "2", "c", "d" }));
		Assert.That (all[0].Features, Is.Empty);
		var selected = device.GetMostFeaturedAliases ();
		Assert.That (selected.Count, Is.EqualTo (2));
		Assert.That (selected["favorite1"].Id, Is.EqualTo ("2"));
		Assert.That (selected["vendor:new"].Id, Is.EqualTo ("d"));
		var command = new Command { Name = "goToAlias", Parameters = [selected["favorite1"].Id] };
		using var serializedCommand = JsonDocument.Parse (JsonSerializer.Serialize (command));
		Assert.That (serializedCommand.RootElement.GetProperty ("parameters")[0].GetString (), Is.EqualTo ("2"));
		}

	[Test]
	public void MalformedAliasEntries_AreSkippedWithoutLosingValidSlots ()
		{
		var device = DeviceWithAliases ("""[null,7,{}, {"id":true,"type":"x"},{"id":1.5,"type":"x"},{"id":"","type":"x"},{"id":"x","type":9},{"id":"x","type":"x","features":null},{"id":"x","type":"x","features":[1]},{"id":"good","type":"unknown","features":["vendor-feature"]}]""");
		Assert.That (device.GetSupportedAliases ().Single ().Id, Is.EqualTo ("good"));
		Assert.That (device.GetSupportedAliases ().Single ().Features, Is.EqualTo (new[] { "vendor-feature" }));
		}

	[Test]
	public void AliasSerialization_UsesAttributedWireNames ()
		{
		var alias = JsonSerializer.Deserialize<SupportedAlias> ("""{"id":"preset","type":"favorite1","features":["position"]}""")!;
		Assert.That (alias.Id, Is.EqualTo ("preset"));
		Assert.That (alias.Type, Is.EqualTo ("favorite1"));
		using var json = JsonDocument.Parse (JsonSerializer.Serialize (alias));
		Assert.That (json.RootElement.EnumerateObject ().Select (p => p.Name), Is.EquivalentTo (new[] { "id", "type", "features" }));
		Assert.That (json.RootElement.GetProperty ("features")[0].GetString (), Is.EqualTo ("position"));
		}

	private static Device DeviceWithAliases (string aliases)
		=> JsonSerializer.Deserialize<Device> ("{\"attributes\":[{\"name\":\"core:SupportedAliases\",\"type\":10,\"value\":" + aliases + "}]}")!;

	private static object? Get (States states, string name, DataType type) => type switch
		{
		DataType.Integer => states.GetValueAsInt (name),
		DataType.Float => states.GetValueAsFloat (name),
		DataType.Boolean => states.GetValueAsBool (name),
		DataType.String => states.GetValueAsStr (name),
		DataType.JsonArray => states.GetValueAsList (name),
		_ => states.GetValueAsDict (name)
		};
	private static object? First (States states, IEnumerable<string> names, DataType type) => type switch
		{
		DataType.Integer => states.FirstValueAsInt (names),
		DataType.Float => states.FirstValueAsFloat (names),
		DataType.Boolean => states.FirstValueAsBool (names),
		DataType.String => states.FirstValueAsStr (names),
		DataType.JsonArray => states.FirstValueAsList (names),
		_ => states.FirstValueAsDict (names)
		};
	}