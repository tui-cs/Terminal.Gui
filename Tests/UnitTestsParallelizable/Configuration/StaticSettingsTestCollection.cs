namespace ConfigurationTests;

[CollectionDefinition ("StaticSettingsTests", DisableParallelization = true)]
public class StaticSettingsTestCollection
{
    // Marker collection for tests that mutate process-wide static settings facades
    // (e.g. DriverSettings.Defaults, PopoverMenuSettings.Defaults). Without
    // DisableParallelization, these tests race views constructed in parallel tests —
    // e.g. temporarily making PopoverMenu.DefaultKey read Ctrl+P while a FileDialog
    // is being constructed elsewhere.
}
