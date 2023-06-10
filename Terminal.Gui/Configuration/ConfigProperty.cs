using System;
using System.Reflection;
using System.Text.Json.Serialization;

#nullable enable

namespace Terminal.Gui;

/// <summary>
/// Holds a property's value and the <see cref="PropertyInfo"/> that allows <see cref="ConfigurationManager"/> 
/// to get and set the property's value.
/// </summary>
/// <remarks>
/// Configuration properties must be <see langword="public"/> and <see langword="static"/> 
/// and have the <see cref="SerializableConfigurationProperty"/>
/// attribute. If the type of the property requires specialized JSON serialization, 
/// a <see cref="JsonConverter"/> must be provided using 
/// the <see cref="JsonConverterAttribute"/> attribute.
/// </remarks>
public class ConfigProperty {
	private object? propertyValue;

	/// <summary>
	/// Describes the property.
	/// </summary>
	public PropertyInfo? PropertyInfo { get; set; }

	/// <summary>
	/// Helper to get either the Json property named (specified by [JsonPropertyName(name)]
	/// or the actual property name.
	/// </summary>
	/// <param name="pi"></param>
	/// <returns></returns>
	public static string GetJsonPropertyName (PropertyInfo pi)
	{
		var jpna = pi.GetCustomAttribute (typeof (JsonPropertyNameAttribute)) as JsonPropertyNameAttribute;
		return jpna?.Name ?? pi.Name;
	}

	/// <summary>
	/// Holds the property's value as it was either read from the class's implementation or from a config file. 
	/// If the property has not been set (e.g. because no configuration file specified a value), 
	/// this will be <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// On <see langword="set"/>, performs a sparse-copy of the new value to the existing value (only copies elements of 
	/// the object that are non-null).
	/// </remarks>
	public object? PropertyValue {
		get => propertyValue;
		set {
			propertyValue = value;
		}
	}

	internal object? UpdateValueFrom (object source)
	{
		if (source == null) {
			return PropertyValue;
		}

		var ut = Nullable.GetUnderlyingType (PropertyInfo!.PropertyType);
		if (source.GetType () != PropertyInfo!.PropertyType && (ut != null && source.GetType () != ut)) {
			throw new ArgumentException ($"The source object ({PropertyInfo!.DeclaringType}.{PropertyInfo!.Name}) is not of type {PropertyInfo!.PropertyType}.");
		}
		if (PropertyValue != null && source != null) {
			PropertyValue = ConfigurationManager.DeepMemberwiseCopy (source, PropertyValue);
		} else {
			PropertyValue = source;
		}

		return PropertyValue;
	}

	/// <summary>
	/// Retrieves (using reflection) the value of the static property described in <see cref="PropertyInfo"/>
	/// into <see cref="PropertyValue"/>.
	/// </summary>
	/// <returns></returns>
	public object? RetrieveValue ()
	{
		return PropertyValue = PropertyInfo!.GetValue (null);
	}

	/// <summary>
	/// Applies the <see cref="PropertyValue"/> to the property described by <see cref="PropertyInfo"/>.
	/// </summary>
	/// <returns></returns>
	public bool Apply ()
	{
		if (PropertyValue != null) {
			PropertyInfo?.SetValue (null, ConfigurationManager.DeepMemberwiseCopy (PropertyValue, PropertyInfo?.GetValue (null)));
		}
		return PropertyValue != null;
	}

}
