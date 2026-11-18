using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace ConfigurationTests;

public class KeyJsonConverterTests
{
    [Theory]
    [InlineData (KeyCode.A, "\"a\"")]
    [InlineData ((KeyCode)'â', "\"â\"")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "\"A\"")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "\"Ctrl+A\"")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt+A\"")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt+Delete\"")]
    [InlineData (KeyCode.D4, "\"4\"")]
    [InlineData (KeyCode.Esc, "\"Esc\"")]
    [InlineData ((KeyCode)'+' | KeyCode.AltMask | KeyCode.CtrlMask, "\"Ctrl+Alt++\"")]
    public void TestKey_Serialize (KeyCode key, string expected)
    {
        // Arrange
        var options = new JsonSerializerOptions ();
        options.Converters.Add (new KeyJsonConverter ());
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        // Act
        string json = JsonSerializer.Serialize ((Key)key, options);

        // Assert
        Assert.Equal (expected, json);
    }

    [Theory]
    [InlineData (KeyCode.A, "a")]
    [InlineData (KeyCode.A | KeyCode.ShiftMask, "A")]
    [InlineData (KeyCode.A | KeyCode.CtrlMask, "Ctrl+A")]
    [InlineData (KeyCode.A | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+A")]
    [InlineData (KeyCode.Delete | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt+Delete")]
    [InlineData (KeyCode.D4, "4")]
    [InlineData (KeyCode.Esc, "Esc")]
    [InlineData ((KeyCode)'+' | KeyCode.AltMask | KeyCode.CtrlMask, "Ctrl+Alt++")]
    public void TestKeyRoundTripConversion (KeyCode key, string expectedStringTo)
    {
        // Arrange

        // Act
        string json = JsonSerializer.Serialize ((Key)key, TuiSerializerContext.Instance.Options);
        var deserializedKey = JsonSerializer.Deserialize<Key> (json, TuiSerializerContext.Instance.Options);

        // Assert
        Assert.Equal (expectedStringTo, deserializedKey!.ToString ());
    }

    [Fact]
    public void Deserialized_Key_Equals ()
    {
        // Arrange
        Key key = Key.Q.WithCtrl;

        // Act
        string json = "\"Ctrl+Q\"";
        Key? deserializedKey = JsonSerializer.Deserialize<Key> (json, TuiSerializerContext.Instance.Options);

        // Assert
        Assert.Equal (key, deserializedKey);

    }
    [Fact]
    public void Separator_Property_Serializes_As_Glyph ()
    {
        // Act
        string json = JsonSerializer.Serialize (Key.Separator, TuiSerializerContext.Instance.Options);

        // Assert
        Assert.Equal ($"\"{Key.Separator}\"", json);
    }
}