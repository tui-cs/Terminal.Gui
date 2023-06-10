using System;

#nullable enable

namespace Terminal.Gui;

/// <summary>
/// An attribute that can be applied to a property to indicate that it should included in the configuration file.
/// </summary>
/// <example>
/// 	[SerializableConfigurationProperty(Scope = typeof(Configuration.ThemeManager.ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
///	public static LineStyle DefaultBorderStyle {
///	...
/// </example>
[AttributeUsage (AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public class SerializableConfigurationProperty : System.Attribute {
	/// <summary>
	/// Specifies the scope of the property. 
	/// </summary>
	public Type? Scope { get; set; }

	/// <summary>
	/// If <see langword="true"/>, the property will be serialized to the configuration file using only the property name
	/// as the key. If <see langword="false"/>, the property will be serialized to the configuration file using the
	/// property name pre-pended with the classname (e.g. <c>Application.UseSystemConsole</c>).
	/// </summary>
	public bool OmitClassName { get; set; }
}