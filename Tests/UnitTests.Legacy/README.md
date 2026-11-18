# UnitTests.Legacy

This project contains tests that have not yet been ported to `UnitTestsParallelizable` or `UnitTests.NonParallelizable`.

**Do not add new tests here.** New tests should go in:
- [`../UnitTestsParallelizable`](../UnitTestsParallelizable/README.md) for tests with no static state dependency.
- [`../UnitTests.NonParallelizable`](../UnitTests.NonParallelizable/README.md) for tests that explicitly depend on process-wide static state.

Each test class in this project is a candidate for one of:
1. **Rewrite** in the appropriate project if the case is not already covered.
2. **Deletion** if the case is already covered in `UnitTestsParallelizable`.

See the [Testing wiki](https://github.com/tui-cs/Terminal.Gui/wiki/Testing) for details.
