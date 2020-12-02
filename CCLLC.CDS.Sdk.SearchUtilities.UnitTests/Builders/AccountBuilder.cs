
namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest.Builders
{
    using DLaB.Xrm.Test;
    using Proxy;

    public class AccountBuilder : EntityBuilder<Account>
    {       
        public AccountBuilder(Id id) : base(id)
        {            
        }

        public AccountBuilder WithAccountNumber(string value)
        {
            Record.AccountNumber = value;
            return this;
        }
    }
}
