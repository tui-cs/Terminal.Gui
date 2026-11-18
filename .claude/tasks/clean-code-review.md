# Clean Git Commit History Workflow

Reimplement the current branch on a new branch with a clean, narrative-quality git commit history suitable for reviewer comprehension.

## Steps

1. **Validate the source branch**
   - Ensure the current branch has no merge conflicts, uncommitted changes, or other issues.
   - Confirm it is up to date with `develop`.

2. **Analyze the diff**
   - Study all changes between the current branch and `develop`.
   - Form a clear understanding of the final intended state.

3. **Create the clean branch**
   - Create a new branch named `{branch_name}-clean` from the current branch.

4. **Plan the commit storyline**
   - Break the implementation down into a sequence of self-contained steps.
   - Each step should reflect a logical stage of development—as if writing a tutorial.

5. **Reimplement the work**
   - Recreate the changes in the clean branch, committing step by step according to your plan.
   - Each commit must:
     - Introduce a single coherent idea.
     - Include a clear commit message and description.
     - Add comments or inline code comments when needed to explain intent.
     - Follow Terminal.Gui commit message conventions (see CONTRIBUTING.md).
     - **Pass formatting validation** (see POST-GENERATION-VALIDATION.md).

6. **Verify correctness**
   - Confirm that the final state of `{branch_name}-clean` exactly matches the final state of the original branch.
   - Use `--no-verify` only when necessary (e.g., to bypass known issues). Individual commits do not need to pass tests, but this should be rare.

7. **Open a pull request**
   - Create a PR from the clean branch to `develop`.
   - Follow Terminal.Gui PR guidelines (see CONTRIBUTING.md).
   - Include a link to the original branch in the PR description.

## Important Notes

- **Each commit must run all tests**: Integration tests, unit tests, and parallelizable unit tests must be executed for every commit. While individual commits do not strictly need to *pass* all tests (this should be exceptional), the tests must be *run* to verify the commit's impact. Use `--no-verify` only when necessary to bypass known issues.
- It is essential that the end state of your new branch be identical to the end state of the source branch.
- Follow all Terminal.Gui coding conventions (see `.claude/rules/`).
- Ensure XML documentation is updated for any API changes (see `.claude/rules/api-documentation.md`).

## Test Commands

Run these for each commit:

```bash
dotnet build --no-restore
dotnet test --project Tests/IntegrationTests --no-build
dotnet test --project Tests/UnitTestsParallelizable --no-build
dotnet test --project Tests/UnitTests.NonParallelizable --no-build
```

## Terminal.Gui Specific Requirements

When creating commits for Terminal.Gui:

1. **Code style**
   - No `var` except for built-in types (see `.claude/rules/type-declarations.md`)
   - Use `new ()` for target-typed new (see `.claude/rules/target-typed-new.md`)
   - Use `[...]` for collections (see `.claude/rules/collection-expressions.md`)
   - Use SubView/SuperView terminology (see `.claude/rules/terminology.md`)

2. **Commit messages**
   - Follow the project's commit message style
   - Include Co-Authored-By line per CONTRIBUTING.md guidelines
   - Keep messages clear and descriptive

3. **Testing**
   - Add tests to `UnitTestsParallelizable` when possible
   - Never decrease code coverage
   - Follow patterns in `.claude/rules/testing-patterns.md`
