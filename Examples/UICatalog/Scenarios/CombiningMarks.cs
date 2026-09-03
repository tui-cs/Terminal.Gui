
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Combining Marks", "Illustrates how Unicode Combining Marks work (or don't).")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Unicode")]
public class CombiningMarks : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        using Runnable top = new ();

        top.DrawingContent += (_, _) =>
        {
            int i = -1;
            top.Move (0, ++i);
            top.AddStr ("Terminal.Gui supports all combining sequences that can be rendered as an unique grapheme.");
            top.Move (0, ++i);
            top.AddStr ("\u0301<- \"\\u0301\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301]<- \"[\\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[ \u0301]<- \"[ \\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301 ]<- \"[\\u0301 ]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u0301\u0301\u0328<- \"\\u0301\\u0301\\u0328\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0301\u0301\u0328]<- \"[\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddRune ('[');
            top.AddRune ('a');
            top.AddRune ('\u0301');
            top.AddRune ('\u0301');
            top.AddRune ('\u0328');
            top.AddRune (']');
            top.AddStr ("<- \"[a\\u0301\\u0301\\u0328]\" using AddRune for each. Avoid use AddRune for combining sequences because may result with empty blocks at end.");
            top.Move (0, ++i);
            top.AddStr ("[a\u0301\u0301\u0328]<- \"[a\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u00ad<- \"\\u00ad\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u00ad]<- \"[\\u00ad]\" using AddStr.");
            top.Move (0, ++i);
            top.AddRune ('[');
            top.AddRune ('\u00ad');
            top.AddRune (']');
            top.AddStr ("<- \"[\\u00ad]\" using AddRune for each.");
            i++;
            top.Move (0, ++i);
            top.AddStr ("From now on we are using TextFormatter");
            TextFormatter tf = new () { Text = "[e\u0301\u0301\u0328]<- \"[e\\u0301\\u0301\\u0328]\" using TextFormatter." };
            tf.Draw (driver: app.Driver, screen: new (0, ++i, tf.Text.Length, 1), normalColor: top.GetAttributeForRole (VisualRole.Normal), hotColor: top.GetAttributeForRole (VisualRole.Normal));
            tf.Text = "[e\u0328\u0301]<- \"[e\\u0328\\u0301]\" using TextFormatter.";
            tf.Draw (driver: app.Driver, screen: new (0, ++i, tf.Text.Length, 1), normalColor: top.GetAttributeForRole (VisualRole.Normal), hotColor: top.GetAttributeForRole (VisualRole.Normal));
            i++;
            top.Move (0, ++i);
            top.AddStr ("From now on we are using Surrogate pairs with combining diacritics");
            top.Move (0, ++i);
            top.AddStr ("[\ud835\udc4b\u0302]<- \"[\\ud835\\udc4b\\u0302]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\ud83d\udc68\ud83e\uddd2]<- \"[\\ud83d\\udc68\\ud83e\\uddd2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("\u200d<- \"\\u200d\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u200d]<- \"[\\u200d]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\ud83d\udc68\u200d\ud83e\uddd2]<- \"[\\ud83d\\udc68\\u200d\\ud83e\\uddd2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F469\U0001F9D2]<- \"[\\U0001F469\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F469\u200D\U0001F9D2]<- \"[\\U0001F469\\u200D\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F468\U0001F469\U0001F9D2]<- \"[\\U0001F468\\U0001F469\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F468\u200D\U0001F469\u200D\U0001F9D2]<- \"[\\U0001F468\\u200D\\U0001F469\\u200D\\U0001F9D2]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466]<- \"[\\U0001F468\\u200D\\U0001F469\\u200D\\U0001F467\\u200D\\U0001F466]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0e32\u0e33]<- \"[\\u0e32\\u0e33]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\U0001F469\u200D\u2764\uFE0F\u200D\U0001F48B\u200D\U0001F468]<- \"[\\U0001F469\\u200D\\u2764\\uFE0F\\u200D\\U0001F48B\\u200D\\U0001F468]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u0061\uFE20\u0065\uFE21]<- \"[\\u0061\\uFE20\\u0065\\uFE21]\" using AddStr.");
            top.Move (0, ++i);
            top.AddStr ("[\u1100\uD7B0]<- \"[\\u1100\\uD7B0]\" using AddStr.");
        };

        app.Run (top);
    }
}
