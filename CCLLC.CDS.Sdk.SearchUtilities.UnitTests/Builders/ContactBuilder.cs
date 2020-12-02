
namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest.Builders
{
    using DLaB.Xrm.Test;
    using Proxy;

    public class ContactBuilder : EntityBuilder<Contact>
    {       
        public ContactBuilder(Id id) : base(id)
        {            
        }

        public ContactBuilder WithParentCustomer(Id value)
        {
            Record.ParentCustomerId = value;
            return this;
        }

        public ContactBuilder WithFirstName(string value)
        {
            Record.FirstName = value;
            return this;
        }
    }
}
