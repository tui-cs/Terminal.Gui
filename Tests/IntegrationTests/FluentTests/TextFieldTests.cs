using System.Drawing;
using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

public class TextFieldTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void TextField_Cursor_AtEnd_WhenTyping (string d)
    {
        // Simulates typing abcd into a TextField with width 3 (wide enough to render 2 characters only)
        using AppTestHelper c = With.A<Window> (100, 20, d,  _out)
                                        .Add (new TextField { Width = 3 })
                                        .Focus<TextField> ()
                                        .AssertCursorPosition (new (1, 1)) // Initial cursor position (because Window has border)
                                        .KeyDown (Key.A)
                                        .ScreenShot ("After typing first letter", _out)
                                        .AssertCursorPosition (new Point (2, 1)) // Cursor moves along as letter is pressed
                                        .KeyDown (Key.B)
                                        .AssertCursorPosition (new Point (3, 1)) // Cursor moves along as letter is pressed
                                        .KeyDown (Key.C)
                                        .ScreenShot ("After typing all letters", _out)
                                        .AssertCursorPosition (new Point (3, 1)) // Cursor stays where it is because we are at end of TextField
                                        .KeyDown (Key.D)
                                        .ScreenShot ("Typing one more letter", _out)
                                        .AssertCursorPosition (new Point (3, 1)) // Cursor still stays at end of TextField
                                        .WriteOutLogs (_out)
            ;
    }
}
