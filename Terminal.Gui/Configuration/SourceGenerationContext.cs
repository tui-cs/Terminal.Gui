using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Terminal.Gui.Input;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Allow AOT and self-contained single file applications with the <see cref="System.Text.Json.Serialization"/>.
///     <para>
///         The SourceGenerationContext class leverages the System.Text.Json source generation feature to pre-generate
///         serialization metadata for specific types. This approach avoids runtime reflection, which is problematic in AOT
///         scenarios where metadata might be stripped, and improves performance by generating serialization code at
///         compile time.
///     </para>
/// </summary>
[JsonSerializable (typeof (bool?))]
[JsonSerializable (typeof (Dictionary<string, object>))]
[JsonSerializable (typeof (List<string>))]

[JsonSerializable (typeof (Attribute))]
[JsonSerializable (typeof (Color))]
[JsonSerializable (typeof (Key))]
[JsonSerializable (typeof (Key []))]
[JsonSerializable (typeof (Alignment))]
[JsonSerializable (typeof (AlignmentModes))]
[JsonSerializable (typeof (LineStyle))]
[JsonSerializable (typeof (ShadowStyles))]
[JsonSerializable (typeof (MouseState))]
[JsonSerializable (typeof (TextStyle))]
[JsonSerializable (typeof (CursorStyle))]
[JsonSerializable (typeof (Dictionary<ColorName16, string>))]
[JsonSerializable (typeof (Dictionary<string, Color>))]

[JsonSerializable (typeof (Scheme))]
[JsonSerializable (typeof (Dictionary<string, Scheme>))]
[JsonSerializable (typeof (ThemeDefinition))]
[JsonSerializable (typeof (Dictionary<string, ThemeDefinition>))]

[JsonSerializable (typeof (TraceCategory))]
[JsonSerializable (typeof (SizeDetectionMode))]
[JsonSerializable (typeof (AppModel))]

[JsonSerializable (typeof (PlatformKeyBinding))]
[JsonSerializable (typeof (PlatformMouseBinding))]

[JsonSerializable (typeof (Dictionary<Command, PlatformKeyBinding>))]
[JsonSerializable (typeof (Dictionary<Command, PlatformMouseBinding>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<Command, PlatformKeyBinding>>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<Command, PlatformMouseBinding>>))]

internal partial class SourceGenerationContext : JsonSerializerContext
{ }
