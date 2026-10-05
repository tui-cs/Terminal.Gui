using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Terminal.Gui.Analyzers;
using Xunit;

namespace Terminal.Gui.Analyzers.Tests
{
    public class V1ApiDiagnosticAnalyzerTests
    {
        private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("TestAssembly",
                new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var analyzer = new V1ApiDiagnosticAnalyzer();
            var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
            return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }

        [Fact]
        public void TestAnalyzerSupportedDiagnostics()
        {
            var analyzer = new V1ApiDiagnosticAnalyzer();
            Assert.Equal(14, analyzer.SupportedDiagnostics.Length);
        }

        [Fact]
        public async Task TestClickedTriggersDiagnostic()
        {
            var source = @"
class Test {
    void Demo(dynamic btn) {
        btn.Clicked += null;
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleClicked.Id);
        }

        [Fact]
        public async Task TestStaticApplicationTriggersDiagnostic()
        {
            var source = @"
class Application { public static void Init() {} }
class Test {
    void Demo() {
        Application.Init();
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleApplicationStatic.Id);
        }

        [Fact]
        public async Task TestToplevelTriggersDiagnostic()
        {
            var source = @"
class Toplevel {}
class Test {
    Toplevel top;
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleToplevel.Id);
        }

        [Fact]
        public async Task TestPositionalConstructorTriggersDiagnostic()
        {
            var source = @"
class Button { public Button(string text) {} }
class Test {
    void Demo() {
        var b = new Button(""OK"");
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RulePositionalConstructor.Id);
        }

        [Fact]
        public async Task TestTabViewTriggersDiagnostic()
        {
            var source = @"
class TabView {}
class Test {
    void Demo() {
        var tv = new TabView();
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleTabView.Id);
        }

        [Fact]
        public async Task TestRadioGroupTriggersDiagnostic()
        {
            var source = @"
class RadioGroup {}
class Test {
    void Demo() {
        var rg = new RadioGroup();
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleRadioGroup.Id);
        }

        [Fact]
        public async Task TestKeyBitmaskTriggersDiagnostic()
        {
            var source = @"
enum KeyCode { A, CtrlMask }
class Test {
    void Demo() {
        var k = KeyCode.A | KeyCode.CtrlMask;
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleKeyBitmask.Id);
        }

        [Fact]
        public async Task TestAcceptingIgnoredArgsTriggersDiagnostic()
        {
            var source = @"
class View { public event System.EventHandler Accepting; }
class Test {
    void Demo(View view) {
        view.Accepting += (_, _) => { };
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleAcceptingIgnoresArgs.Id);
        }

        [Fact]
        public async Task TestMenuItem6ArgCtorTriggersDiagnostic()
        {
            var source = @"
class MenuItem { public MenuItem(string a, string b, System.Action c, System.Func<bool> d, object e, object f) {} }
class Test {
    void Demo() {
        var mi = new MenuItem(""a"", ""b"", null, null, null, null);
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleMenuItem6ArgCtor.Id);
        }

        [Fact]
        public async Task TestLayoutStyleTriggersDiagnostic()
        {
            var source = @"
class LayoutStyle { public static object Computed; }
class Test {
    void Demo() {
        var style = LayoutStyle.Computed;
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RuleLayoutStyle.Id);
        }

        [Fact]
        public async Task TestPosAtTriggersDiagnostic()
        {
            var source = @"
class Pos { public static object At(int n) => null; }
class Test {
    void Demo() {
        var p = Pos.At(5);
    }
}";
            var diagnostics = await GetDiagnosticsAsync(source);
            Assert.Contains(diagnostics, d => d.Id == V1ApiDiagnosticAnalyzer.RulePosAt.Id);
        }
    }
}
