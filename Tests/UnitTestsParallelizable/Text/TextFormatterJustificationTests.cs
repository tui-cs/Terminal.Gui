using System.Text;
using UnitTests;

// Alias Console to MockConsole so we don't accidentally use Console

namespace TextTests;

public class TextFormatterJustificationTests (ITestOutputHelper output) : TestDriverBase
{
    [Theory]

    // Horizontal with Alignment.Start
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // Horizontal with Alignment.End
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 2 4**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
**0 2 4")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*0 2 4*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0  2  4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*0 你 4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
0  你 4")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 2 4**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
**0 2 4")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*0 2 4*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0  2  4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*0 你 4")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0 你 4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
0  你 4")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 2 0**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
**4 2 0")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*4 2 0*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4  2  0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
*4 你 0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*******
*******
*******
4  你 0")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 2 0**")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
**4 2 0")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*4 2 0*")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4  2  0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
*4 你 0")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4 你 0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*******
*******
*******
4  你 0")]

    // Horizontal with alignment.Centered
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 2 4**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
**0 2 4
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*0 2 4*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0  2  4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
*0 你 4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_TopBottom,
                    @"
*******
*******
*******
0  你 4
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 2 4**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
**0 2 4
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*0 2 4*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0  2  4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
*0 你 4
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0 你 4*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.LeftRight_BottomTop,
                    @"
*******
*******
*******
0  你 4
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 2 0**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
**4 2 0
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*4 2 0*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4  2  0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
*4 你 0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_TopBottom,
                    @"
*******
*******
*******
4  你 0
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 2 0**
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
**4 2 0
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*4 2 0*
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4  2  0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
*4 你 0
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4 你 0*
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.RightLeft_BottomTop,
                    @"
*******
*******
*******
4  你 0
*******
*******
*******")]

    // Horizontal with alignment.Justified
    // LeftRight_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_TopBottom,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // LeftRight_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 2 4**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
**0 2 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 2 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  2  4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
*0 你 4
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0 你 4*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.LeftRight_BottomTop,
                    @"
0  你 4
*******
*******
*******
*******
*******
*******")]

    // RightLeft_TopBottom
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_TopBottom,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // RightLeft_BottomTop
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 2 0**
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
**4 2 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 2 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  2  0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
*4 你 0
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4 你 0*
*******
*******
*******
*******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.RightLeft_BottomTop,
                    @"
