using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SharpChromeTests
{
    [TestClass]
    public class BaseTester
    {
        public TestContext TestContext { get; set; } 

        protected static TestContext GlobalContext;

        [ClassInitialize]
        public static void SetupTests(TestContext context)
        {
            GlobalContext = context;
        }
    }
}