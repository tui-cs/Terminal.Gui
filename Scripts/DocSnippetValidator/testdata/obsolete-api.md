# Negative-test fixture: obsolete v1 API must be rejected

This file is **not** part of the documented snippets. It exists so the validator's
own behavior can be regression-tested: the block below uses the obsolete v1 static
`Application` API (`CS0618`) and is **not** marked `// WRONG`, so a correct validator
must reject it. See `Scripts/DocSnippetValidator/README.md`.

```csharp
Application.Init ();
Application.Shutdown ();
```
