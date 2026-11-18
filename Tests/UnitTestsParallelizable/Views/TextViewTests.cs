#nullable disable
using System.Text;
using UnitTests;

namespace ViewsTests.TextViewTests;

public class TextViewTests (ITestOutputHelper output)
{
    [Fact]
    public void CloseFile_Throws_If_FilePath_Is_Null ()
    {
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => tv.CloseFile ());
    }

    [Fact]
    public void LoadFile_Should_Release_File_Lock_After_Return ()
    {
        // Arrange
        string tempFile = Path.GetTempFileName ();
        File.WriteAllText (tempFile, "test content");

        TextView tv = new ();

        // Act
        Assert.True (tv.Load (tempFile));
        Assert.Equal ("test content", tv.Text);

        // Assert
        // Try to reopen the file with NO sharing (exclusive lock)
        // This will fail if the previous stream is still open
        Exception exception = Record.Exception (() =>
                                                {
                                                    using FileStream stream = new (tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                                                });

        Assert.Null (exception);

        // Cleanup
        File.Delete (tempFile);
    }

    [Fact]
    public void ContentsChanged_Event_Fires_ClearHistoryChanges ()
    {
        var eventcount = 0;

        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Width = 50, Height = 10, Text = text };
        tv.ContentsChanged += (s, e) => { eventcount++; };

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);

        var expectedEventCount = 1; // for ENTER key
        Assert.Equal (expectedEventCount, eventcount);

        tv.ClearHistoryChanges ();
        expectedEventCount = 2;
        Assert.Equal (expectedEventCount, eventcount);
    }

    [Fact]
    public void ContentsChanged_Event_Fires_LoadStream_By_Calling_HistoryText_Clear ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };
        tv.ContentsChanged += (s, e) => { eventcount++; };

        var text = "This is the first line.\r\nThis is the second line.\r\n";
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);

        Assert.Equal (1, eventcount);
    }

    [Fact]
    public void ContentsChanged_Event_Fires_On_LoadFile_By_Calling_HistoryText_Clear ()
    {
        var eventcount = 0;

        var tv = new TextView { Width = 50, Height = 10 };
        tv.BeginInit ();
        tv.EndInit ();

        tv.ContentsChanged += (s, e) => { eventcount++; };

        var fileName = "textview.txt";
        File.WriteAllText (fileName, "This is the first line.\r\nThis is the second line.\r\n");

        tv.Load (fileName);
        Assert.Equal (1, eventcount);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void GetRegion_StringFromRunes_Environment_NewLine ()
    {
        var tv = new TextView { Text = $"1{Environment.NewLine}2" };

        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal ("", tv.SelectedText);

        tv.SelectAll ();
        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal ($"1{Environment.NewLine}2", tv.SelectedText);
    }

    [Fact]
    public void HistoryText_ClearHistoryChanges ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        tv.ClearHistoryChanges ();

        Assert.Equal ($"{Environment.NewLine}This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Exceptions ()
    {
        var ht = new HistoryText ();

        foreach (object ls in Enum.GetValues (typeof (TextEditingLineStatus)))
        {
            if ((TextEditingLineStatus)ls != TextEditingLineStatus.Original)
            {
                Assert.Throws<ArgumentException> (() => ht.Add (new List<List<Cell>> (), Point.Empty, (TextEditingLineStatus)ls));
            }
        }

        Assert.Null (Record.Exception (() => ht.Add (new List<List<Cell>> { new () }, Point.Empty)));
    }

    [Fact]
    public void HistoryText_IsDirty_HasHistoryChanges ()
    {
        var tv = new TextView ();

        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ($"1{Environment.NewLine}2", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ($"1{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // IsDirty cannot be based on HasHistoryChanges because HasHistoryChanges is greater than 0
        // The only way is comparing from the original text
        Assert.False (tv.IsDirty);

        // Still true because HasHistoryChanges is greater than 0
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Changing_On_Middle_Clear_History_Forwards ()
    {
        var tv = new TextView ();

        Assert.True (tv.NewKeyDownEvent (Key.D1));
        Assert.Equal ("1", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D2));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D3));
        Assert.Equal ("123", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("12", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.D4));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("124", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (3, 0), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Disabled_On_WordWrap ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        Assert.False (tv.WordWrap);
        tv.WordWrap = true;

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Ending_With_Newline_Multi_Line_Selected_Almost_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.\n";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (12, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (12, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.{Environment.NewLine}",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (12, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}third line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine}athird line.{Environment.NewLine}", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_First_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine}a line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_KillWordBackward ()
    {
        var text = "First line.\nSecond line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (12, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (12, 1), tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (11, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second ", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (7, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (11, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First line", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First ", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (6, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_KillWordForward ()
    {
        var text = "First line.\nSecond line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Delete.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"First line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"line.{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($".{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}Second line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("Second line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("line.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal (".", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_All_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"{Environment.NewLine}a", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (1, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharLeft_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Backspace));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multi_Line_Selected_DeleteCharRight_All ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.End.WithCtrl.WithShift));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (70 + Environment.NewLine.Length * 2, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.False (tv.HasHistoryChanges);

        Assert.True (tv.NewKeyDownEvent (Key.Delete));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (23, 2), tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.False (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("", tv.Text);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.SelectedLength);
        Assert.True (tv.IsDirty);
        Assert.True (tv.HasHistoryChanges);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Multiline_Selected_Tab ()
    {
        var text = $"First line.{Environment.NewLine}Second line.{Environment.NewLine}Third line.";
        var tv = new TextView { Width = 80, Height = 5, Text = text };

        tv.SelectionStartColumn = 6;
        tv.InsertionPoint = new Point (6, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Tab));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (7, 0), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal (text, tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (6, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("First \tline.", tv.Text);
        Assert.Equal (1, tv.Lines);
        Assert.Equal (new Point (7, 0), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 0);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Single_Second_Line_Selected_Return_And_InsertText ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.SelectionStartRow = 1;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.A));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the {Environment.NewLine}a line.{Environment.NewLine}This is the third line.",
                      tv.Text);
        Assert.Equal (4, tv.Lines);
        Assert.Equal (new Point (1, 2), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Three_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (17, 2);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (17, 2), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ($"This is the {Environment.NewLine} line.", tv.Text);
        Assert.Equal (2, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_Two_Line_Selected_Return ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (18, 1);

        Assert.True (tv.NewKeyDownEvent (Key.Enter));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        Assert.Equal ($"This is the {Environment.NewLine} line.{Environment.NewLine}This is the third line.", tv.Text);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 1), tv.InsertionPoint);
    }

    [Fact]
    public void HistoryText_Undo_Redo_ApplyCellsAttribute ()
    {
        var text = "This is the first line.\nThis is the second line.\nThis is the third line.";
        var tv = new TextView { Text = text };

        tv.SelectionStartColumn = 12;
        tv.InsertionPoint = new Point (18, 1);

        if (Environment.NewLine.Length == 2)
        {
            Assert.Equal (31, tv.SelectedLength);
        }
        else
        {
            Assert.Equal (30, tv.SelectedLength);
        }

        Assert.Equal ($"first line.{Environment.NewLine}This is the second", tv.SelectedText);
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", Cell.ToString (tv.SelectedCellsList));
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        AssertNullAttribute ();

        tv.ApplyCellsAttribute (new Attribute (Color.Red, Color.Green));

        AssertRedGreenAttribute ();

        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Equal ($"first line.{Environment.NewLine}This is the second", Cell.ToString (tv.SelectedCellsList));
        Assert.Equal (new Point (18, 1), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        // Undo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));

        AssertNullAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new Point (12, 0), tv.InsertionPoint);
        Assert.False (tv.IsDirty);

        // Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));

        AssertRedGreenAttribute ();

        Assert.Equal (12, tv.SelectionStartColumn);
        Assert.Equal (0, tv.SelectionStartRow);
        Assert.Equal (0, tv.SelectedLength);
        Assert.Equal ("", tv.SelectedText);
        Assert.Empty (tv.SelectedCellsList);
        Assert.Equal (new Point (12, 0), tv.InsertionPoint);
        Assert.True (tv.IsDirty);

        void AssertNullAttribute ()
        {
            tv.GetRegion (out List<List<Cell>> region, 0, 12, 1, 18);

            foreach (List<Cell> cells in region)
            {
                foreach (Cell cell in cells)
                {
                    Assert.Null (cell.Attribute);
                }
            }
        }

        void AssertRedGreenAttribute ()
        {
            tv.GetRegion (out List<List<Cell>> region, 0, 12, 1, 18);

            foreach (List<Cell> cells in region)
            {
                foreach (Cell cell in cells)
                {
                    Assert.Equal ("[Red,Green,None]", cell.Attribute.ToString ());
                }
            }
        }
    }

    [Fact]
    public void Internal_Tests ()
    {
        var txt = "This is a text.";
        List<Cell> txtStrings = Cell.StringToCells (txt);
        Assert.Equal (txt.Length, txtStrings.Count);
        Assert.Equal ("T", txtStrings [0].Grapheme);
        Assert.Equal ("h", txtStrings [1].Grapheme);
        Assert.Equal ("i", txtStrings [2].Grapheme);
        Assert.Equal ("s", txtStrings [3].Grapheme);
        Assert.Equal (" ", txtStrings [4].Grapheme);
        Assert.Equal ("i", txtStrings [5].Grapheme);
        Assert.Equal ("s", txtStrings [6].Grapheme);
        Assert.Equal (" ", txtStrings [7].Grapheme);
        Assert.Equal ("a", txtStrings [8].Grapheme);
        Assert.Equal (" ", txtStrings [9].Grapheme);
        Assert.Equal ("t", txtStrings [10].Grapheme);
        Assert.Equal ("e", txtStrings [11].Grapheme);
        Assert.Equal ("x", txtStrings [12].Grapheme);
        Assert.Equal ("t", txtStrings [13].Grapheme);
        Assert.Equal (".", txtStrings [^1].Grapheme);

        var col = 0;
        Assert.True (TextModel.SetCol (ref col, 80, 79));
        Assert.False (TextModel.SetCol (ref col, 80, 80));
        Assert.Equal (79, col);

        var start = 0;
        var x = 8;
        Assert.Equal (8, TextModel.GetColFromX (txtStrings, start, x));
        Assert.Equal ("a", txtStrings [start + x].Grapheme);
        start = 1;
        x = 7;
        Assert.Equal (7, TextModel.GetColFromX (txtStrings, start, x));
        Assert.Equal ("a", txtStrings [start + x].Grapheme);

        Assert.Equal ((15, 15), TextModel.DisplaySize (txtStrings));
        Assert.Equal ((6, 6), TextModel.DisplaySize (txtStrings, 1, 7));

        Assert.Equal (0, TextModel.CalculateLeftColumn (txtStrings, 0, 7, 8));
        Assert.Equal (1, TextModel.CalculateLeftColumn (txtStrings, 0, 8, 8));
        Assert.Equal (2, TextModel.CalculateLeftColumn (txtStrings, 0, 9, 8));

        var tm = new TextModel ();
        tm.AddLine (0, Cell.StringToCells ("This is first line."));
        tm.AddLine (1, Cell.StringToCells ("This is last line."));
        Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out bool gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 0), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 1), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 1), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 0), true), tm.FindNextText ("is", out gaveFullTurn));
        Assert.True (gaveFullTurn);
        tm.ResetContinuousFind (Point.Empty);
        Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (2, 0), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.False (gaveFullTurn);
        Assert.Equal ((new Point (5, 1), true), tm.FindPreviousText ("is", out gaveFullTurn));
        Assert.True (gaveFullTurn);

        Assert.Equal ((new Point (9, 1), true), tm.ReplaceAllText ("is", false, false, "really"));
        Assert.Equal (Cell.StringToCells ("Threally really first line."), tm.GetLine (0));
        Assert.Equal (Cell.StringToCells ("Threally really last line."), tm.GetLine (1));
        tm = new TextModel ();
        tm.AddLine (0, Cell.StringToCells ("This is first line."));
        tm.AddLine (1, Cell.StringToCells ("This is last line."));
        Assert.Equal ((new Point (5, 1), true), tm.ReplaceAllText ("is", false, true, "really"));
        Assert.Equal (Cell.StringToCells ("This really first line."), tm.GetLine (0));
        Assert.Equal (Cell.StringToCells ("This really last line."), tm.GetLine (1));
    }

    [Fact]
    public void Viewport_X_Add_One_If_Text_Length_Is_Equal_To_Width ()
    {
        var tv = new TextView { Width = 10, Text = "1234567890" };

        Assert.Equal (Point.Empty, tv.InsertionPoint);
        Assert.Equal (0, tv.Viewport.X);

        tv.InsertionPoint = new Point (9, 0);
        Assert.Equal (new Point (9, 0), tv.InsertionPoint);
        Assert.Equal (0, tv.Viewport.X);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        tv.InsertionPoint = new Point (10, 0);
        Assert.Equal (new Point (10, 0), tv.InsertionPoint);
        Assert.Equal (1, tv.Viewport.X);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Is_Empty ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<ArgumentException> (() => result = tv.Load (""));
        Assert.False (result);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Is_Null ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => result = tv.Load ((string)null));
        Assert.False (result);
    }

    [Fact]
    public void LoadFile_Throws_If_File_Not_Exist ()
    {
        var result = false;
        var tv = new TextView ();
        Assert.Throws<FileNotFoundException> (() => result = tv.Load ("blabla"));
        Assert.False (result);
    }

    [Fact]
    public void LoadStream_CRLF ()
    {
        var text = "This is the first line.\r\nThis is the second line.\r\n";
        var tv = new TextView ();
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void LoadStream_IsDirty ()
    {
        var text = "Testing";

        using (var stream = new MemoryStream ())
        {
            var writer = new StreamWriter (stream);
            writer.Write (text);
            writer.Flush ();
            stream.Position = 0;

            var tv = new TextView ();
            tv.Load (stream);

            Assert.Equal (7, text.Length);
            Assert.Equal (text.Length, tv.Text.Length);
            Assert.Equal (text, tv.Text);
            Assert.False (tv.IsDirty);
        }
    }

    [Fact]
    public void LoadStream_IsDirty_With_Null_On_The_Text ()
    {
        var text = "Test\0ing";

        using (var stream = new MemoryStream ())
        {
            var writer = new StreamWriter (stream);
            writer.Write (text);
            writer.Flush ();
            stream.Position = 0;

            var tv = new TextView ();
            tv.Load (stream);

            Assert.Equal (8, text.Length);
            Assert.Equal (text.Length, tv.Text.Length);
            Assert.Equal (8, text.Length);
            Assert.Equal (8, tv.Text.Length);
            Assert.Equal (text, tv.Text);
            Assert.False (tv.IsDirty);
            Assert.Equal ((Rune)'\u2400', ((Rune)tv.Text [4]).MakePrintable ());
        }
    }

    [Fact]
    public void LoadStream_LF ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView ();
        tv.Load (new MemoryStream (Encoding.ASCII.GetBytes (text)));

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void LoadStream_Stream_Is_Empty ()
    {
        var tv = new TextView ();
        tv.Load (new MemoryStream ());
        Assert.Equal ("", tv.Text);
    }

    [Fact]
    public void LoadStream_Throws_If_Stream_Is_Null ()
    {
        var tv = new TextView ();
        Assert.Throws<ArgumentNullException> (() => tv.Load ((Stream)null));
    }

    [Fact]
    public void ReplaceAllText_Does_Not_Throw_Exception ()
    {
        var textToFind = "hello! hello!";
        var textToReplace = "hello!";
        var tv = new TextView { Width = 20, Height = 3, Text = textToFind };

        Exception exception = Record.Exception (() => tv.ReplaceAllText (textToFind, false, false, textToReplace));
        Assert.Null (exception);
        Assert.Equal (textToReplace, tv.Text);
    }

    [Fact]
    public void StringToRunes_Slipts_CRLF ()
    {
        var text = "This is the first line.\r\nThis is the second line.\r\n";
        var tv = new TextView ();
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void StringToRunes_Slipts_LF ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView ();
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void MultiLine_But_Without_Tabs ()
    {
        var view = new TextView ();

        // the default for TextView
        Assert.True (view.Multiline);

        view.TabKeyAddsTab = false;
        Assert.False (view.TabKeyAddsTab);

        Assert.True (view.Multiline);
    }

    [Fact]
    public void WordBackward_WordForward_Limits_Return_Null ()
    {
        var model = new TextModel ();
        model.LoadString ("Test");
        (int col, int row)? newPos = model.WordBackward (0, 0, false);
        Assert.Null (newPos);
        newPos = model.WordForward (4, 0, false);
        Assert.Null (newPos);
    }

    [Fact]
    public void WordWrap_Gets_Sets ()
    {
        var tv = new TextView { WordWrap = true };
        Assert.True (tv.WordWrap);
        tv.WordWrap = false;
        Assert.False (tv.WordWrap);
    }

    [Fact]
    public void WordWrap_True_Text_Always_Returns_Unwrapped ()
    {
        var text = "This is the first line.\nThis is the second line.\n";
        var tv = new TextView { Width = 10 };
        tv.Text = text;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
        tv.WordWrap = true;

        Assert.Equal ($"This is the first line.{Environment.NewLine}This is the second line.{Environment.NewLine}", tv.Text);
    }

    [Fact]
    public void HotKey_Command_SetsFocus ()
    {
        var view = new TextView ();

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.InvokeCommand (Command.HotKey);
        Assert.True (view.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var view = new TextView ();
        var accepted = false;
        view.Accepting += OnAccept;
        view.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) => accepted = true;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 1)]
    public void Accepted_Command_Raises_Accepted_Regardless_Of_EnterKeyAddsLine (bool allowsReturn, int expectedAcceptEvents)
    {
        var view = new TextView { EnterKeyAddsLine = allowsReturn };

        var acceptedEvents = 0;
        view.Accepting += Accept;
        view.InvokeCommand (Command.Accept);
        Assert.Equal (expectedAcceptEvents, acceptedEvents);

        return;

        void Accept (object sender, CommandEventArgs e) => acceptedEvents++;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_AllowsReturn (bool allowsReturn, int expectedAccepts)
    {
        var view = new TextView { Multiline = allowsReturn };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) => accepted++;
    }

    [Theory]
    [InlineData (false, 1)]
    [InlineData (true, 0)]
    public void Enter_Key_Fires_Accepted_BasedOn_Multiline (bool multiline, int expectedAccepts)
    {
        var view = new TextView { Multiline = multiline };

        var accepted = 0;
        view.Accepting += Accept;
        view.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, accepted);

        return;

        void Accept (object sender, CommandEventArgs e) => accepted++;
    }

    [Fact]
    public void Space_Key_Types_Space ()
    {
        TextView view = new ();

        view.NewKeyDownEvent (Key.Space);

        Assert.Equal (" ", view.Text);
    }

    [Theory]
    [InlineData (false, false, 1, 1)]
    [InlineData (false, true, 1, 0)]
    [InlineData (true, false, 0, 0)]
    [InlineData (true, true, 0, 0)]
    public void Accepted_Event_Handled_Prevents_Default_Button_Accept (bool multiline, bool handleAccept, int expectedAccepts, int expectedButtonAccepts)
    {
        var superView = new Window ();

        var tv = new TextView { Multiline = multiline };

        var button = new Button { IsDefault = true };

        superView.Add (tv, button);

        var buttonAccept = 0;
        button.Accepting += ButtonAccept;

        var textViewAccept = 0;
        tv.Accepting += TextViewAccept;

        tv.SetFocus ();
        Assert.True (tv.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, textViewAccept);
        Assert.Equal (expectedButtonAccepts, buttonAccept);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedAccepts, textViewAccept);
        Assert.Equal (expectedButtonAccepts + 1, buttonAccept);

        return;

        void TextViewAccept (object sender, CommandEventArgs e)
        {
            textViewAccept++;
            e.Handled = handleAccept;
        }

        void ButtonAccept (object sender, CommandEventArgs e) => buttonAccept++;
    }

    [Theory]
    [InlineData (true, 0)]
    [InlineData (false, 1)]
    public void Accepted_No_Handler_Enables_Default_Button_Accept (bool multiline, int expectedButtonAccept)
    {
        var superView = new Window ();

        var tv = new TextView { Multiline = multiline };

        var button = new Button { IsDefault = true };

        superView.Add (tv, button);

        var buttonAccept = 0;
        button.Accepting += ButtonAccept;

        tv.SetFocus ();
        Assert.True (tv.HasFocus);

        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedButtonAccept, buttonAccept);

        button.SetFocus ();
        superView.NewKeyDownEvent (Key.Enter);
        Assert.Equal (expectedButtonAccept + 1, buttonAccept);

        return;

        void ButtonAccept (object sender, CommandEventArgs e) => buttonAccept++;
    }

    [Fact]
    public void Autocomplete_Popup_Added_To_SuperView_On_Init ()
    {
        View superView = new () { CanFocus = true };

        TextView t = new ();

        superView.Add (t);
        Assert.Single (superView.SubViews);

        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete__Added_To_SuperView_On_Add ()
    {
        View superView = new () { CanFocus = true, Id = "superView" };

        superView.BeginInit ();
        superView.EndInit ();
        Assert.Empty (superView.SubViews);

        TextView t = new () { Id = "t" };

        superView.Add (t);

        Assert.Equal (2, superView.SubViews.Count);
    }

    [Fact]
    public void Autocomplete_Visible_False_By_Default ()
    {
        View superView = new () { CanFocus = true };

        TextView t = new ();

        superView.Add (t);
        superView.BeginInit ();
        superView.EndInit ();

        Assert.Equal (2, superView.SubViews.Count);

        Assert.True (t.Visible);
        Assert.False (t.Autocomplete.Visible);
    }

    [Fact]
    public void Right_CursorAtEnd_WithSelection_ShouldClearSelection ()
    {
        var tv = new TextView { Text = "Hello" };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.End.WithShift);
        Assert.Equal (5, tv.InsertionPoint.X);

        // When there is selected text and the cursor is at the end of the text field
        Assert.Equal ("Hello", tv.SelectedText);

        // Pressing right should not move focus, instead it should clear selection
        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Empty (tv.SelectedText);

        // Now that the selection is cleared another right keypress should move focus
        Assert.False (tv.NewKeyDownEvent (Key.CursorRight));
    }

    [Fact]
    public void Left_CursorAtStart_WithSelection_ShouldClearSelection ()
    {
        var tv = new TextView { Text = "Hello" };
        tv.SetFocus ();

        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorRight);

        Assert.Equal (2, tv.InsertionPoint.X);

        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft.WithShift));

        // When there is selected text and the cursor is at the start of the text field
        Assert.Equal ("He", tv.SelectedText);

        // Pressing left should not move focus, instead it should clear selection
        Assert.True (tv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Empty (tv.SelectedText);

        // When clearing selected text with left the cursor should be at the start of the selection
        Assert.Equal (0, tv.InsertionPoint.X);

        // Now that the selection is cleared another left keypress should move focus
        Assert.False (tv.NewKeyDownEvent (Key.CursorLeft));
    }

    [Fact]
    public void Equals_True ()
    {
        var c1 = new Cell ();
        var c2 = new Cell ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));

        c1.Grapheme = new string ("a");
        c1.Attribute = new Attribute ();
        c2.Grapheme = new string ("a");
        c2.Attribute = new Attribute ();
        Assert.True (c1.Equals (c2));
        Assert.True (c2.Equals (c1));
    }

    [Fact]
    public void Cell_LoadCells_Without_Scheme_Is_Never_Null ()
    {
        List<Cell> cells = new ()
        {
            new Cell { Grapheme = new string ("T") },
            new Cell { Grapheme = new string ("e") },
            new Cell { Grapheme = new string ("s") },
            new Cell { Grapheme = new string ("t") }
        };
        TextView tv = CreateTextView ();
        var top = new Runnable ();
        top.Add (tv);
        tv.Load (cells);

        for (var i = 0; i < tv.Lines; i++)
        {
            List<Cell> line = tv.GetLine (i);

            foreach (Cell c in line)
            {
                Assert.NotNull (c.Attribute);
            }
        }
    }

    [Theory]
    [InlineData ("", false, "")]
    [InlineData ("", true, "")]
    [InlineData (" ", false, "")]
    [InlineData (" ", true, "")]
    [InlineData ("  ", false, "")]
    [InlineData ("  ", true, "")]
    [InlineData ("a", false, "")]
    [InlineData ("a", true, "")]
    [InlineData ("a ", false, "")]
    [InlineData ("a ", true, "")]
    [InlineData (" a ", false, "a ", "")]
    [InlineData (" a ", true, "a ", "")]
    [InlineData ("  H1  ", false, "H1  ", "")]
    [InlineData ("  H1  ", true, "H1  ", "")]
    [InlineData ("a$", false, "$", "")]
    [InlineData ("a$", true, "$", "")]
    [InlineData ("a$#", false, "$#", "")]
    [InlineData ("a$#", true, "$#", "#", "")]
    [InlineData ("  a$#  ", false, "a$#  ", "$#  ", "")]
    [InlineData ("  a$#  ", true, "a$#  ", "$#  ", "#  ", "")]
    [InlineData ("\"$schema\"", false, "schema\"", "\"", "")]
    [InlineData ("\"$schema\"", true, "$schema\"", "schema\"", "\"", "")]
    [InlineData ("\": \"", false, "\"", "")]
    [InlineData ("\": \"", true, "\"", "")]
    [InlineData ("\"$schema\": \"", false, "schema\": \"", "\": \"", "\"", "")]
    [InlineData ("\"$schema\": \"", true, "$schema\": \"", "schema\": \"", "\": \"", "\"", "")]
    [InlineData ("1ºªA", false, "")]
    [InlineData ("1ºªA", true, "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", false, "\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", true, "\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", "|$=+´`~^<>£€¨", "")]
    [InlineData ("{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 false,
                 "\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/schemas/tui-config-schema.json\"\r\n}",
                 "schemas/tui-config-schema.json\"\r\n}",
                 "/tui-config-schema.json\"\r\n}",
                 "tui-config-schema.json\"\r\n}",
                 "-config-schema.json\"\r\n}",
                 "config-schema.json\"\r\n}",
                 "-schema.json\"\r\n}",
                 "schema.json\"\r\n}",
                 ".json\"\r\n}",
                 "json\"\r\n}",
                 "\"\r\n}",
                 "\r\n}",
                 "}",
                 "")]
    [InlineData ("{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 true,
                 "\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "\"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 ".Gui/schemas/tui-config-schema.json\"\r\n}",
                 "Gui/schemas/tui-config-schema.json\"\r\n}",
                 "/schemas/tui-config-schema.json\"\r\n}",
                 "schemas/tui-config-schema.json\"\r\n}",
                 "/tui-config-schema.json\"\r\n}",
                 "tui-config-schema.json\"\r\n}",
                 "-config-schema.json\"\r\n}",
                 "config-schema.json\"\r\n}",
                 "-schema.json\"\r\n}",
                 "schema.json\"\r\n}",
                 ".json\"\r\n}",
                 "json\"\r\n}",
                 "\"\r\n}",
                 "\r\n}",
                 "}",
                 "")]
    public void WordForward_WordWrap_False_True (string text, bool useSameRuneType, params string [] expectedText)
    {
        TextView tv = CreateTextView ();
        tv.UseSameRuneTypeForWords = useSameRuneType;

        ProcessDeleteWithCtrl ();

        tv.WordWrap = true;
        ProcessDeleteWithCtrl ();

        void ProcessDeleteWithCtrl ()
        {
            tv.Text = text;
            var idx = 0;

            while (!string.IsNullOrEmpty (tv.Text))
            {
                tv.NewKeyDownEvent (Key.Delete.WithCtrl);
                Assert.Equal (expectedText [idx].Replace ("\r\n", Environment.NewLine), tv.Text);
                idx++;
            }
        }
    }

    [Theory]
    [InlineData ("", false, "")]
    [InlineData ("", true, "")]
    [InlineData (" ", false, "")]
    [InlineData (" ", true, "")]
    [InlineData ("  ", false, "")]
    [InlineData ("  ", true, "")]
    [InlineData ("a", false, "")]
    [InlineData ("a", true, "")]
    [InlineData ("a ", false, "")]
    [InlineData ("a ", true, "")]
    [InlineData (" a ", false, " ", "")]
    [InlineData (" a ", true, " ", "")]
    [InlineData ("  H1  ", false, "  ", "")]
    [InlineData ("  H1  ", true, "  ", "")]
    [InlineData ("a$", false, "a", "")]
    [InlineData ("a$", true, "a", "")]
    [InlineData ("a$#", false, "a", "")]
    [InlineData ("a$#", true, "a$", "a", "")]
    [InlineData ("  a$#  ", false, "  a", "  ", "")]
    [InlineData ("  a$#  ", true, "  a$", "  a", "  ", "")]
    [InlineData ("\"$schema\"", false, "\"$schema", "\"$", "")]
    [InlineData ("\"$schema\"", true, "\"$schema", "\"$", "\"", "")]
    [InlineData ("\"$schema\": \"", false, "\"$schema\": ", "\"$schema", "\"$", "")]
    [InlineData ("\"$schema\": \"", true, "\"$schema\": ", "\"$schema", "\"$", "\"", "")]
    [InlineData ("1ºªA", false, "")]
    [InlineData ("1ºªA", true, "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", false, "ºª", "")]
    [InlineData ("ºª\\!\"#%&/()?'«»*;,:._-@{[]}]|$=+´`~^<>£€¨", true, "ºª\\!\"#%&/()?'«»*;,:._-@{[]}]", "ºª", "")]
    [InlineData ("{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 false,
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github",
                 "{\r\n  \"$schema\": \"https://tui-cs.",
                 "{\r\n  \"$schema\": \"https://tui-cs",
                 "{\r\n  \"$schema\": \"https://tui-",
                 "{\r\n  \"$schema\": \"https://tui",
                 "{\r\n  \"$schema\": \"https://",
                 "{\r\n  \"$schema\": \"https",
                 "{\r\n  \"$schema\": \"",
                 "{\r\n  \"$schema\": ",
                 "{\r\n  \"$schema",
                 "{\r\n  \"$",
                 "{\r\n  ",
                 "{\r\n",
                 "{",
                 "")]
    [InlineData ("{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n}",
                 true,
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"\r\n",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json\"",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-schema",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config-",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-config",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui-",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/tui",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/schemas",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.Gui",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/Terminal",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io/",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.io",
                 "{\r\n  \"$schema\": \"https://tui-cs.github.",
                 "{\r\n  \"$schema\": \"https://tui-cs.github",
                 "{\r\n  \"$schema\": \"https://tui-cs.",
                 "{\r\n  \"$schema\": \"https://tui-cs",
                 "{\r\n  \"$schema\": \"https://tui-",
                 "{\r\n  \"$schema\": \"https://tui",
                 "{\r\n  \"$schema\": \"https://",
                 "{\r\n  \"$schema\": \"https",
                 "{\r\n  \"$schema\": \"",
                 "{\r\n  \"$schema\": ",
                 "{\r\n  \"$schema",
                 "{\r\n  \"$",
                 "{\r\n  \"",
                 "{\r\n  ",
                 "{\r\n",
                 "{",
                 "")]
    public void WordBackward_WordWrap_False_True (string text, bool useSameRuneType, params string [] expectedText)
    {
        TextView tv = CreateTextView ();
        tv.UseSameRuneTypeForWords = useSameRuneType;

        ProcessBackspaceWithCtrl ();

        tv.WordWrap = true;
        ProcessBackspaceWithCtrl ();

        void ProcessBackspaceWithCtrl ()
        {
            tv.Text = text;
            tv.MoveEnd ();
            var idx = 0;

            while (!string.IsNullOrEmpty (tv.Text))
            {
                tv.NewKeyDownEvent (Key.Backspace.WithCtrl);
                Assert.Equal (expectedText [idx].Replace ("\r\n", Environment.NewLine), tv.Text);
                idx++;
            }
        }
    }

    [Theory]
    [InlineData ("", 0, false, "")]
    [InlineData ("", 0, true, "")]
    [InlineData ("a", 0, false, "a")]
    [InlineData ("a", 0, true, "a")]
    [InlineData ("a:", 0, false, "a")]
    [InlineData ("a:", 0, true, "a")]
    [InlineData ("a:", 1, false, ":")]
    [InlineData ("a:", 1, true, ":")]
    [InlineData ("a ", 0, false, "a ")]
    [InlineData ("a ", 0, true, "a")]
    [InlineData ("a ", 1, false, "a ")]
    [InlineData ("a ", 1, true, "a")]
    [InlineData ("a b", 0, false, "a ")]
    [InlineData ("a b", 0, true, "a")]
    [InlineData ("a b", 1, false, "a ")]
    [InlineData ("a b", 1, true, "a")]
    [InlineData ("a b ", 2, false, "b ")]
    [InlineData ("a b ", 2, true, "b")]
    [InlineData ("a b ", 3, false, "b ")]
    [InlineData ("a b ", 3, true, "b")]
    [InlineData (" a b ", 0, false, " ")]
    [InlineData (" a b ", 0, true, " ")]
    [InlineData (" a  b ", 2, false, "  ")]
    [InlineData (" a  b ", 2, true, "  ")]
    [InlineData (" a  b ", 3, false, "  ")]
    [InlineData (" a  b ", 3, true, "  ")]
    [InlineData (" H1$&#2you ", 2, false, "H1")]
    [InlineData (" H1$&#2you ", 2, true, "H1")]
    [InlineData (" H1$&#2you ", 3, false, "$&#")]
    [InlineData (" H1$&#2you ", 3, true, "$&#")]
    [InlineData (" H1$&#2you ", 4, false, "$&#")]
    [InlineData (" H1$&#2you ", 4, true, "$&#")]
    [InlineData (" H1$&#2you ", 5, false, "$&#")]
    [InlineData (" H1$&#2you ", 5, true, "$&#")]
    [InlineData (" H1$&#2you ", 6, false, "2you ")]
    [InlineData (" H1$&#2you ", 6, true, "2you")]
    public void ProcessDoubleClickSelection_False_True (string text, int col, bool selectWordOnly, string expectedText)
    {
        TextView tv = CreateTextView ();
        tv.Text = text;
        tv.SelectWordOnlyOnDoubleClick = selectWordOnly;

        Assert.True (tv.NewMouseEvent (new Mouse { Position = new Point (col, 0), Flags = MouseFlags.LeftButtonDoubleClicked }));
        Assert.Equal (expectedText, tv.SelectedText);
    }

    [Fact]
    public void ReadOnly_True_Move_Right_Moves_Until_The_End_Of_Text_More_One_Column ()
    {
        TextView tv = CreateTextView ();
        tv.Text = "Hi";
        tv.ReadOnly = true;

        Assert.Equal (0, tv.CurrentColumn);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (1, tv.CurrentColumn);
        Assert.Equal ("H", tv.SelectedText);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight.WithShift));
        Assert.Equal (2, tv.CurrentColumn);
        Assert.Equal ("Hi", tv.SelectedText);
    }

    [Fact]
    public void Viewport_X_Treat_Negative_Width_Glyph_As_One_Column ()
    {
        TextView tv = new () { Width = 2, Height = 1, Text = "\u001B[" };

        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal (new Point (1, 0), tv.InsertionPoint);

        Assert.True (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tv.Viewport.X);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);

        Assert.False (tv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (1, tv.Viewport.X);
        Assert.Equal (new Point (2, 0), tv.InsertionPoint);
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated when text is edited, allowing scrollbars to appear.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Text_Is_Edited ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();

        Size initialContentSize = tv.GetContentSize ();
        Assert.Equal (1, initialContentSize.Height); // Empty text should initially have 1 cell for the cursor

        // Act: Type text that exceeds viewport height (add multiple lines)
        tv.NewKeyDownEvent (Key.A);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.B);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.C);
        tv.NewKeyDownEvent (Key.Enter);
        tv.NewKeyDownEvent (Key.D);

        // Assert: ContentSize height should reflect the number of lines (4 lines)
        Size newContentSize = tv.GetContentSize ();
        Assert.Equal (4, newContentSize.Height);

        // With height=3 and 4 lines of content, vertical scrollbar should be visible
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");
    }

    [Fact]
    public void ContentSize_Updates_When_Text_Is_Deleted ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true, Text = "A\nB\nC\nD" };
        tv.BeginInit ();
        tv.EndInit ();

        Size initialContentSize = tv.GetContentSize ();
        Assert.Equal (4, initialContentSize.Height); // Initially 4 lines

        // With height=3 and 4 lines of content, vertical scrollbar should be visible
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");

        // Act: Delete all text so that the lines are shorter than the viewport height (delete multiple lines)
        for (int i = 0; i < 7; i++)
        {
            tv.NewKeyDownEvent (Key.Delete);
        }

        // Assert: ContentSize height should reflect the number of lines (1 line)
        Size newContentSize = tv.GetContentSize ();
        Assert.Equal (1, newContentSize.Height);

        // With height=3 and 1 lines of content, vertical scrollbar should be invisible
        Assert.False (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that horizontal scrollbar appears when text width exceeds viewport width.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void HorizontalScrollBar_Appears_When_Text_Width_Exceeds_Viewport ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled, WordWrap disabled
        TextView tv = new () { Width = Dim.Fill (), Height = Dim.Fill (), ScrollBars = true, WordWrap = false };
        tv.BeginInit ();
        tv.EndInit ();
        tv.SetRelativeLayout (new Size (10, 5));

        // Assert initial state
        Assert.False (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should not be visible initially");

        // Act: Type text that exceeds viewport width
        for (var i = 0; i < 20; i++)
        {
            tv.NewKeyDownEvent (Key.A);
        }

        // Assert: ContentSize width should reflect the text width
        Size newContentSize = tv.GetContentSize ();
        Assert.True (newContentSize.Width >= 20, "Content width should reflect the typed text length");

        // With width=10 and 20 characters, horizontal scrollbar should be visible
        Assert.True (tv.HorizontalScrollBar.Visible, "Horizontal scrollbar should be visible when content exceeds viewport width");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated when Text property is set programmatically.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Updates_When_Text_Property_Is_Set ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();

        Size initialContentSize = tv.GetContentSize ();
        Assert.Equal (1, initialContentSize.Height); // Empty text should have one cell for the cursor

        // Act: Set Text property programmatically with multiple lines
        tv.Text = "Line1\nLine2\nLine3\nLine4";

        // Assert: After setting Text programmatically with multiple lines,
        // the content size should reflect the new number of lines
        Size contentSizeAfterTextSet = tv.GetContentSize ();
        Assert.Equal (4, contentSizeAfterTextSet.Height);

        // And scrollbar should be visible since content (4 lines) > viewport (3 lines)
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible after setting multi-line text");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentsChanged event is fired and content size is updated appropriately.
    ///     This test verifies the behavior described in issue #4660.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentsChanged_Fires_And_ContentSize_Is_Correct_After_Typing ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();

        // Track ContentsChanged events and content size at each event
        var contentsChangedCount = 0;
        var contentSizeAtLastContentsChanged = Size.Empty;

        tv.ContentsChanged += (_, _) =>
                              {
                                  contentsChangedCount++;
                                  contentSizeAtLastContentsChanged = tv.GetContentSize ();
                              };

        // Act: Type text that exceeds viewport height
        tv.NewKeyDownEvent (Key.A);
        Assert.Equal (1, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (2, contentsChangedCount);

        // After pressing Enter, we have 2 lines
        Assert.Equal (2, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.B);
        Assert.Equal (3, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (4, contentsChangedCount);

        // After 2nd Enter, we have 3 lines
        Assert.Equal (3, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.C);
        Assert.Equal (5, contentsChangedCount);

        tv.NewKeyDownEvent (Key.Enter);
        Assert.Equal (6, contentsChangedCount);

        // After 3rd Enter, we have 4 lines
        Assert.Equal (4, contentSizeAtLastContentsChanged.Height);

        tv.NewKeyDownEvent (Key.D);
        Assert.Equal (7, contentsChangedCount);

        // Final content size should be 4 lines
        Assert.Equal (4, contentSizeAtLastContentsChanged.Height);

        // Assert: Vertical scrollbar should be visible since content (4 lines) > viewport (3 lines)
        Assert.True (tv.VerticalScrollBar.Visible, "Vertical scrollbar should be visible when content exceeds viewport height");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that ContentSize is updated *during* the ContentsChanged event, not just after.
    ///     This test specifically verifies the bug where UpdateContentSize() needs to be called
    ///     in OnContentsChanged() so that the content size is correct at the time the event fires.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ContentSize_Is_Updated_During_ContentsChanged_Event ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 1, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();

        // Track whether content size is correct INSIDE the ContentsChanged handler
        // The bug is that UpdateContentSize() isn't called in OnContentsChanged(),
        // so the content size may be stale when the event fires.
        var contentSizeWasCorrectInHandler = true;

        tv.ContentsChanged += (_, _) =>
                              {
                                  Size contentSize = tv.GetContentSize ();
                                  int expectedHeight = tv.Lines;

                                  if (contentSize.Height != expectedHeight)
                                  {
                                      contentSizeWasCorrectInHandler = false;
                                  }
                              };

        // Act: Type text that creates multiple lines
        tv.NewKeyDownEvent (Key.A); // Line 1
        tv.NewKeyDownEvent (Key.Enter); // Creates line 2
        tv.NewKeyDownEvent (Key.B); // Line 2 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 3
        tv.NewKeyDownEvent (Key.C); // Line 3 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 4
        tv.NewKeyDownEvent (Key.D); // Line 4 content

        // Assert: Content size should have been correct at each ContentsChanged event
        Assert.True (contentSizeWasCorrectInHandler, "Content size should be updated during ContentsChanged event, not just after");
    }

    // GitHub Copilot - Issue #4660: TextView scrollbars do not appear when text is edited
    /// <summary>
    ///     Tests that scrollbar visibility is correct *during* the ContentsChanged event.
    ///     The bug in issue #4660 is that scrollbar visibility isn't updated until after
    ///     OnContentsChanged returns, which can cause issues for event handlers.
    ///     See https://github.com/tui-cs/Terminal.Gui/issues/4660
    /// </summary>
    [Fact]
    public void ScrollBar_Visibility_Is_Correct_During_ContentsChanged_Event ()
    {
        // Arrange: Create a small TextView with ScrollBars enabled
        TextView tv = new () { Width = 10, Height = 3, ScrollBars = true };
        tv.BeginInit ();
        tv.EndInit ();

        // Track whether scrollbar visibility was correct when ContentsChanged fired
        // and content exceeded viewport
        var scrollbarWasVisibleWhenContentExceededViewport = false;

        tv.ContentsChanged += (_, _) =>
                              {
                                  // When we have 4 lines (height=3 viewport), scrollbar should be visible
                                  if (tv.Lines > tv.Viewport.Height)
                                  {
                                      scrollbarWasVisibleWhenContentExceededViewport = tv.VerticalScrollBar.Visible;
                                  }
                              };

        // Act: Type text that creates 4 lines (exceeds viewport height of 3)
        tv.NewKeyDownEvent (Key.A); // Line 1
        tv.NewKeyDownEvent (Key.Enter); // Creates line 2
        tv.NewKeyDownEvent (Key.B); // Line 2 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 3
        tv.NewKeyDownEvent (Key.C); // Line 3 content
        tv.NewKeyDownEvent (Key.Enter); // Creates line 4 - now content > viewport
        tv.NewKeyDownEvent (Key.D); // Line 4 content

        // Assert: Scrollbar should have been visible DURING ContentsChanged when content exceeded viewport
        Assert.True (scrollbarWasVisibleWhenContentExceededViewport,
                     "Vertical scrollbar should be visible during ContentsChanged event when content exceeds viewport height");
    }

    [Fact]
    public void Command_Activate_SetsFocus ()
    {
        TextView textView = new () { Text = "Test", Width = 10 };
        textView.BeginInit ();
        textView.EndInit ();
        Assert.False (textView.HasFocus);

        textView.InvokeCommand (Command.Activate);

        Assert.True (textView.HasFocus);

        textView.Dispose ();
    }

    [Fact]
    public void Command_HotKey_SetsFocus ()
    {
        TextView textView = new () { Text = "Test" };
        textView.BeginInit ();
        textView.EndInit ();
        Assert.False (textView.HasFocus);

        textView.InvokeCommand (Command.HotKey);

        Assert.True (textView.HasFocus);

        textView.Dispose ();
    }

    [Theory]
    [InlineData (0, 4, "\tTest", "    Test")]
    [InlineData (4, 8, "Test\t", "Test    ")]
    [InlineData (1, 4, "T\test", "T   est")]
    public void Tab_And_Shift_Tab_ContentsChanged_With_TabKeyAddsTab_True_AddsRemovesTabCharacter (
        int col,
        int expectedCursor,
        string expectedText,
        string expectedOutput)
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        View view1 = new () { CanFocus = true };
        TextView textView = CreateTextView ();
        textView.Text = "Test";
        int count = 0;
        textView.ContentsChanged += (_, _) => count++;
        View view2 = new () { CanFocus = true };
        runnable.Add (view1, textView, view2);

        app.Begin (runnable);

        // Move focus to textView
        textView.SetFocus ();
        Assert.True (textView.TabKeyAddsTab);

        // Move insertion point to specified column
        textView.InsertionPoint = new Point (col, 0);

        // Press Tab - should add tab character
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal (expectedText, textView.Text);
        Assert.True (textView.HasFocus);
        Assert.Equal (expectedCursor, textView.Cursor.Position!.Value.X);
        Assert.Equal (1, count);
        textView.Draw ();
        DriverAssert.AssertDriverContentsAre (expectedOutput, output, app.Driver);

        // Press Shift+Tab - should remove tab character
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ("Test", textView.Text);
        Assert.True (textView.HasFocus);
        Assert.Equal (col, textView.Cursor.Position!.Value.X);
        Assert.Equal (2, count);
    }

    [Theory]
    [InlineData (0,
                 0,
                 4,
                 0,
                 "\tTesting WordWrap",
                 """

                 Testing
                 WordWrap
                 """)]
    [InlineData (8,
                 1,
                 4,
                 2,
                 "Testing WordWrap\t",
                 """
                 Testing 
                 WordWrap

                 """)]
    [InlineData (1,
                 0,
                 4,
                 0,
                 "T\testing WordWrap",
                 """
                 T    
                 esting 
                 WordWrap
                 """)]
    public void Tab_And_Shift_Tab_ContentsChanged_With_TabKeyAddsTab_And_WordWrap_True_AddsRemovesTabCharacter (
        int col,
        int row,
        int expectedCursorX,
        int expectedCursorY,
        string expectedText,
        string expectedOutput)
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        View view1 = new () { CanFocus = true };
        TextView textView = new () { Width = 10, Height = 3 };
        string text = "Testing WordWrap";
        textView.Text = text;
        var count = 0;
        textView.ContentsChanged += (_, _) => count++;
        View view2 = new () { CanFocus = true };
        runnable.Add (view1, textView, view2);

        app.Begin (runnable);

        // Move focus to textView
        textView.SetFocus ();
        Assert.True (textView.TabKeyAddsTab);
        Assert.False (textView.WordWrap);

        textView.WordWrap = true;

        // Move insertion point to specified column
        textView.InsertionPoint = new Point (col, row);

        // Press Tab - should add tab character
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal (expectedText, textView.Text);
        Assert.True (textView.HasFocus);
        Assert.Equal (expectedCursorX, textView.Cursor.Position!.Value.X);
        Assert.Equal (expectedCursorY, textView.Cursor.Position!.Value.Y);
        Assert.Equal (1, count);
        textView.Draw ();
        DriverAssert.AssertDriverContentsAre (expectedOutput, output, app.Driver);

        // Press Shift+Tab - should remove tab character
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal (text, textView.Text);
        Assert.True (textView.HasFocus);
        Assert.Equal (col, textView.Cursor.Position!.Value.X);
        Assert.Equal (row, textView.Cursor.Position!.Value.Y);
        Assert.Equal (2, count);
    }

    [Fact]
    public void Tab_And_Shift_Tab_With_TabKeyAddsTab_False_DoesNotAddTabCharacter_AndMovesFocusToNextPreviousView ()
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        View view1 = new () { CanFocus = true };
        TextView textView = new () { Text = "Test", TabKeyAddsTab = false };
        View view2 = new () { CanFocus = true };
        runnable.Add (view1, textView, view2);

        app.Begin (runnable);

        // Move focus to textView
        textView.SetFocus ();

        // Press Tab - should move focus to next view
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab));
        Assert.Equal ("Test", textView.Text);
        Assert.False (textView.HasFocus);
        Assert.True (view2.HasFocus);

        // Move focus back to textView
        textView.SetFocus ();

        // Press Shift+Tab - should move focus to previous view
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ("Test", textView.Text);
        Assert.False (textView.HasFocus);
        Assert.True (view1.HasFocus);
    }

    [Fact]
    public void Shift_Tab_With_TabKeyAddsTab_True_DoesNotRemoveTabCharacter_IfTabCharacterNotPresent ()
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        View view1 = new () { CanFocus = true };
        TextView textView = new () { Text = "Test", TabKeyAddsTab = true };
        View view2 = new () { CanFocus = true };
        runnable.Add (view1, textView, view2);
        app.Begin (runnable);

        // Move focus to textView
        textView.SetFocus ();

        // Press Shift+Tab - should not remove tab character since it's not present
        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.Tab.WithShift));
        Assert.Equal ("Test", textView.Text);
        Assert.True (textView.HasFocus);
    }

    [Theory]
    [InlineData (0, "aTest")]
    [InlineData (2, "Teast")]
    public void Used_True_DoesNotOverwrite_ExistentText_AndAddTypedKey (int col, string expected)
    {
        TextView textView = CreateTextView ();
        textView.Text = "Test";
        textView.InsertionPoint = new Point (col, 0);

        Assert.True (textView.Used); // default is true

        // Simulate a key event with Used = true
        Assert.True (textView.NewKeyDownEvent (Key.A));

        // Text should not be overwritten, but the typed key should be added at the insertion point
        Assert.Equal (expected, textView.Text);
        Assert.Equal (new Point (col + 1, 0), textView.InsertionPoint);
    }

    [Theory]
    [InlineData (0, "aest")]
    [InlineData (2, "Teat")]
    public void Used_False_DoesOverwrite_ExistentText_AndReplaceWithTypedKey (int col, string expected)
    {
        TextView textView = CreateTextView ();
        textView.Text = "Test";
        textView.InsertionPoint = new Point (col, 0);
        textView.Used = false;

        // Simulate a key event with Used = false
        Assert.True (textView.NewKeyDownEvent (Key.A));

        // Text should be overwritten at the insertion point with the typed key
        Assert.Equal (expected, textView.Text);
        Assert.Equal (new Point (col + 1, 0), textView.InsertionPoint);
    }

    [Fact]
    public void Key_Insert_InvokeCommand_ToggleOverwrite_And_Toggles_Used_Property ()
    {
        TextView textView = new ();

        Assert.True (textView.Used);

        Assert.True (textView.InvokeCommand (Command.ToggleOverwrite));
        Assert.False (textView.Used);

        Assert.True (textView.InvokeCommand (Command.ToggleOverwrite));
        Assert.True (textView.Used);
    }

    [Fact]
    public void InvokeCommand_RightEnd_Draws_Cursor_At_End_Of_Line ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "Testing End Key",
            Width = 10,
            Height = 5,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 3), textView.Viewport);

        Assert.True (textView.InvokeCommand (Command.RightEnd));
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │End Key │
                       │        │
                       │        │
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);
    }

    [Fact]
    public void InvokeCommand_Right_Draws_Cursor_At_Start_Of_NextLine ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1 Testing Line 1\n2 Testing Line 2",
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 1), textView.Viewport);

        textView.InsertionPoint = new Point (16, 0); // End of first line
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.Right));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │2 Testin│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_Right_BeforeLastColumn_Draws_Cursor_At_End_Of_Line ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1 Testing Line 1\n2 Testing Line 2",
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 1), textView.Viewport);

        textView.InsertionPoint = new Point (15, 0); // Before the last column of first line
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.Right));
        Assert.Equal (new Point (16, 0), textView.InsertionPoint);
        Assert.Equal (new Point (9, 0), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │ Line 1 │
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (8, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_Left_Draws_Cursor_At_End_Of_PreviousLine ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1 Testing Line 1\n2 Testing Line 1",
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 1), textView.Viewport);

        textView.InsertionPoint = new Point (0, 1); // Start of second line
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.Left));
        Assert.Equal (new Point (16, 0), textView.InsertionPoint);
        Assert.Equal (new Point (9, 0), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │ Line 1 │
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (8, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_Left_AfterFirstColumn_Draws_Cursor_At_Start_Of_Line ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1 Testing Line 1\n2 Testing Line 1",
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 1), textView.Viewport);

        textView.InsertionPoint = new Point (1, 1); // After first column of second line
        textView.Viewport = new Rectangle (1, 1, 8, 1); // Scroll to the first column of second line
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.Left));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │2 Testin│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_PageDown_PageUp_With_ViewportHeight_One_Draws_Cursor_Correctly ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1111\n22222\n333333",
            Width = 10,
            Height = 3,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 1), textView.Viewport);

        // PageDown - Move to the next page
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.PageDown));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │22222   │
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        // PageDown - Move to the next page
        Assert.True (textView.InvokeCommand (Command.PageDown));
        Assert.Equal (new Point (0, 2), textView.InsertionPoint);
        Assert.Equal (new Point (0, 2), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │333333  │
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        // PageUp - Move previous page
        Assert.True (textView.InvokeCommand (Command.PageUp));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │22222   │
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        // PageUp - Move previous page
        Assert.True (textView.InvokeCommand (Command.PageUp));
        Assert.Equal (new Point (0, 0), textView.InsertionPoint);
        Assert.Equal (new Point (0, 0), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │1111    │
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_PageDown_PageUp_With_ViewportHeight_GreaterThanOne_Draws_Cursor_Correctly ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1111111111\n22222222222\n333333333333\n4444444444444",
            Width = 10,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 2), textView.Viewport);

        // PageDown - Move to the next page
        Assert.True (textView.NeedsDraw);
        textView.Draw (); // Draw before invoking command to ensure NeedsDraw is false
        Assert.False (textView.NeedsDraw);
        Assert.True (textView.InvokeCommand (Command.PageDown));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        var expected = """
                       ┌────────┐
                       │22222222│
                       │33333333│
                       └────────┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        // PageDown - Move to the next page
        Assert.True (textView.InvokeCommand (Command.PageDown));
        Assert.Equal (new Point (0, 2), textView.InsertionPoint);
        Assert.Equal (new Point (0, 2), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │33333333│
                   │44444444│
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        // PageDown - Move to the last line and should stay on the last line and not scroll further
        Assert.True (textView.InvokeCommand (Command.PageDown));
        Assert.Equal (new Point (0, 3), textView.InsertionPoint);
        Assert.Equal (new Point (0, 2), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │33333333│
                   │44444444│
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 2), textView.Cursor.Position!.Value);

        // PageUp - Move previous page
        Assert.True (textView.InvokeCommand (Command.PageUp));
        Assert.Equal (new Point (0, 2), textView.InsertionPoint);
        Assert.Equal (new Point (0, 1), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │22222222│
                   │33333333│
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 2), textView.Cursor.Position!.Value);

        // PageUp - Move previous page
        Assert.True (textView.InvokeCommand (Command.PageUp));
        Assert.Equal (new Point (0, 1), textView.InsertionPoint);
        Assert.Equal (new Point (0, 0), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │11111111│
                   │22222222│
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 2), textView.Cursor.Position!.Value);

        // PageUp - Move to the first line and should stay on the first line and not scroll further
        Assert.True (textView.InvokeCommand (Command.PageUp));
        Assert.Equal (new Point (0, 0), textView.InsertionPoint);
        Assert.Equal (new Point (0, 0), textView.Viewport.Location);
        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌────────┐
                   │11111111│
                   │22222222│
                   └────────┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void InvokeCommand_PageDown_PageUp_TrackColumns_AtMiddle_And_Draws_Cursor_Correctly ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1111111111\n22222222222\n333333333333\n4444444444444",
            Width = 10,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 2), textView.Viewport);

        var columnToTrack = 5; // Middle of first line
        textView.InsertionPoint = new Point (columnToTrack, 0);

        for (var i = 0; i < 3; i++)
        {
            // PageDown - Move to the next page
            Assert.True (textView.InvokeCommand (Command.PageDown));
            Assert.Equal (new Point (columnToTrack, i + 1), textView.InsertionPoint);
            textView.PositionCursor ();

            if (i == 2)
            {
                Assert.Equal (new Point (0, i), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 2), textView.Cursor.Position!.Value);
            }
            else
            {
                Assert.Equal (new Point (0, i + 1), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 1), textView.Cursor.Position!.Value);
            }
        }

        for (var i = 3; i > 0; i--)
        {
            // PageUp - Move to the previous page
            Assert.True (textView.InvokeCommand (Command.PageUp));
            Assert.Equal (new Point (columnToTrack, i - 1), textView.InsertionPoint);
            textView.PositionCursor ();

            if (i == 1)
            {
                Assert.Equal (new Point (0, i - 1), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 1), textView.Cursor.Position!.Value);
            }
            else
            {
                Assert.Equal (new Point (0, i - 2), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 2), textView.Cursor.Position!.Value);
            }
        }
    }

    [Fact]
    public void InvokeCommand_PageDown_PageUp_TrackColumns_AtEnd_And_Draws_Cursor_Correctly ()
    {
        using IApplication app = Application.Create ().Init ();

        TextView textView = new ()
        {
            Text = "1111111\n222222\n33333\n4444",
            Width = 10,
            Height = 4,
            BorderStyle = LineStyle.Single,
            Driver = app.Driver
        };
        textView.BeginInit ();
        textView.EndInit ();

        Assert.Equal (new Rectangle (0, 0, 8, 2), textView.Viewport);

        int columnToTrack = textView.GetCurrentLine ().Count; // End of first line
        textView.InsertionPoint = new Point (columnToTrack, 0);
        Assert.Equal (Point.Empty, textView.Viewport.Location);

        for (var i = 0; i < 3; i++)
        {
            // PageDown - Move to the next page
            Assert.True (textView.InvokeCommand (Command.PageDown));
            columnToTrack = textView.GetCurrentLine ().Count;
            Assert.Equal (new Point (columnToTrack, i + 1), textView.InsertionPoint);
            textView.PositionCursor ();

            if (i == 2)
            {
                Assert.Equal (new Point (0, i), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 2), textView.Cursor.Position!.Value);
            }
            else
            {
                Assert.Equal (new Point (0, i + 1), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 1), textView.Cursor.Position!.Value);
            }
        }

        for (var i = 3; i > 0; i--)
        {
            // PageUp - Move to the previous page
            Assert.True (textView.InvokeCommand (Command.PageUp));
            columnToTrack = textView.GetCurrentLine ().Count;
            Assert.Equal (new Point (columnToTrack, i - 1), textView.InsertionPoint);
            textView.PositionCursor ();

            if (i == 1)
            {
                Assert.Equal (new Point (0, i - 1), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 1), textView.Cursor.Position!.Value);
            }
            else
            {
                Assert.Equal (new Point (0, i - 2), textView.Viewport.Location);

                // Cursor is always screen relative
                Assert.Equal (new Point (columnToTrack + 1, 2), textView.Cursor.Position!.Value);
            }
        }
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void KeyDown_CursorDown_MovesFocusToNextView_WhenCursorIsAtLastColumnOfLastLine_TabKeyAddsTab_TrueOrFalse (bool tabKeyAddsTab)
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        TextView textView = new () { Text = "Test", TabKeyAddsTab = tabKeyAddsTab };
        View nextView = new () { CanFocus = true };
        runnable.Add (textView, nextView);
        app.Begin (runnable);

        // Assert it's the last line at column 0
        Assert.Equal (textView.CurrentRow, textView.Lines - 1);
        Assert.Equal (0, textView.CurrentColumn);

        // Press Down - should insertion point to last column of last line
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        Assert.True (textView.HasFocus);
        Assert.Equal (4, textView.CurrentColumn);

        // Press Down - should move focus to next view since we're at end of line
        app.Keyboard.RaiseKeyDownEvent (Key.CursorDown);
        Assert.False (textView.HasFocus);
        Assert.Equal (4, textView.CurrentColumn);
        Assert.True (nextView.HasFocus);
    }

    [Theory]
    [InlineData (true)]
    [InlineData (false)]
    public void KeyDown_CursorUp_MovesFocusToPreviousView_WhenCursorIsAtFirstColumnOfFirstLine_TabKeyAddsTab_TrueOrFalse (bool tabKeyAddsTab)
    {
        using IApplication app = Application.Create ().Init ();
        Runnable runnable = new ();
        View previousView = new () { CanFocus = true };
        TextView textView = new () { Text = "Test", TabKeyAddsTab = tabKeyAddsTab };
        runnable.Add (previousView, textView);
        app.Begin (runnable);

        // Set focus to textView
        textView.SetFocus ();

        // Assert it's the first line
        Assert.Equal (0, textView.CurrentRow);

        //  Move to end of first line
        textView.InsertionPoint = new Point (4, 0);

        // Press Up - should move insertion point to first column of first line
        app.Keyboard.RaiseKeyDownEvent (Key.CursorUp);
        Assert.True (textView.HasFocus);
        Assert.Equal (0, textView.CurrentColumn);

        // Press Up - should move focus to previous view since we're at start of line
        app.Keyboard.RaiseKeyDownEvent (Key.CursorUp);
        Assert.False (textView.HasFocus);
        Assert.Equal (0, textView.CurrentColumn);
        Assert.True (previousView.HasFocus);
    }

    [Theory]
    [InlineData (1, 0, 0, 0, 0, 0)]
    [InlineData (4, 0, 1, 0, 4, 0)]
    [InlineData (5, 0, 1, 0, 4, 0)]
    [InlineData (8, 0, 2, 0, 8, 0)]
    [InlineData (9, 0, 3, 0, 9, 0)]
    [InlineData (10, 0, 3, 0, 9, 0)]
    [InlineData (13, 0, 5, 0, 13, 0)]
    [InlineData (14, 0, 5, 0, 13, 0)]
    [InlineData (15, 0, 6, 0, 16, 0)]
    [InlineData (18, 0, 7, 0, 17, 0)]
    [InlineData (19, 0, 7, 0, 17, 0)]
    [InlineData (20, 0, 7, 0, 17, 0)]
    [InlineData (1, 1, 0, 1, 0, 1)]
    [InlineData (4, 1, 1, 1, 4, 1)]
    [InlineData (5, 1, 2, 1, 5, 1)]
    [InlineData (6, 1, 2, 1, 5, 1)]
    [InlineData (9, 1, 3, 1, 8, 1)]
    [InlineData (10, 1, 4, 1, 12, 1)]
    [InlineData (13, 1, 5, 1, 13, 1)]
    [InlineData (14, 1, 6, 1, 14, 1)]
    [InlineData (15, 1, 6, 1, 14, 1)]
    [InlineData (16, 1, 7, 1, 16, 1)]
    [InlineData (19, 1, 8, 1, 20, 1)]
    [InlineData (20, 1, 8, 1, 20, 1)]
    [InlineData (23, 1, 8, 1, 20, 1)]
    [InlineData (24, 1, 8, 1, 20, 1)]
    [InlineData (1, 2, 0, 2, 0, 2)]
    [InlineData (2, 2, 1, 2, 2, 2)]
    [InlineData (3, 2, 2, 2, 3, 2)]
    public void Mouse_Click_On_Text_With_Tabs_Sets_Cursor_At_Correct_Position (int x,
                                                                               int y,
                                                                               int expectedColumn,
                                                                               int expectedRow,
                                                                               int expectedCursorX,
                                                                               int expectedCursorY)
    {
        using IApplication app = Application.Create ().Init ();
        TextView textView = CreateTextView ();
        textView.Text = "\t\t1\t2\t3\n\t4\t\t56\t\t\n🍎7";

        // Will draw likes this:
        // "        1   2   3"
        // "    4       56      "
        // "🍎7"

        Runnable runnable = new ();
        runnable.Add (textView);

        app.Begin (runnable);

        Assert.Equal (4, textView.TabWidth);

        textView.NewMouseEvent (new Mouse { Position = new Point (x, y), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (expectedColumn, textView.CurrentColumn);
        Assert.Equal (expectedRow, textView.CurrentRow);
        Assert.Equal (expectedCursorX, textView.Cursor.Position!.Value.X);
        Assert.Equal (expectedCursorY, textView.Cursor.Position!.Value.Y);
    }

    [Fact]
    public void Mouse_Click_Beyond_Available_Lines_Sets_Last_Line_Position ()
    {
        using IApplication app = Application.Create ().Init ();
        TextView textView = CreateTextView ();
        textView.Text = "Line1\nLine2\nLine3";
        Runnable runnable = new ();
        runnable.Add (textView);

        app.Begin (runnable);

        Assert.Equal (3, textView.Lines);
        Assert.Equal (10, textView.Viewport.Height);

        textView.NewMouseEvent (new Mouse { Position = new Point (0, textView.Viewport.Height - 1), Flags = MouseFlags.LeftButtonClicked });
        Assert.Equal (2, textView.CurrentRow);
    }

    [Fact]
    public void Mouse_Left_Clicked_At_Start_And_End_Viewport_Change_ViewportX ()
    {
        // Create a TextView with text
        TextView tv = new () { Text = "Hello World", Width = 5, Height = 1 };
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);

        // Mouse button clicked at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        Mouse ev = new () { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (4, tv.InsertionPoint.X);
        Assert.Equal (1, tv.Viewport.X);

        // Mouse button clicked at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (1, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);
    }

    [Fact]
    public void Mouse_Left_Clicked_At_Start_And_End_Viewport_Change_ViewportX_Wide_Glyphs ()
    {
        // Create a TextView with text
        TextView tv = new () { Text = "👨‍👩‍👧Hello World", Width = 5, Height = 1 };
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);

        // Mouse button clicked at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        Mouse ev = new () { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (4, tv.InsertionPoint.X);
        Assert.Equal (2, tv.Viewport.X);

        // Mouse button clicked at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (1, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);
    }

    [Fact]
    public void Mouse_Selecting_From_End_And_From_Start_Viewport_SelectsCorrectly ()
    {
        // Create a TextView with text
        TextView tv = new () { Text = "Hello World\n", Width = 5, Height = 2 };
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);

        // Mouse button pressed at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        Mouse ev = new () { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed };
        tv.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the left while pressing
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Continue selecting text by moving mouse to the left while pressing forcing SelectedLength to be greater than 0
        ev = new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Mouse move to left while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal ("Hell", tv.SelectedText);

        // Release mouse button
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased };
        tv.NewMouseEvent (ev);

        // Mouse button pressed at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        tv.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the right while pressing
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Continue selecting text by moving mouse to the left while pressing forcing SelectedLength to be greater than 0
        ev = new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Mouse move to right while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (11, tv.InsertionPoint.X);
        Assert.Equal (7, tv.Viewport.X);
        Assert.Equal ("Hello World", tv.SelectedText);
    }

    [Fact]
    public void Mouse_Selecting_From_End_And_From_Start_Viewport_SelectsCorrectly_Wide_Glyphs ()
    {
        // Create a TextView with text
        TextView tv = new () { Text = "Hello World👨‍👩‍👧", Width = 5, Height = 1 };
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state with text longer than width
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);

        // Mouse button pressed at the end of the viewport should move the cursor and adjust incrementing ScrollOffset
        Mouse ev = new () { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed };
        tv.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the left while pressing
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Continue selecting text by moving mouse to the left while pressing forcing SelectedLength to be greater than 0
        ev = new Mouse { Position = new Point (3, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Mouse move to left while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (0, tv.InsertionPoint.X);
        Assert.Equal (0, tv.Viewport.X);
        Assert.Equal ("Hell", tv.SelectedText);

        // Release mouse button
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased };
        tv.NewMouseEvent (ev);

        // Mouse button pressed at the start of the viewport should move the cursor and adjust decreasing ScrollOffset
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed };
        tv.NewMouseEvent (ev);

        // Start selecting text by moving mouse to the right while pressing
        ev = new Mouse { Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Mouse move to right while pressing should select text and adjust ScrollOffset
        ev = new Mouse { Position = new Point (4, 0), Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport };
        tv.NewMouseEvent (ev);

        // Verify cursor is positioned at pressed location
        Assert.Equal (0, tv.InsertionPoint.Y);
        Assert.Equal (0, tv.Viewport.Y);
        Assert.Equal (12, tv.InsertionPoint.X);
        Assert.Equal (9, tv.Viewport.X);
        Assert.Equal ("Hello World👨‍👩‍👧", tv.SelectedText);
    }

    [Fact]
    public void Mouse_Click_At_Position_GreaterThanZero_On_Empty_TextView_Should_Set_ViewportX_To_Zero ()
    {
        // Create a TextView with empty text
        TextView tv = new () { Width = 5, Height = 1 };
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);
        Assert.Equal (new Point (0, 0), tv.Viewport.Location);

        // Simulate mouse click
        Mouse ev = new () { Position = new Point (1, 0), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify Viewport.X remains at zero
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);
        Assert.Equal (new Point (0, 0), tv.Viewport.Location);
    }

    [Fact]
    public void Mouse_Click_At_Position_GreaterOrEqual_To_Lines_Count_Should_Set_InsertionPoint_To_Last_Line ()
    {
        // Create a TextView with 3 lines of text
        TextView tv = new () { Width = 6, Height = 5 };
        tv.Text = "Line1\nLine2\nLine3";
        tv.BeginInit ();
        tv.EndInit ();

        // Verify initial state
        Assert.False (tv.WordWrap);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);
        Assert.Equal (new Point (0, 0), tv.Viewport.Location);

        // Simulate mouse click at Y position greater than or equal to LinesCount
        Mouse ev = new () { Position = new Point (0, 3), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify InsertionPoint is set to the last line
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);

        // Now test with WordWrap enabled
        tv.WordWrap = true;

        // Verify initial state with WordWrap enabled
        Assert.True (tv.WordWrap);
        Assert.Equal (3, tv.Lines);
        Assert.Equal (new Point (0, 0), tv.InsertionPoint);
        Assert.Equal (new Point (0, 0), tv.Viewport.Location);

        // Simulate mouse click at Y position greater than or equal to LinesCount
        ev = new Mouse { Position = new Point (0, 3), Flags = MouseFlags.LeftButtonClicked };
        tv.NewMouseEvent (ev);

        // Verify InsertionPoint is set to the last line
        Assert.Equal (new Point (0, 2), tv.InsertionPoint);
    }

    [Fact]
    public void MoveEnd_AdjustsViewportToShowInsertionPoint_When_InsertionPointIsBeyondViewport ()
    {
        using IApplication app = Application.Create ().Init ();
        TextView textView = new () { Width = 5, Height = 5, BorderStyle = LineStyle.Single, App = app };
        textView.Text = "Line1\nLine2\nLine3\nLine4\nLine5";

        textView.BeginInit ();
        textView.EndInit ();

        // Content size only updates after initialization, so it should reflect the size of the text content (5 lines of 6 characters each)
        Assert.Equal (new Size (6, 5), textView.GetContentSize ());

        // Assert initial state - insertion point at (0,0) and viewport shows top-left 3x3 area
        Assert.Equal (Point.Empty, textView.InsertionPoint);
        Assert.Equal (new Rectangle (0, 0, 3, 3), textView.Viewport);

        // Cursor is always screen relative, so it should be at (1,1) initially in viewport coordinates
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);

        textView.Draw ();

        var expected = """
                       ┌───┐
                       │Lin│
                       │Lin│
                       │Lin│
                       └───┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        // Invoke MoveEnd method - should move insertion point to last column of the last line
        Assert.True (textView.MoveEnd ());
        Assert.Equal (new Point (5, 4), textView.InsertionPoint);
        Assert.Equal (new Rectangle (3, 2, 3, 3), textView.Viewport);

        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌───┐
                   │e3 │
                   │e4 │
                   │e5 │
                   └───┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative, so it should be at (3,3) in viewport coordinates
        Assert.Equal (new Point (3, 3), textView.Cursor.Position!.Value);
    }

    [Fact]
    public void MoveHome_AdjustsViewportToShowInsertionPoint_When_InsertionPointIsBeyondViewport ()
    {
        using IApplication app = Application.Create ().Init ();
        TextView textView = new () { Width = 5, Height = 5, BorderStyle = LineStyle.Single, App = app };
        textView.Text = "Line1\nLine2\nLine3\nLine4\nLine5";

        textView.BeginInit ();
        textView.EndInit ();

        // Content size only updates after initialization, so it should reflect the size of the text content (5 lines of 6 characters each)
        Assert.Equal (new Size (6, 5), textView.GetContentSize ());

        // Move insertion point to last column of last line
        textView.InsertionPoint = new Point (5, 4);
        Assert.Equal (new Rectangle (3, 2, 3, 3), textView.Viewport);

        textView.Draw ();

        var expected = """
                       ┌───┐
                       │e3 │
                       │e4 │
                       │e5 │
                       └───┘
                       """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        // Cursor is always screen relative, so it should be at (3,3) in viewport coordinates
        Assert.Equal (new Point (3, 3), textView.Cursor.Position!.Value);

        // Invoke MoveHome method - should move insertion point to first column of first line
        Assert.True (textView.MoveHome ());
        Assert.Equal (Point.Empty, textView.InsertionPoint);
        Assert.Equal (new Rectangle (0, 0, 3, 3), textView.Viewport);

        textView.SetClipToScreen ();
        textView.Draw ();

        expected = """
                   ┌───┐
                   │Lin│
                   │Lin│
                   │Lin│
                   └───┘
                   """;

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);

        textView.PositionCursor ();

        // Cursor is always screen relative, so it should be at (1,1) initially in viewport coordinates
        Assert.Equal (new Point (1, 1), textView.Cursor.Position!.Value);
    }

    private TextView CreateTextView () => new () { Width = 30, Height = 10 };

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_Undo_Redo_Paste_DeleteAll ()
    {
        // Ctrl+Y → Redo (was Paste)
        // Ctrl+V → Paste (was PageDown via Emacs)
        // Ctrl+R → No longer Redo
        // Ctrl+G → No longer DeleteAll
        // Ctrl+Shift+Delete → DeleteAll
        TextView tv = new () { Width = 40, Height = 10, Text = "hello" };
        tv.InsertionPoint = new Point (tv.Text.Length, 0);

        // Ctrl+Z → Undo
        tv.NewKeyDownEvent (Key.Backspace); // delete so undo has something
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl));
        Assert.Equal ("hello", tv.Text);

        // Ctrl+Y → Redo
        Assert.True (tv.NewKeyDownEvent (Key.Y.WithCtrl));
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl)); // undo to restore
        Assert.Equal ("hello", tv.Text);

        // Ctrl+R no longer mapped to Redo
        Assert.False (tv.KeyBindings.TryGet (Key.R.WithCtrl, out _));

        // Ctrl+G no longer mapped to DeleteAll
        Assert.False (tv.KeyBindings.TryGet (Key.G.WithCtrl, out _));

        // Ctrl+Shift+D no longer mapped to DeleteAll
        Assert.False (tv.KeyBindings.TryGet (Key.D.WithCtrl.WithShift, out _));

        // Ctrl+Shift+Delete → DeleteAll (verify binding exists)
        Assert.True (tv.KeyBindings.TryGet (Key.Delete.WithCtrl.WithShift, out KeyBinding deleteAllBinding));
        Assert.Contains (Command.DeleteAll, deleteAllBinding.Commands);
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_CtrlV_Is_Paste_Not_PageDown ()
    {
        // Ctrl+V should map to Paste, not PageDown
        TextView tv = new () { Width = 40, Height = 10 };
        Assert.True (tv.KeyBindings.TryGet (Key.V.WithCtrl, out KeyBinding binding));
        Assert.Contains (Command.Paste, binding.Commands);
        Assert.DoesNotContain (Command.PageDown, binding.Commands);
    }

    // Copilot
    [Fact]
    public void UnifiedKeyBindings_NonWindows_Undo_Redo ()
    {
        if (OperatingSystem.IsWindows ())
        {
            return; // non-Windows-only bindings are not added on Windows
        }

        TextView tv = new () { Width = 40, Height = 10, Text = "hello" };
        tv.InsertionPoint = new Point (tv.Text.Length, 0);

        // Ctrl+/ → Undo
        tv.NewKeyDownEvent (Key.Backspace); // delete so undo has something
        Assert.Equal ("hell", tv.Text);
        Assert.True (tv.NewKeyDownEvent (new Key ('/').WithCtrl));
        Assert.Equal ("hello", tv.Text);

        // Ctrl+Shift+Z → Redo
        Assert.True (tv.NewKeyDownEvent (Key.Z.WithCtrl.WithShift));
        Assert.Equal ("hell", tv.Text);
    }

    [Theory]
    [InlineData (0,
                 """
                 1234123412
                 1   2   3
                 🦮  👨‍👩‍👧‍👦  🍎
                 """)]
    [InlineData (1,
                 """
                 2341234123
                    2   3
                    👨‍👩‍👧‍👦  🍎
                 """)]
    [InlineData (2,
                 """
                 3412341234
                   2   3
                   👨‍👩‍👧‍👦  🍎
                 """)]
    [InlineData (3,
                 """
                 4123412341
                  2   3   4
                  👨‍👩‍👧‍👦  🍎
                 """)]
    [InlineData (4,
                 """
                 1234123412
                 2   3   4
                 👨‍👩‍👧‍👦  🍎  👨‍🚒
                 """)]
    [InlineData (5,
                 """
                 2341234123
                    3   4
                    🍎  👨‍🚒
                 """)]
    [InlineData (6,
                 """
                 3412341234
                   3   4
                   🍎  👨‍🚒
                 """)]
    public void Text_With_Tabs_And_Mixed_Glyphs_Draws_Correctly (int viewportX, string expected)
    {
        using IApplication app = Application.Create ().Init ();
        TextView tv = new () { Width = 10, Height = 3, Driver = app.Driver };
        tv.Text = "1234123412341234\n1\t2\t3\t4\n🦮\t👨‍👩‍👧‍👦\t🍎\t👨‍🚒";
        tv.BeginInit ();
        tv.EndInit ();

        Assert.Equal (4, tv.TabWidth);

        tv.Viewport = tv.Viewport with { X = viewportX };
        tv.Draw ();

        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);
    }

    [Fact]
    public void Text_Start_With_Wide_Glyph_With_ViewportX_Of_One_Draw_Space_In_The_First_Column ()
    {
        using IApplication app = Application.Create ().Init ();
        TextView tv = new () { Width = 10, Height = 3, Driver = app.Driver };
        tv.Text = "1234123412341234\n1\t2\t3\t4\n🦮\t👨‍👩‍👧‍👦\t🍎\t👨‍🚒";
        tv.BeginInit ();
        tv.EndInit ();

        Assert.Equal (4, tv.TabWidth);

        tv.Viewport = tv.Viewport with { X = 1 };
        tv.Draw ();

        Assert.Equal (" ", tv.Driver?.Contents! [2, 0].Grapheme);

        var expected = """
                       2341234123
                          2   3
                          👨‍👩‍👧‍👦  🍎
                       """;
        DriverAssert.AssertDriverContentsAre (expected, output, app.Driver);
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     When Kitty keyboard protocol sets AssociatedText on Alt+letter keys,
    ///     TextView must not insert the text. Alt-modified keys are never text input.
    /// </summary>
    [Fact]
    public void AltKey_With_AssociatedText_Does_Not_Insert_Into_TextView ()
    {
        // Copilot
        TextView tv = new () { Width = 20, Height = 5 };
        tv.SetFocus ();

        // Simulate Alt+T with AssociatedText set by Kitty keyboard protocol which in a real scenario would be set AssociatedText as empty string
        // for Alt+letter keys, with the exception of AltGr+key which would set AssociatedText to "key" (e.g. "€" for AltGr+E)
        Key altT = new (Key.T.WithAlt) { AssociatedText = "" };
        tv.NewKeyDownEvent (altT);

        Assert.Equal ("", tv.Text);
    }

    /// <summary>
    ///     Regression test for https://github.com/tui-cs/Terminal.Gui/issues/4963
    ///     Ctrl-modified keys with AssociatedText must not be inserted as text.
    /// </summary>
    [Fact]
    public void CtrlKey_With_AssociatedText_Does_Not_Insert_Into_TextView ()
    {
        // Copilot
        TextView tv = new () { Width = 20, Height = 5 };
        tv.SetFocus ();

        Key ctrlT = new (Key.T.WithCtrl) { AssociatedText = "t" };
        tv.NewKeyDownEvent (ctrlT);

        Assert.Equal ("", tv.Text);
    }
}
