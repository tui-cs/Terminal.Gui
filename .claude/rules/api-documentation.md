# API Documentation Requirements

**Source:** [CONTRIBUTING.md - API Documentation Requirements](../../CONTRIBUTING.md#api-documentation-requirements)

## XML Documentation Rules

- **All public APIs MUST have XML documentation:**

### Required Elements

1. **`<summary>` tag** - Clear, concise description
   - Use proper English and grammar
   - Clear, concise, complete
   - Use imperative mood ("Gets the value" not "Get the value")
   - Use infinitive-of-purpose + imperative pattern for instructional sentences

2. **`<see cref=""/>` for cross-references**
   - Link to related types, methods, properties
   - Example: `<see cref="View.Draw"/>` or `<see cref="Application"/>`

3. **`<remarks>` for context**
   - Add only if obviously essential to do so
   - Add important notes, warnings, or additional context
   - Explain non-obvious behavior

5. **`<example>` for non-obvious usage**
   - Include code examples ONLY when usage isn't immediately clear
   - Use working, compilable code snippets that strictly adhear to the coding/syntax style

6. **Complex topics → `docfx/docs/*.md` files**
   - For architecture concepts, patterns, or deep dives
   - Link from XML docs to conceptual docs

## Documentation Style

### Imperative Mood
```csharp
// ✅ CORRECT
/// <summary>Gets or sets the width of the view.</summary>

// ❌ WRONG
/// <summary>Get or set the width of the view.</summary>
```

### Clear Cross-References
```csharp
// ✅ CORRECT
/// <summary>
/// Draws the view. See <see cref="OnDrawContent"/> for customizing the drawing behavior.
/// </summary>

// ❌ WRONG
/// <summary>Draws the view. Override OnDrawContent to customize.</summary>
```

### Examples for Complex APIs
```csharp
/// <summary>
/// Creates a new <see cref="Dialog"/> with the specified buttons.
/// </summary>
/// <example>
/// <code>
/// Dialog dialog = new () {
///     Title = "Confirm",
///     Buttons = [new Button ("OK"), new Button ("Cancel")]
/// };
/// Application.Run (dialog);
/// </code>
/// </example>
```

## When to Create Conceptual Docs

Create a new file in `docfx/docs/` when:

- Explaining architecture concepts (e.g., `application.md`, `layout.md`)
- Documenting patterns (e.g., `cancellable-work-pattern.md`)
- Providing tutorials or guides
- Content is too long for XML documentation

Link from XML docs to conceptual docs:
```csharp
/// <summary>
/// Manages the application lifecycle. See the 
/// <a href="../docs/application.md">Application Deep Dive</a> for details.
/// </summary>
```

## Documentation is the Spec

**Code Style Tenet #5:** Documentation is the specification.

- API documentation defines the contract
- Implementation must match documentation
- When docs and code conflict, fix the code or update the docs
- Keep documentation synchronized with code changes
