---
name: Bug report
about: Create a report to help us improve Terminal.Gui
title: ''
labels: bug
assignees: ''

---

## Describe the bug

A clear and concise description of what the bug is.

## To Reproduce

Steps to reproduce the behavior:

1. Run the following code:
   ```csharp
   // Paste your minimal reproduction code here
   ```

2. Expected behavior: (describe what should happen)

3. Actual behavior: (describe what actually happens)

## Environment

Please run the following commands in your terminal and paste the output:

### OS Information

**Windows (PowerShell):**
```powershell
"OS: $(Get-CimInstance Win32_OperatingSystem | Select-Object -ExpandProperty Caption) $(Get-CimInstance Win32_OperatingSystem | Select-Object -ExpandProperty Version)"
```

**macOS/Linux:**
```bash
echo "OS: $(uname -s) $(uname -r)"
```

**Output:**
```
(paste output here)
```

### Terminal Information

**Windows Terminal:**
```powershell
"Terminal: Windows Terminal $(Get-AppxPackage -Name Microsoft.WindowsTerminal | Select-Object -ExpandProperty Version)"
```

**Other terminals:**
```bash
echo $TERM
```

**Output:**
```
(paste output here)
```

### PowerShell Version

```powershell
$PSVersionTable.PSVersion
```

**Output:**
```
(paste output here)
```

### .NET Information

```bash
dotnet --version
dotnet --info
```

**Output:**
```
(paste output here)
```

### Terminal.Gui Version

**Option 1 - Run UICatalog (easiest):**

UICatalog displays the Terminal.Gui version in its About box and status bar.

```bash
dotnet run --project Examples/UICatalog/UICatalog.csproj
```

**Option 2 - NuGet Package Version:**
```
(e.g., 2.0.0-alpha.1, 2.0.0-develop.123, etc.)
```

**Option 3 - Building from source:**
```bash
git rev-parse HEAD
git describe --tags --always --dirty
```

**Version:**
```
(paste version here)
```

## Screenshots, GIFs, or Terminal Output

If applicable, add screenshots, animated GIFs, or copy/paste terminal output to help explain your problem.

**Animated GIFs are especially helpful for showing behavior!**

- **Windows**: [ShareX](https://getsharex.com/) (free, captures screen to GIF)
- **macOS**: [Kap](https://getkap.co/) (free, open source)
- **Linux**: [Peek](https://github.com/phw/peek) (free)

**For terminal output, use code blocks:**

```
(paste terminal output here)
```

## Additional context

Add any other context about the problem here, such as:
- Does this happen consistently or intermittently?
- Did this work in a previous version?
- Are there any error messages in the console?
- Terminal configuration or settings that might be relevant?

## For Maintainers

**Set Project & Milestone:** If you have access, please don't forget to set the right Project and Milestone.
