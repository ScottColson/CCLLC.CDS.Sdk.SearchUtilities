using System;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public interface IQuickFindChildEntity<TTarget, TChild> : IQuickFindLinkedEntity where TTarget : Entity where TChild : Entity
    {
        IQuickFindChildEntity<TTarget, TChild> SearchFields(params string[] fields);
        IQuickFindChildEntity<TTarget, TChild> SearchFields(Expression<Func<TChild, object>> anonymousTypeInitializer);
        IQuickFindChildEntity<TTarget, TChild> IncludeInactiveRecords();
    }

}
