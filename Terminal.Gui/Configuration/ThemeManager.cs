using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

#nullable enable

namespace Terminal.Gui; 

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
public class ThemeManager : IDictionary<string, ThemeScope> {
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
		get => ConfigurationManager.Settings? ["Themes"]?.PropertyValue as Dictionary<string, ThemeScope>; // themes ?? new Dictionary<string, ThemeScope> ();
		set {
			//if (themes == null || value == null) {
			//	themes = value;
			//} else {
			//	themes = (Dictionary<string, ThemeScope>)DeepMemberwiseCopy (value!, themes!)!;
			//}
			ConfigurationManager.Settings! ["Themes"].PropertyValue = value;
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
#pragma warning disable 1591

	public ICollection<string> Keys => ((IDictionary<string, ThemeScope>)Themes!).Keys;
	public ICollection<ThemeScope> Values => ((IDictionary<string, ThemeScope>)Themes!).Values;
	public int Count => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Count;
	public bool IsReadOnly => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).IsReadOnly;
	public ThemeScope this [string key] { get => ((IDictionary<string, ThemeScope>)Themes!) [key]; set => ((IDictionary<string, ThemeScope>)Themes!) [key] = value; }
	public void Add (string key, ThemeScope value)
	{
		((IDictionary<string, ThemeScope>)Themes!).Add (key, value);
	}
	public bool ContainsKey (string key)
	{
		return ((IDictionary<string, ThemeScope>)Themes!).ContainsKey (key);
	}
	public bool Remove (string key)
	{
		return ((IDictionary<string, ThemeScope>)Themes!).Remove (key);
	}
	public bool TryGetValue (string key, out ThemeScope value)
	{
		return ((IDictionary<string, ThemeScope>)Themes!).TryGetValue (key, out value!);
	}
	public void Add (KeyValuePair<string, ThemeScope> item)
	{
		((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Add (item);
	}
	public void Clear ()
	{
		((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Clear ();
	}
	public bool Contains (KeyValuePair<string, ThemeScope> item)
	{
		return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Contains (item);
	}
	public void CopyTo (KeyValuePair<string, ThemeScope> [] array, int arrayIndex)
	{
		((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).CopyTo (array, arrayIndex);
	}
	public bool Remove (KeyValuePair<string, ThemeScope> item)
	{
		return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Remove (item);
	}
	public IEnumerator<KeyValuePair<string, ThemeScope>> GetEnumerator ()
	{
		return ((IEnumerable<KeyValuePair<string, ThemeScope>>)Themes!).GetEnumerator ();
	}

	IEnumerator IEnumerable.GetEnumerator ()
	{
		return ((IEnumerable)Themes!).GetEnumerator ();
	}
#pragma warning restore 1591

	#endregion
}