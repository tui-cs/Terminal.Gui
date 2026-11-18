#nullable enable

using System.Collections;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;
using UnitTests;

namespace ResourcesTests;

public class ResourceManagerTests : TestDriverBase
{
    private const string EXISTENT_CULTURE = "pt-PT";
    private const string NO_EXISTENT_CULTURE = "de-DE";
    private const string NO_EXISTENT_KEY = "blabla";
    private const string NO_TRANSLATED_KEY = "fdDeleteTitle";
    private const string NO_TRANSLATED_VALUE = "Delete {0}";
    private const string TRANSLATED_KEY = "cmdSelectAll";
    private const string TRANSLATED_VALUE = "_Selecionar Tudo";


    [ModuleInitializer]
    internal static void SaveOriginalCultureInfo ()
    {
        _savedCulture = CultureInfo.CurrentCulture;
        _savedUICulture = CultureInfo.CurrentUICulture;
    }

    private static CultureInfo? _savedCulture;
    private static CultureInfo? _savedUICulture;
    private static string? _stringsNoTranslatedKey;
    // ReSharper disable once NotAccessedField.Local
    private static string? _stringsTranslatedKey;

    [Fact]
    public void GetObject_Does_Not_Overflows_If_Key_Does_Not_Exist () { Assert.Null (GlobalResources.GetObject (NO_EXISTENT_KEY, CultureInfo.CurrentCulture)); }

    [Fact]
    public void GetObject_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetObject (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        ResetCultureInfo ();
    }

    [Fact]
    public void GetObject_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetObject (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        ResetCultureInfo ();
    }

    [Fact]
    public void GetResourceSet_With_Filter_Does_Not_Overflows_If_Key_Does_Not_Exist ()
    {
        ResourceSet value = GlobalResources.GetResourceSet (CultureInfo.CurrentCulture, true, true, d => (string)d.Key == NO_EXISTENT_KEY)!;
        Assert.NotNull (value);
        Assert.Empty (value.Cast<DictionaryEntry> ());
    }

    [Fact]
    public void GetResourceSet_Without_Filter_Does_Not_Overflows_If_Key_Does_Not_Exist ()
    {
        ResourceSet value = GlobalResources.GetResourceSet (CultureInfo.CurrentCulture, true, true)!;
        Assert.NotNull (value);
        Assert.NotEmpty (value.Cast<DictionaryEntry> ());
    }

    [Fact]
    public void GetString_Does_Not_Overflows_If_Key_Does_Not_Exist ()
    {
        Assert.Null (GlobalResources.GetString (NO_EXISTENT_KEY, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void GetString_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetString (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        ResetCultureInfo ();
    }

    [Fact]
    public void GetString_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (EXISTENT_CULTURE);

        // This is really already translated
        Assert.Equal (TRANSLATED_VALUE, GlobalResources.GetString (TRANSLATED_KEY, CultureInfo.CurrentCulture));

        // These aren't already translated
        // Calling Strings.fdDeleteBody return always the invariant culture
        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetString (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));

        ResetCultureInfo ();
    }

    [Fact]
    public void Strings_Always_FallBack_To_Default_For_No_Existent_Culture_File ()
    {
        CultureInfo.CurrentCulture = new (NO_EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (NO_EXISTENT_CULTURE);

        Assert.Equal (NO_TRANSLATED_VALUE, GlobalResources.GetString (NO_TRANSLATED_KEY, CultureInfo.CurrentCulture));
        
        ResetCultureInfo ();

        Assert.Equal (NO_TRANSLATED_VALUE, _stringsNoTranslatedKey);
    }

    [Fact]
    public void Strings_Always_FallBack_To_Default_For_Not_Translated_Existent_Culture_File ()
    {
        ResetCultureInfo ();

        CultureInfo.CurrentCulture = new (EXISTENT_CULTURE);
        CultureInfo.CurrentUICulture = new (EXISTENT_CULTURE);

        // This is really already translated
        Assert.Equal (TRANSLATED_VALUE, Strings.cmdSelectAll);

        // This isn't already translated
        Assert.Equal (NO_TRANSLATED_VALUE, _stringsNoTranslatedKey);

        ResetCultureInfo ();
    }

    private void ResetCultureInfo ()
    {
        CultureInfo.CurrentCulture = _savedCulture!;
        CultureInfo.CurrentUICulture = _savedUICulture!;

        _stringsNoTranslatedKey = Strings.fdDeleteTitle;
        _stringsTranslatedKey = Strings.cmdSelectAll;
    }
}
