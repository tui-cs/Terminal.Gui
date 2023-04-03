using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using static Terminal.Gui.Configuration.ConfigurationManager;

#nullable enable

namespace Terminal.Gui.Configuration {

	public static partial class ConfigurationManager {
		/// <summary>
		/// The root object for a Theme. A Theme is a set of settings that are applied to the running <see cref="Application"/>
		/// as a group.
		/// </summary>
		/// <remarks>
		/// <para>
		/// </para>
		/// </remarks>
		/// <example><code>
		/// 	"Default": {
		/// 		"ColorSchemes": [
		/// 		{
		/// 		"TopLevel": {
		/// 		"Normal": {
		/// 			"Foreground": "BrightGreen",
		/// 			"Background": "Black"
		/// 		},
		/// 		"Focus": {
		/// 		"Foreground": "White",
		/// 			"Background": "Cyan"
		/// 
		/// 		},
		/// 		"HotNormal": {
		/// 			"Foreground": "Brown",
		/// 			"Background": "Black"
		/// 
		/// 		},
		/// 		"HotFocus": {
		/// 			"Foreground": "Blue",
		/// 			"Background": "Cyan"
		/// 		},
		/// 		"Disabled": {
		/// 			"Foreground": "DarkGray",
		/// 			"Background": "Black"
		/// 
		/// 		}
		/// 	}
		/// </code></example> 
		[JsonConverter (typeof (ScopeJsonConverter<ThemeScope>))]
		public class ThemeScope : Scope<ThemeScope> {

			/// <inheritdoc/>
			internal override bool Apply ()
			{
				var ret = base.Apply ();
				Application.Driver?.InitalizeColorSchemes ();
				return ret;
			}
		}

		/// <summary>
		/// Contains a dictionary of the <see cref="ThemeManager.Theme"/>s for a Terminal.Gui application.
		/// </summary>
		/// <remarks>
		/// <para>
		/// A Theme is a collection of settings that are named. The default theme is named "Default".
		/// </para>
		/// <para>
		/// The <see cref="ThemeManager.Theme"/> property is used to detemrine the currently active theme. 
		/// </para>
		/// </remarks>
		/// <para>
		/// <see cref="ThemeManager"/> is a singleton class. It is created when the first <see cref="ThemeManager"/> property is accessed.
		/// Accessing <see cref="ThemeManager.Instance"/> is the same as accessing <see cref="ConfigurationManager.Themes"/>.
		/// </para>
		/// <example><code>
		/// 	"Themes": [
		/// 	{
		/// 		"Default": {
		/// 			"ColorSchemes": [
		/// 			{
		/// 			"TopLevel": {
		/// 			"Normal": {
		/// 				"Foreground": "BrightGreen",
		/// 				"Background": "Black"
		/// 			},
		/// 			"Focus": {
		/// 			"Foreground": "White",
		/// 				"Background": "Cyan"
		/// 
		/// 			},
		/// 			"HotNormal": {
		/// 				"Foreground": "Brown",
		/// 				"Background": "Black"
		/// 
		/// 			},
		/// 			"HotFocus": {
		/// 				"Foreground": "Blue",
		/// 				"Background": "Cyan"
		/// 			},
		/// 			"Disabled": {
		/// 				"Foreground": "DarkGray",
		/// 				"Background": "Black"
		/// 
		/// 			}
		/// 		}
		/// 	}
		/// </code></example> 
		public partial class ThemeManager : IDictionary<string, ThemeScope> {
			private static readonly ThemeManager _instance = new ThemeManager ();
			static ThemeManager () { } // Make sure it's truly lazy
			private ThemeManager () { } // Prevent instantiation outside

			/// <summary>
			/// Class is a singleton...
			/// </summary>
			public static ThemeManager Instance { get { return _instance; } }

			private static string _theme = string.Empty;

			/// <summary>
			/// The currently selected theme. This is the internal version; see <see cref="Theme"/>.
			/// </summary>
			[JsonInclude, SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true), JsonPropertyName ("Theme")]
			internal static string SelectedTheme {
				get => _theme;
				set {
					var oldTheme = _theme;
					_theme = value;
					if (oldTheme != _theme &&
						ConfigurationManager.Settings! ["Themes"]?.PropertyValue is Dictionary<string, ThemeScope> themes &&
						themes.ContainsKey (_theme)) {
						ConfigurationManager.Settings! ["Theme"].PropertyValue = _theme;
						Instance.OnThemeChanged (oldTheme);
					}
				}
			}

