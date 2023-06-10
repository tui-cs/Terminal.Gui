using System.Text.Json.Serialization;

#nullable enable

namespace Terminal.Gui; 

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