4  你 0
*******
*******
*******
*******
*******
*******")]

    // Vertical with alignment.Left
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Start,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // Vertical with alignment.Right
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
******0
****** 
******2
****** 
******4
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
******0
****** 
******2
****** 
******4")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
******0
****** 
******2
****** 
******4
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
******0
****** 
****** 
******2
****** 
****** 
******4")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
*****0*
***** *
*****你
***** *
*****4*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
*****0*
***** *
*****你
***** *
*****4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*****0*
***** *
*****你
***** *
*****4*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
*****0*
***** *
***** *
*****你
***** *
***** *
*****4*")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
******0
****** 
******2
****** 
******4
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
******0
****** 
******2
****** 
******4")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
******0
****** 
******2
****** 
******4
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
******0
****** 
****** 
******2
****** 
****** 
******4")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
*****0*
***** *
*****你
***** *
*****4*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
*****0*
***** *
*****你
***** *
*****4*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*****0*
***** *
*****你
***** *
*****4*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
*****0*
***** *
***** *
*****你
***** *
***** *
*****4*")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
******4
****** 
******2
****** 
******0
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
******4
****** 
******2
****** 
******0")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
******4
****** 
******2
****** 
******0
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
******4
****** 
****** 
******2
****** 
****** 
******0")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
*****4*
***** *
*****你
***** *
*****0*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
*****4*
***** *
*****你
***** *
*****0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*****4*
***** *
*****你
***** *
*****0*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
*****4*
***** *
***** *
*****你
***** *
***** *
*****0*")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
******4
****** 
******2
****** 
******0
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
******4
****** 
******2
****** 
******0")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
******4
****** 
******2
****** 
******0
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
******4
****** 
****** 
******2
****** 
****** 
******0")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
*****4*
***** *
*****你
***** *
*****0*
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
*****4*
***** *
*****你
***** *
*****0*")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*****4*
***** *
*****你
***** *
*****0*
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.End,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
*****4*
***** *
***** *
*****你
***** *
***** *
*****0*")]

    // Vertical with alignment.Centered
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
***0***
*** ***
***2***
*** ***
***4***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
***0***
*** ***
***2***
*** ***
***4***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
***0***
*** ***
***2***
*** ***
***4***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
***0***
*** ***
*** ***
***2***
*** ***
*** ***
***4***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
**0****
** ****
**你***
** ****
**4****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
**0****
** ****
**你***
** ****
**4****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
**0****
** ****
**你***
** ****
**4****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
**0****
** ****
** ****
**你***
** ****
** ****
**4****")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
***0***
*** ***
***2***
*** ***
***4***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
***0***
*** ***
***2***
*** ***
***4***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
***0***
*** ***
***2***
*** ***
***4***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
***0***
*** ***
*** ***
***2***
*** ***
*** ***
***4***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
**0****
** ****
**你***
** ****
**4****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
**0****
** ****
**你***
** ****
**4****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
**0****
** ****
**你***
** ****
**4****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
**0****
** ****
** ****
**你***
** ****
** ****
**4****")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
***4***
*** ***
***2***
*** ***
***0***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
***4***
*** ***
***2***
*** ***
***0***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
***4***
*** ***
***2***
*** ***
***0***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
***4***
*** ***
*** ***
***2***
*** ***
*** ***
***0***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
**4****
** ****
**你***
** ****
**0****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
**4****
** ****
**你***
** ****
**0****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
**4****
** ****
**你***
** ****
**0****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
**4****
** ****
** ****
**你***
** ****
** ****
**0****")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
***4***
*** ***
***2***
*** ***
***0***
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
***4***
*** ***
***2***
*** ***
***0***")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
***4***
*** ***
***2***
*** ***
***0***
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
***4***
*** ***
*** ***
***2***
*** ***
*** ***
***0***")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
**4****
** ****
**你***
** ****
**0****
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
**4****
** ****
**你***
** ****
**0****")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
**4****
** ****
**你***
** ****
**0****
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Center,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
**4****
** ****
** ****
**你***
** ****
** ****
**0****")]

    // Vertical with alignment.Justified
    // TopBottom_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_LeftRight,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_LeftRight,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // TopBottom_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
2******
 ******
4******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
2******
 ******
4******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
2******
 ******
4******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
2******
 ******
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
你*****
 ******
4******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
*******
0******
 ******
你*****
 ******
4******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.TopBottom_RightLeft,
                    @"
*******
0******
 ******
你*****
 ******
4******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.TopBottom_RightLeft,
                    @"
0******
 ******
 ******
你*****
 ******
 ******
4******")]

    // BottomTop_LeftRight
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_LeftRight,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_LeftRight,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]

    // BottomTop_RightLeft
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
2******
 ******
0******
*******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
2******
 ******
0******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
2******
 ******
0******
*******")]
    [InlineData (
                    "0 2 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
2******
 ******
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Start,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
你*****
 ******
0******
*******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.End,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
*******
4******
 ******
你*****
 ******
0******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Center,
                    TextDirection.BottomTop_RightLeft,
                    @"
*******
4******
 ******
你*****
 ******
0******
*******")]
    [InlineData (
                    "0 你 4",
                    Alignment.Fill,
                    Alignment.Fill,
                    TextDirection.BottomTop_RightLeft,
                    @"
4******
 ******
 ******
你*****
 ******
 ******
0******")]
    public void Draw_Text_Justification (string text, Alignment horizontalTextAlignment, Alignment alignment, TextDirection textDirection, string expectedText)
    {
        IDriver driver = CreateTestDriver ();

        TextFormatter tf = new ()
        {
            Alignment = horizontalTextAlignment,
            VerticalAlignment = alignment,
            Direction = textDirection,
            ConstrainToSize = new (7, 7),
            Text = text
        };

        driver.FillRect (new (0, 0, 7, 7), (Rune)'*');
        tf.Draw (driver: driver, screen: new (0, 0, 7, 7), normalColor: Attribute.Default, hotColor: Attribute.Default);
        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, output, driver);
    }
}
