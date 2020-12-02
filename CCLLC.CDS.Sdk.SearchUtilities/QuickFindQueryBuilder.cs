using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public class QuickFindQueryBuilder<TEntity> : IQuickFindQueryBuilder<TEntity> where TEntity : Entity, new()
    {
        private class QuickFindParentEntity<TTarget, TParent> : IQuickFindParentEntity<TTarget, TParent> where TTarget : Entity, new() where TParent : Entity, new()
        {
            private readonly HashSet<string> searchFields = new HashSet<string>();
            private string linkingAttribute;
            private bool includeInactiveRecords;

            public QuickFindParentEntity(string fromAttribute = null)
            {
                linkingAttribute = fromAttribute;
            }

            public IList<Guid> GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm)
            {
                if (linkingAttribute.Length == 0 || searchFields.Count == 0)
                {
                    return new List<Guid>();
                }

                var targetLogicalName = new TTarget().LogicalName;
                var targetIdField = targetLogicalName + "id";
                var parentLogicalName = new TParent().LogicalName;
                var parentIdField = parentLogicalName + "id";

                // create a query that returns ids for the target entity that are joined to
                // the parent entity via the identified linking attribute where the parent entity
                // matches any of the search fields.
                var qry = new QueryExpression
                {
                    EntityName = targetLogicalName,
                    ColumnSet = new ColumnSet(targetIdField),
                    Distinct = true,
                    NoLock = true,
                    LinkEntities =
                    {
                        new LinkEntity
                        {
                            JoinOperator = JoinOperator.Inner,
                            LinkFromEntityName = targetLogicalName,
                            LinkFromAttributeName = linkingAttribute,
                            LinkToEntityName = parentLogicalName,
                            LinkToAttributeName = parentIdField,
                            LinkCriteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And,
                                Conditions = { },
                                Filters =
                                {
                                    new FilterExpression
                                    {
                                        FilterOperator = LogicalOperator.Or,
                                        Conditions = { }
                                    }
                                }

                            }
                        }
                    }
                };

                // unless inactive parents are excluded then add a condition to the linked entity criteria to only
                // join active records.
                if (!includeInactiveRecords)
                {
                    qry.LinkEntities[0].LinkCriteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                }

                // add one condition for each search field to the linked entity search filter.
                foreach (var field in searchFields)
                {
                    qry.LinkEntities[0].LinkCriteria.Filters[0].Conditions.Add(new ConditionExpression(field, ConditionOperator.Like, searchTerm + "%"));
                }

                // execute the query and return a list of target ids found.
                return executionContext.OrganizationService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToList();
            }

            public IQuickFindParentEntity<TTarget, TParent> SearchFields(params string[] fields)
            {
                foreach (var field in fields)
                {
                    if (!searchFields.Contains(field))
                    {
                        searchFields.Add(field);
                    }
                }

                return this;
            }
           
            public IQuickFindParentEntity<TTarget, TParent> SearchFields(Expression<Func<TParent, object>> anonymousTypeInitializer)
            {
                var columns = anonymousTypeInitializer.GetAttributeNamesArray<TParent>();
                return SearchFields(columns);
            }

            public IQuickFindParentEntity<TTarget, TParent> LinkedByField(string linkingAttribute)
            {
                this.linkingAttribute = linkingAttribute;
                return this;
            }            

            public IQuickFindParentEntity<TTarget, TParent> LinkedByField(Expression<Func<TTarget, object>> anonymousTypeInitializer)
            {
                var columns = anonymousTypeInitializer.GetAttributeNamesArray<TTarget>();
                return LinkedByField(columns[0]);
            }

            public IQuickFindParentEntity<TTarget, TParent> IncludeInactiveRecords()
            {
                includeInactiveRecords = true;
                return this;
            }
        }

        private class QuickFindChildEntity<TTarget, TChild> : IQuickFindChildEntity<TTarget, TChild> where TTarget : Entity, new() where TChild : Entity, new()
        {
            private readonly HashSet<string> searchFields = new HashSet<string>();
            private string linkingAttribute;
            private bool includeInactiveRecords;

            public QuickFindChildEntity(string toAttribute = null)
            {
                linkingAttribute = toAttribute;
            }

            public IList<Guid> GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm)
            {
                if (linkingAttribute.Length == 0 || searchFields.Count == 0)
                {
                    return new List<Guid>();
                }

                var targetLogicalName = new TTarget().LogicalName;
                var targetIdField = targetLogicalName + "id";
                var childLogicalName = new TChild().LogicalName;
                var childIdField = childLogicalName + "id";

                // create a query that returns ids for the target entity that are joined to
                // the parent entity via the identified linking attribute where the parent entity
                // matches any of the search fields.
                var qry = new QueryExpression
                {
                    EntityName = targetLogicalName,
                    ColumnSet = new ColumnSet(targetIdField),
                    Distinct = true,
                    NoLock = true,
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions = { }
                    },
                    LinkEntities =
                    {
                        new LinkEntity
                        {
                            JoinOperator = JoinOperator.Inner,
                            LinkFromEntityName = targetLogicalName,
                            LinkFromAttributeName = targetIdField,
                            LinkToEntityName = childLogicalName,
                            LinkToAttributeName = linkingAttribute,
                            LinkCriteria = new FilterExpression
                            {
                                FilterOperator = LogicalOperator.And,
                                Conditions = { },
                                Filters =
                                {
                                    new FilterExpression
                                    {
                                        FilterOperator = LogicalOperator.Or,
                                        Conditions = { }
                                    }
                                }

                            }
                        }
                    }
                };

                // unless inactive parents are excluded then add a condition to the criteria to only
                // get active records.
                if (!includeInactiveRecords)
                {
                    qry.Criteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                }

                // add one condition for each search field to the linked entity search filter.
                foreach (var field in searchFields)
                {
                    qry.LinkEntities[0].LinkCriteria.Filters[0].Conditions.Add(new ConditionExpression(field, ConditionOperator.Like, searchTerm + "%"));
                }

                // execute the query and return a list of target ids found.
                return executionContext.OrganizationService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToList();
            }

            public IQuickFindChildEntity<TTarget, TChild> SearchFields(params string[] fields)
            {
                foreach (var field in fields)
                {
                    if (!searchFields.Contains(field))
                    {
                        searchFields.Add(field);
                    }
                }

                return this;
            }

            public IQuickFindChildEntity<TTarget, TChild> SearchFields(Expression<Func<TChild, object>> anonymousTypeInitializer)
            {
                var columns = anonymousTypeInitializer.GetAttributeNamesArray<TChild>();
                return SearchFields(columns);
            }

            public IQuickFindChildEntity<TTarget, TChild> LinkedByField(string linkingAttribute)
            {
                this.linkingAttribute = linkingAttribute;
                return this;
            }

            public IQuickFindChildEntity<TTarget, TChild> LinkedByField(Expression<Func<TChild, object>> anonymousTypeInitializer)
            {
                var columns = anonymousTypeInitializer.GetAttributeNamesArray<TChild>();
                return LinkedByField(columns[0]);
            }

            public IQuickFindChildEntity<TTarget, TChild> IncludeInactiveRecords()
            {
                includeInactiveRecords = true;
                return this;
            }
        }

        private class QuickFindQueryExpressionBuilder<TTarget> : IQuickFindLinkedEntity where TTarget : Entity
        {
            IQueryExpressionBuilder<TTarget> QueryExpressionBuilder;

            public QuickFindQueryExpressionBuilder(IQueryExpressionBuilder<TTarget> queryExpressionBuilder)
            {
                QueryExpressionBuilder = queryExpressionBuilder;
            }

            public IList<Guid> GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm)
            {
                var qry = QueryExpressionBuilder
                    .Select(cols => cols.Id)
                    .WithSearchValue(searchTerm)
                    .Build();

                // execute the query and return a list of target ids found.
                return executionContext.OrganizationService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToList();
            }
        }

        private readonly ICDSExecutionContext executionContext;
        private readonly QueryExpression sourceQueryExpresion;
        private readonly string targetEntityLogicalName;
        private readonly string targetEntityIdField;
        private readonly IList<IQuickFindLinkedEntity> linkedEntities;
        private readonly IList<ISearchQuerySignature> searchSignatures;
        
        public QuickFindQueryBuilder(ICDSExecutionContext executionContext, QueryExpression sourceQueryExpression)
        {
            this.executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            this.sourceQueryExpresion = sourceQueryExpression ?? throw new ArgumentNullException(nameof(sourceQueryExpression));
                        
            this.targetEntityLogicalName = new TEntity().LogicalName;
            this.targetEntityIdField = this.targetEntityLogicalName + "id";

            this.linkedEntities = new List<IQuickFindLinkedEntity>();
            this.searchSignatures = new List<ISearchQuerySignature>();
        }

        public IQuickFindQueryBuilder<TEntity> SearchParent<TParent>(string fromAttribute, Action<IQuickFindParentEntity<TEntity, TParent>> expression) where TParent : Entity, new()
        {
            var linkedParent = new QuickFindParentEntity<TEntity, TParent>(fromAttribute);
            expression(linkedParent);
            linkedEntities.Add(linkedParent);
            return this;
        }        

        public IQuickFindQueryBuilder<TEntity> SearchChildren<TChild>(string toAttribute, Action<IQuickFindChildEntity<TEntity, TChild>> expression) where TChild : Entity, new()
        {
            var linkedChild = new QuickFindChildEntity<TEntity, TChild>(toAttribute);
            expression(linkedChild);
            linkedEntities.Add(linkedChild);
            return this;
        }

        public IQuickFindQueryBuilder<TEntity> FluentQuerySearch(Action<IQueryExpressionBuilder<TEntity>> expression)
        {
            var qryExpressionBuilder = new QueryExpressionBuilder<TEntity>();
            expression(qryExpressionBuilder);
            linkedEntities.Add(new QuickFindQueryExpressionBuilder<TEntity>(qryExpressionBuilder));
            return this;
        }

        public IQuickFindQueryBuilder<TEntity> AddSearchSignature(ISearchQuerySignature signature)
        {
            throw new NotImplementedException();
        }

        public QueryExpression Build()
        {
            if (sourceQueryExpresion.EntityName != targetEntityLogicalName)
            {
                return sourceQueryExpresion;
            }

            // return input query if it does not look like a search query filter
            if (!Helpers.MatchesSearchFilterSignature(sourceQueryExpresion.Criteria, searchSignatures))
            {
                return sourceQueryExpresion;
            }

            string searchTerm;

            if (!Helpers.TryGetSearchTerm(sourceQueryExpresion.Criteria, out searchTerm))
            {
                return sourceQueryExpresion;
            }

            var linkedEntityFilter = GenerateLinkedEntityFilter(searchTerm);

            return GenerateEnhancedQuery(sourceQueryExpresion, linkedEntityFilter);
        }

        private FilterExpression GenerateLinkedEntityFilter(string searchTerm)
        {
            var linkedEntityFilter = new FilterExpression(LogicalOperator.Or);

            foreach (var linkedEntity in linkedEntities)
            {
                var linkedIds = linkedEntity.GetLinkedIds(executionContext, searchTerm);
                if (linkedIds.Count > 0)
                {
                    linkedEntityFilter.AddCondition(new ConditionExpression(targetEntityIdField, ConditionOperator.In, linkedIds));
                }
            }

            return linkedEntityFilter;
        }

        private QueryExpression GenerateEnhancedQuery(QueryExpression sourceQuery, FilterExpression linkedEntityFilter)
        {
            
            if (linkedEntityFilter is null || linkedEntityFilter.Conditions.Count <= 0)
                return sourceQuery;

            var replacementQuery = new QueryExpression()
            {
                EntityName = sourceQuery.EntityName,
                ColumnSet = sourceQuery.ColumnSet,
                Distinct = true
            };

            foreach (var columnOrder in sourceQueryExpresion.Orders)
            {
                replacementQuery.AddOrder(columnOrder.AttributeName, columnOrder.OrderType);
            }

            replacementQuery.Criteria = new FilterExpression(LogicalOperator.Or)
            {                
            };

            replacementQuery.Criteria.AddFilter(sourceQueryExpresion.Criteria);
            replacementQuery.Criteria.AddFilter(linkedEntityFilter);

            // Remove any quickfind flags in the filter to avoid blocks based 
            // on setting of Organization.QuickFindRecordLimitEnabled flag. See
            // https://msdn.microsoft.com/en-us/library/gg328300.aspx
            Helpers.ClearQuickFindFlags(replacementQuery.Criteria);

            return replacementQuery;
        }
               
    }
}
