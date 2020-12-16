using System;
using System.Collections.Generic;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public interface IQuickFindLinkedEntity
    {
        Guid[] GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm);
    }
}
