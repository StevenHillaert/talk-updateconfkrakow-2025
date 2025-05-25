using Microsoft;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = Analyzers.Test.CSharpCodeFixVerifier<
    Analyzers.DateTimeAnalyzer,
    Analyzers.DateTimeAnalyzerCodeFixProvider>;

namespace Analyzers.Tests
{
    [TestClass]
    public class DateTimeAnalyzerCodeFixProviderTests
    {
        [TestMethod]
        public async Task ReplacesDateTimeNowWithDateTimeProvider()
        {
            var test = @"
using System;

class MyClass
{
    public DateTime GetNow()
    {
        return [|DateTime.Now|];
    }
}";

            var fixedCode = @"
using System;

class MyClass
{
    public MyClass(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    private readonly IDateTimeProvider _dateTimeProvider;

    public DateTime GetNow()
    {
        return _dateTimeProvider.UtcNow;
    }
}";

            await VerifyCS.VerifyCodeFixAsync(test, fixedCode);
        }
    }
}