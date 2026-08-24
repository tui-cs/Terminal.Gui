# Restore Pre-#4949 ANSI Input Behavior

## Problem Statement

Windows Terminal previously emitted some Portuguese keyboard input as duplicated legacy characters while Kitty keyboard mode was enabled. Terminal.Gui expanded #4927's mixed Kitty-plus-legacy suppression through the #4949/#4977 raw-fallback workaround, which later evolved into a 50 ms suppression window. Windows Terminal fixed the underlying Kitty keyboard encoding defect, so Terminal.Gui can restore the behavior immediately before #4949 without the timing heuristic.

## Implementation Steps

1. Replace the timed ANSI printable-suppression state with the pre-#4949 single pending-printable state.
2. Arm suppression only after an unmodified printable parsed key press, compare only the next fallback key, and never arm suppression from fallback input.
3. Retain the protected input-pipeline clock accessor for compatibility, although suppression no longer uses time.
4. Carry the matching parser pattern through the internal input pipeline so only Kitty CSI-u input can arm suppression; legacy sequences such as Shift+Tab must remain lossless.
5. Require the fallback duplicate to be adjacent in the raw input stream so intervening keyboard, mouse, paste, or response input invalidates suppression.
6. Update pipeline regressions to verify time-independent mixed-input suppression, Portuguese Kitty-plus-legacy input, lossless repeated legacy input, Shift+Tab followed by Tab, and intervening parser input.

## File Changes

- `Terminal.Gui/Drivers/AnsiDriver/AnsiInputProcessor.cs`: remove timed suppression and restore single-use pending suppression.
- `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseParserBase.cs`: expose parser-pattern provenance to the internal input pipeline.
- `Terminal.Gui/Drivers/Input/InputProcessorImpl.cs`: retain the protected clock accessor and route parser-pattern provenance through the parser/fallback hooks.
- `Tests/UnitTestsParallelizable/Drivers/AnsiHandling/KittyKeyboardPipelineTests.cs`: update and add focused regression coverage.

## Verification

1. Run the focused `KittyKeyboardPipelineTests` class.
2. Build the solution in Debug configuration.
3. Run the full parallelizable unit-test project.
4. Run the full non-parallelizable unit-test project.
