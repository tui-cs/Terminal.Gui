namespace Terminal.Gui.Text;

/// <summary>Text direction enumeration, controls how text is displayed.</summary>
/// <remarks>
///     <para>TextDirection  [H] = Horizontal  [V] = Vertical</para>
///     <table>
///         <tr>
///             <th>TextDirection</th> <th>Description</th>
///         </tr>
///         <tr>
///             <td>LeftRight_TopBottom [H]</td> <td>Normal</td>
///         </tr>
///         <tr>
///             <td>TopBottom_LeftRight [V]</td> <td>Normal</td>
///         </tr>
///         <tr>
///             <td>RightLeft_TopBottom [H]</td> <td>Invert Text</td>
///         </tr>
///         <tr>
///             <td>TopBottom_RightLeft [V]</td> <td>Invert Lines</td>
///         </tr>
///         <tr>
///             <td>LeftRight_BottomTop [H]</td> <td>Invert Lines</td>
///         </tr>
///         <tr>
///             <td>BottomTop_LeftRight [V]</td> <td>Invert Text</td>
///         </tr>
///         <tr>
///             <td>RightLeft_BottomTop [H]</td> <td>Invert Text + Invert Lines</td>
///         </tr>
///         <tr>
///             <td>BottomTop_RightLeft [V]</td> <td>Invert Text + Invert Lines</td>
///         </tr>
///     </table>
/// </remarks>
public enum TextDirection
{
    /// <summary>Normal horizontal direction. <code>HELLO<br/>WORLD</code></summary>
    LeftRight_TopBottom,

    /// <summary>Normal vertical direction. <code>H W<br/>E O<br/>L R<br/>L L<br/>O D</code></summary>
    TopBottom_LeftRight,

    /// <summary>This is a horizontal direction. <br/> RTL <code>OLLEH<br/>DLROW</code></summary>
    RightLeft_TopBottom,

    /// <summary>This is a vertical direction. <code>W H<br/>O E<br/>R L<br/>L L<br/>D O</code></summary>
    TopBottom_RightLeft,

    /// <summary>This is a horizontal direction. <code>WORLD<br/>HELLO</code></summary>
    LeftRight_BottomTop,

    /// <summary>This is a vertical direction. <code>O D<br/>L L<br/>L R<br/>E O<br/>H W</code></summary>
    BottomTop_LeftRight,

    /// <summary>This is a horizontal direction. <code>DLROW<br/>OLLEH</code></summary>
    RightLeft_BottomTop,

    /// <summary>This is a vertical direction. <code>D O<br/>L L<br/>R L<br/>O E<br/>W H</code></summary>
    BottomTop_RightLeft
}
