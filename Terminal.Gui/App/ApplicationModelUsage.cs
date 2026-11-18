namespace Terminal.Gui.App;

/// <summary>
///     Defines the different application usage models.
/// </summary>
public enum ApplicationModelUsage
{
    /// <summary>No model has been used yet.</summary>
    None,

    /// <summary>Legacy static model (Application.Init/ApplicationImpl.Instance).</summary>
    LegacyStatic,

    /// <summary>Modern instance-based model (Application.Create).</summary>
    InstanceBased
}
