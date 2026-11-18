# Unicode and Grapheme Handling

**Source:** [CONTRIBUTING.md - Unicode and Grapheme Handling](../../CONTRIBUTING.md#unicode-and-grapheme-handling)

## Core Principle

**Think in graphemes, not runes.** A grapheme cluster is what the user perceives as a single character, but it may consist of multiple `Rune` values (e.g., base character + combining marks, or ZWJ emoji sequences).

## Width Measurement

**Always use `string.GetColumns()`** to measure display width.

```csharp
// ✅ CORRECT — grapheme-aware, handles ZWJ emoji, combining marks, CJK
int width = text.GetColumns ();

// ❌ WRONG — sums individual rune widths, inflates multi-rune grapheme clusters
int width = text.EnumerateRunes ().Sum (r => r.GetColumns ());

// ❌ WRONG — counts chars, not terminal cells (wrong for CJK, emoji, surrogates)
int width = text.Length;
```

**Why:** A ZWJ family emoji like 👨‍👩‍👦‍👦 occupies 2 terminal cells, but `EnumerateRunes().Sum(GetColumns)` returns 8 and `string.Length` returns 11. `string.GetColumns()` correctly returns 2 by iterating grapheme clusters and clamping each to max 2 columns.

## Text Iteration and Rendering

**Iterate by grapheme cluster** using `GraphemeHelper.GetGraphemes()` and render with `AddStr`.

```csharp
// ✅ CORRECT — preserves grapheme cluster integrity
foreach (string grapheme in GraphemeHelper.GetGraphemes (text))
{
    AddStr (grapheme);
}

// ❌ WRONG — breaks combining marks, ZWJ sequences
foreach (Rune rune in text.EnumerateRunes ())
{
    AddRune (rune);
}
```

**Why:** Combining marks (e.g., `e` + U+0301 combining acute = `é`) must be sent together. `AddRune` sends them separately, preventing composition. `AddStr` with a complete grapheme string preserves the cluster.

## When Rune Iteration Is Appropriate

Rune-level iteration is correct when you need to inspect individual Unicode scalar values rather than render text — for example, counting zero-width runes for vertical text layout, or checking character properties. The key distinction: **rendering and measurement should use graphemes; analysis may use runes.**
