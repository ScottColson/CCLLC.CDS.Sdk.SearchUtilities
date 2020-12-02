using DLaB.Xrm.Test;
using DLaB.Xrm.Test.Builders;
using Microsoft.Xrm.Sdk;

namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest
{
    public abstract class TestMethodClassBase : TestMethodClassBaseDLaB
    {
        public TestMethodClassBase() : base()
        {            
        }

        protected override IAgnosticServiceBuilder GetOrganizationServiceBuilder(IOrganizationService service) { return new Builders.OrganizationServiceBuilder(service); }

        protected override void LoadConfigurationSettings()
        {
            TestInitializer.InitializeTestSettings();
        }

      
        public void Test()
        {
            Test(new DebugLogger());
        }
    }
}
