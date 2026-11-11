using NUnit.Framework;
using Seedfall.Core;

namespace Seedfall.Tests.EditMode
{
    /// <summary>Proves the headless test loop works: Core compiles, the test assembly sees it, and results are reported.</summary>
    public class SmokeTests
    {
        [Test]
        public void CoreAssemblyIsReachable()
        {
            Assert.AreEqual("Seedfall", SeedfallInfo.GameName);
        }
    }
}
