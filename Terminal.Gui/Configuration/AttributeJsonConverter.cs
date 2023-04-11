using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui;

namespace Terminal.Gui {
	/// <summary>
	/// Json converter fro the <see cref="Attribute"/> class.
	/// </summary>
	public class AttributeJsonConverter : JsonConverter<Attribute> {
		private static AttributeJsonConverter instance;

		/// <summary>
		/// 
		/// </summary>
		public static AttributeJsonConverter Instance {
			get {
				if (instance == null) {
					instance = new AttributeJsonConverter ();
				}

				return instance;
			}
		}

		/// <inheritdoc/>
		public override Attribute Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject) {
				throw new JsonException ($"Unexpected StartObject token when parsing Attribute: {reader.TokenType}.");
			}

			Attribute attribute = new Attribute ();
			Color foreground =  (Color)(-1);
			Color background =  (Color)(-1);
			while (reader.Read ()) {
				if (reader.TokenType == JsonTokenType.EndObject) {
					if (foreground ==  (Color)(-1) || background ==  (Color)(-1)) {
						throw new JsonException ($"Both Foreground and Background colors must be provided.");
					}
					return attribute;
				}

				if (reader.TokenType != JsonTokenType.PropertyName) {
					throw new JsonException ($"Unexpected token when parsing Attribute: {reader.TokenType}.");
				}

				string propertyName = reader.GetString ();
				reader.Read ();
				string color = $"\"{reader.GetString ()}\"";

				switch (propertyName.ToLower ()) {
				case "foreground":
					foreground = JsonSerializer.Deserialize<Color> (color, options);
					break;
				case "background":
					background = JsonSerializer.Deserialize<Color> (color, options);
					break;
				//case "Bright":
				//	attribute.Bright = reader.GetBoolean ();
				//	break;
				//case "Underline":
				//	attribute.Underline = reader.GetBoolean ();
				//	break;
				//case "Reverse":
				//	attribute.Reverse = reader.GetBoolean ();
				//	break;
				default:
					throw new JsonException ($"Unknown Attribute property {propertyName}.");
				}

				attribute = new Attribute (foreground, background);
			}
			throw new JsonException ();
		}

		/// <inheritdoc/>
		public override void Write (Utf8JsonWriter writer, Attribute value, JsonSerializerOptions options)
		{
			writer.WriteStartObject ();
			writer.WritePropertyName ("Foreground");
			ColorJsonConverter.Instance.Write (writer, value.Foreground, options);
			writer.WritePropertyName ("Background");
			ColorJsonConverter.Instance.Write (writer, value.Background, options);
			writer.WriteEndObject ();
		}
	}
}

