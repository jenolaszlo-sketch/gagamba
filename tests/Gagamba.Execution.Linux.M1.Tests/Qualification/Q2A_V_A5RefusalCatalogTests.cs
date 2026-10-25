// RC2A-02A Q2A-V A5 refusal catalog execution (test-only). Each named catalog
// entry is a separately reported qualification case with fixed expected code and
// construction-derived exact offset.
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

public sealed class Q2A_V_A5RefusalCatalogTests
{
    public static IEnumerable<object[]> CatalogCaseIds() =>
        QualificationRefusalCatalog.Cases.Select(item => new object[] { item.CaseId });

    [Theory]
    [MemberData(nameof(CatalogCaseIds))]
    public void Q2A_V_A5_RefusalCatalogCase(string caseId)
    {
        var item = QualificationRefusalCatalog.Cases.Single(entry => entry.CaseId == caseId);
        item.Execute();
    }

    [Fact]
    public void Q2A_V_A5_CatalogIsCompleteDistinctAndClassified()
    {
        var cases = QualificationRefusalCatalog.Cases;
        Assert.Equal(78, cases.Length);
        Assert.Equal(cases.Length, cases.Select(item => item.CaseId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(cases, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.FieldId));
            Assert.False(string.IsNullOrWhiteSpace(item.Description));
            Assert.Contains(item.CaseClass, new[] { "malformed", "noncanonical", "misuse" });
        });
        Assert.True(cases.Count(item => item.CaseClass == "malformed") >= 50);
        Assert.True(cases.Count(item => item.CaseClass == "noncanonical") >= 20);
    }
}
