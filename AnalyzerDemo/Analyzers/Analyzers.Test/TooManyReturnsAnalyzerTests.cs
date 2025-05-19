using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Analyzers.Test.CSharpAnalyzerVerifier<Analyzers.TooManyReturnsAnalyzer>;

namespace Analyzers.Tests
{
    [TestClass]
    public class TooManyReturnsAnalyzerTests
    {
        [TestMethod]
        public async Task ReportsDiagnostic_WhenMethodHasMoreThanFiveReturns()
        {
            string test = @"
class TestClass
{
    int TestMethod(int x)
    {
        if (x == 0) return 0;
        if (x == 1) return 1;
        if (x == 2) return 2;
        if (x == 3) return 3;
        if (x == 4) return 4;
        if (x == 5) return 5;
        return -1;
    }
}";
            // The diagnostic should be reported at the method identifier 'TestMethod'
            Microsoft.CodeAnalysis.Testing.DiagnosticResult expected = VerifyCS.Diagnostic("API0002").WithSpan(4, 9, 4, 19).WithArguments("TestMethod");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DoesNotReportDiagnostic_WhenMethodHasFiveOrFewerReturns()
        {
            string test = @"
class TestClass
{
    int TestMethod(int x)
    {
        if (x == 0) return 0;
        if (x == 1) return 1;
        if (x == 2) return 2;
        if (x == 3) return 3;
        return -1;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
