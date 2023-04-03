using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Terminal.Gui.Configuration {

	class DictionaryJsonConverter<T> : JsonConverter<Dictionary<string, T>> {
		public override Dictionary<string, T> Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			var dictionary = new Dictionary<string, T> ();
			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.StartObject) {
					reader.Read ();
					if (reader.TokenType == JsonTokenType.PropertyName) {
						string key = reader.GetString ();
						reader.Read ();
						T value = JsonSerializer.Deserialize<T> (ref reader, options);
						dictionary.Add (key, value);
					}
				} else if (reader.TokenType == JsonTokenType.EndArray)
					break;
			}
			return dictionary;
		}


		public override void Write (Utf8JsonWriter writer, Dictionary<string, T> value, JsonSerializerOptions options)
		{
			writer.WriteStartArray ();
			foreach (var item in value) {
				writer.WriteStartObject ();
				//writer.WriteString (item.Key, item.Key);
				writer.WritePropertyName (item.Key);
				JsonSerializer.Serialize (writer, item.Value, options);
				writer.WriteEndObject ();
			}
			writer.WriteEndArray ();
		}
	}
}
