using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = Analyzers.Test.CSharpAnalyzerVerifier<Analyzers.DateTimeAnalyzer>;

namespace Analyzers.Tests
{
    [TestClass]
    public class DateTimeAnalyzerTests
    {
        [TestMethod]
        public async Task ReportsDiagnostic_WhenDateTimeNowIsUsed()
        {
            string test = @"
using System;

class TestClass
{
    void TestMethod()
    {
        var now = {|API0001:DateTime.Now|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ReportsDiagnostic_WhenDateTimeUtcNowIsUsed()
        {
            string test = @"
using System;

class TestClass
{
    void TestMethod()
    {
        var utcNow = {|API0001:DateTime.UtcNow|};
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task DoesNotReportDiagnostic_WhenOtherMembersAreUsed()
        {
            string test = @"
using System;

class TestClass
{
    void TestMethod()
    {
        var today = DateTime.Today;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
    }
}
