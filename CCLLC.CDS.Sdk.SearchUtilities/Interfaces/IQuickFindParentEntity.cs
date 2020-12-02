using System;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public interface IQuickFindParentEntity<TTarget, TParent> : IQuickFindLinkedEntity where TTarget : Entity where TParent : Entity
    {
        IQuickFindParentEntity<TTarget, TParent> SearchFields(params string[] fields);
        IQuickFindParentEntity<TTarget, TParent> SearchFields(Expression<Func<TParent, object>> anonymousTypeInitializer);
        IQuickFindParentEntity<TTarget, TParent> IncludeInactiveRecords();
    }
}
