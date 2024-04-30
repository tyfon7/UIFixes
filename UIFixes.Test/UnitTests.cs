namespace UIFixes.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void RemoveTrailingZeroTest()
        {
            var testCases = new Dictionary<string, string>
            {
                { "10", "10" },
                { "400", "400" },
                { "4.01", "4.01" },
                { "5.060", "5.06" },
                { "3.000", "3" },
                { "2.0000001000", "2.0000001" },
                { "0.5", "0.5" },
                { "0.05", "0.05" },
                { "0.50", "0.5" },
                { "400sec", "400sec" },
                { "del. 2sec", "del. 2sec" },
                { "Hello.world", "Hello.world" },
                { "2Fast20Furious0", "2Fast20Furious0" },
                { "1.0.2", "1.0.2" }
            };

            foreach (var testCase in testCases)
            {
                string result = ItemPanelPatches.RemoveTrailingZeros(testCase.Key);
                Assert.AreEqual(result, testCase.Value);
            }
        }
    }
}