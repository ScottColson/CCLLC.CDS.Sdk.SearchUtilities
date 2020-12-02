using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public class AndClauseWithQuickFind : SearchQuerySignatureBase
    {
        public AndClauseWithQuickFind() : base()
        {
            this.FilterOperator = LogicalOperator.And;
            this.RequireQuickFind = true;
        }
    }
}
