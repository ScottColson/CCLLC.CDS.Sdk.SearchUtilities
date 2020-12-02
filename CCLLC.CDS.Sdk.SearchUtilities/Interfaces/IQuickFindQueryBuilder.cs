using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public interface IQuickFindQueryBuilder<TEntity> where TEntity : Entity
    {
        IQuickFindQueryBuilder<TEntity> SearchParent<TParent>(string fromAttribute, Action<IQuickFindParentEntity<TEntity, TParent>> expression) where TParent : Entity, new();
        IQuickFindQueryBuilder<TEntity> SearchChildren<TChild>(string toAttribute, Action<IQuickFindChildEntity<TEntity, TChild>> expression) where TChild : Entity, new();
        IQuickFindQueryBuilder<TEntity> FluentQuerySearch(Action<IQueryExpressionBuilder<TEntity>> expression);
        IQuickFindQueryBuilder<TEntity> AddSearchSignature(ISearchQuerySignature signature);
        QueryExpression Build();
    }
}
