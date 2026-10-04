namespace MiniTest;

[TestClass]
public sealed class AddsUp
{
    [TestMethod]
    public void OnePlusOneIsTwo() => Assert.AreEqual(2, 1 + 1);
}