			/// <summary>
			/// Gets or sets the currently selected theme. The value is persisted to the "Theme"
			/// property.
			/// </summary>
			[JsonIgnore]
			public string Theme {
				get => ThemeManager.SelectedTheme;
				set {
					ThemeManager.SelectedTheme = value;
				}
			}

			/// <summary>
			/// Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.
			/// </summary>
			internal void OnThemeChanged (string theme)
			{
				Debug.WriteLine ($"Themes.OnThemeChanged({theme}) -> {Theme}");
				ThemeChanged?.Invoke (this, new ThemeManagerEventArgs (theme));
			}

			/// <summary>
			/// Event fired he selected theme has changed.
			/// application.
			/// </summary>
			public event EventHandler<ThemeManagerEventArgs>? ThemeChanged;

			/// <summary>
			/// Holds the <see cref="ThemeScope"/> definitions. 
			/// </summary>
			[JsonInclude, JsonConverter (typeof (DictionaryJsonConverter<ThemeScope>))]
			[SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
			public static Dictionary<string, ThemeScope>? Themes {
				get => Settings? ["Themes"]?.PropertyValue as Dictionary<string, ThemeScope>; // themes ?? new Dictionary<string, ThemeScope> ();
				set {
					//if (themes == null || value == null) {
					//	themes = value;
					//} else {
					//	themes = (Dictionary<string, ThemeScope>)DeepMemberwiseCopy (value!, themes!)!;
					//}
					Settings! ["Themes"].PropertyValue = value;
				}
			}

			internal static void Reset ()
			{
				Debug.WriteLine ($"Themes.Reset()");

				Themes?.Clear ();
				SelectedTheme = string.Empty;
			}

			internal static void GetHardCodedDefaults ()
			{
				Debug.WriteLine ($"Themes.GetHardCodedDefaults()");
				var theme = new ThemeScope ();
				theme.RetrieveValues ();

				Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase) { { "Default", theme } };
				SelectedTheme = "Default";
			}

			#region IDictionary
			/// <inheritdoc/>
			public ICollection<string> Keys => ((IDictionary<string, ThemeScope>)Themes!).Keys;
			/// <inheritdoc/>
			public ICollection<ThemeScope> Values => ((IDictionary<string, ThemeScope>)Themes!).Values;
			/// <inheritdoc/>
			public int Count => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Count;
			/// <inheritdoc/>
			public bool IsReadOnly => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).IsReadOnly;
			/// <inheritdoc/>
			public ThemeScope this [string key] { get => ((IDictionary<string, ThemeScope>)Themes!) [key]; set => ((IDictionary<string, ThemeScope>)Themes!) [key] = value; }
			/// <inheritdoc/>
			public void Add (string key, ThemeScope value)
			{
				((IDictionary<string, ThemeScope>)Themes!).Add (key, value);
			}
			/// <inheritdoc/>
			public bool ContainsKey (string key)
			{
				return ((IDictionary<string, ThemeScope>)Themes!).ContainsKey (key);
			}
			/// <inheritdoc/>
			public bool Remove (string key)
			{
				return ((IDictionary<string, ThemeScope>)Themes!).Remove (key);
			}
			/// <inheritdoc/>
			public bool TryGetValue (string key, out ThemeScope value)
			{
				return ((IDictionary<string, ThemeScope>)Themes!).TryGetValue (key, out value!);
			}
			/// <inheritdoc/>
			public void Add (KeyValuePair<string, ThemeScope> item)
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Add (item);
			}
			/// <inheritdoc/>
			public void Clear ()
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Clear ();
			}
			/// <inheritdoc/>
			public bool Contains (KeyValuePair<string, ThemeScope> item)
			{
				return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Contains (item);
			}
			/// <inheritdoc/>
			public void CopyTo (KeyValuePair<string, ThemeScope> [] array, int arrayIndex)
			{
				((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).CopyTo (array, arrayIndex);
			}
			/// <inheritdoc/>
			public bool Remove (KeyValuePair<string, ThemeScope> item)
			{
				return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Remove (item);
			}
			/// <inheritdoc/>
			public IEnumerator<KeyValuePair<string, ThemeScope>> GetEnumerator ()
			{
				return ((IEnumerable<KeyValuePair<string, ThemeScope>>)Themes!).GetEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return ((IEnumerable)Themes!).GetEnumerator ();
			}
			#endregion
		}
	}
}