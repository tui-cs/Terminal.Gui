using System.Text.Json;

namespace ConfigurationTests;

public class KeyCodeJsonConverterTests
{
    [Theory]
    [InlineData (KeyCode.A, "A")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "A, ShiftMask")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "A, CtrlMask")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "A, CtrlMask, AltMask")]
    [InlineData ((KeyCode)'a' | KeyCode.AltMask | KeyCode.CtrlMask, "Space, A, CtrlMask, AltMask")]
    [InlineData ((KeyCode)'a' | KeyCode.ShiftMask, "Space, A, ShiftMask")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "Delete, CtrlMask, AltMask")]
    [InlineData (KeyCode.D4, "D4")]
    [InlineData (KeyCode.Esc, "Esc")]
    public void TestKeyRoundTripConversion (KeyCode key, string expectedStringTo)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new KeyCodeJsonConverter ());

        // Act
        string json = JsonSerializer.Serialize (key, options);
        var deserializedKey = JsonSerializer.Deserialize<KeyCode> (json, options);

        // Assert
        Assert.Equal (expectedStringTo, deserializedKey.ToString ());
    }
}