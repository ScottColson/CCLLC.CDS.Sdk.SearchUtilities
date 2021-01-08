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
            private int topCount = 20;

            public QuickFindParentEntity(string fromAttribute = null)
            {
                linkingAttribute = fromAttribute;
            }

            public Guid[] GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm, bool useElevatedAccess)
            {                
                var processName = $"{nameof(QuickFindParentEntity<TTarget, TParent>)}.{nameof(GetLinkedIds)}";
                executionContext.Trace($"Entered {processName}");

                if (linkingAttribute.Length == 0 || searchFields.Count == 0)
                {
                    executionContext.Trace($"Exiting {processName} - No search fields or linking attribute.");
                    return Array.Empty<Guid>();
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
                    TopCount = topCount,
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
                    executionContext.Trace($"{processName}: Filtering on active records.");
                    qry.LinkEntities[0].LinkCriteria.Conditions.Add(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
                }

                // add one condition for each search field to the linked entity search filter.
                foreach (var field in searchFields)
                {
                    qry.LinkEntities[0].LinkCriteria.Filters[0].Conditions.Add(new ConditionExpression(field, ConditionOperator.Like, searchTerm + "%"));
                }

                // execute the query and return a list of target ids found.
                var orgService = useElevatedAccess ? executionContext.ElevatedOrganizationService : executionContext.OrganizationService;
                var ids = orgService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToArray();

                executionContext.Trace($"Exiting {processName} - Returning {ids.Length} record ids.");
                return ids;
            
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
            private int topCount = 20;

            public QuickFindChildEntity(string toAttribute = null)
            {
                linkingAttribute = toAttribute;
            }

            public Guid[] GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm, bool useElevatedAccess)
            {
                var processName = $"{nameof(QuickFindChildEntity<TTarget, TChild>)}.{nameof(GetLinkedIds)}";
                executionContext.Trace($"Entered {processName}");

                if (linkingAttribute.Length == 0 || searchFields.Count == 0)
                {
                    executionContext.Trace($"Exiting {processName} - No search fields or linking attribute.");
                    return Array.Empty<Guid>();
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
                    TopCount = topCount,
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
                var orgService = useElevatedAccess ? executionContext.ElevatedOrganizationService : executionContext.OrganizationService;
                var ids = orgService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToArray();

                executionContext.Trace($"Exiting {processName} - Returning {ids.Length} record ids.");
                return ids;
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

            public Guid[] GetLinkedIds(ICDSExecutionContext executionContext, string searchTerm, bool useElevatedAccess)
            {
                var processName = $"{nameof(QuickFindQueryExpressionBuilder<TTarget>)}.{nameof(GetLinkedIds)}";
                executionContext.Trace($"Entered {processName}");

                var qry = QueryExpressionBuilder                    
                    .WithSearchValue(searchTerm)
                    .Build();

                // execute the query and return a list of target ids found.
                var orgService = useElevatedAccess ? executionContext.ElevatedOrganizationService : executionContext.OrganizationService;
                var ids = orgService.RetrieveMultiple(qry).Entities.Select(e => e.Id).ToArray();

                executionContext.Trace($"Exiting {processName} - Returning {ids.Length} record ids.");
                return ids;
            }
        }

        private readonly ICDSExecutionContext executionContext;
        private readonly QueryExpression sourceQueryExpresion;
        private readonly string targetEntityLogicalName;
        private readonly string targetEntityIdField;
        private readonly IList<IQuickFindLinkedEntity> linkedEntities;
        private readonly IList<ISearchQuerySignature> searchSignatures;
        private readonly bool useElevatedAccess;

        /// <summary>
        /// Builder to enhance the passed in search filter by simulating a union with the results of additional
        /// queries defined in the builder.
        /// </summary>
        /// <param name="executionContext"></param>
        /// <param name="sourceQueryExpression">The source query</param>
        /// <param name="useElevatedAccess">Will execute the additional defined queries as system user.</param>
        public QuickFindQueryBuilder(ICDSExecutionContext executionContext, QueryExpression sourceQueryExpression, bool useElevatedAccess = false)
        {
            this.executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            this.sourceQueryExpresion = sourceQueryExpression ?? throw new ArgumentNullException(nameof(sourceQueryExpression));
                        
            this.targetEntityLogicalName = new TEntity().LogicalName;
            this.targetEntityIdField = this.targetEntityLogicalName + "id";

            this.linkedEntities = new List<IQuickFindLinkedEntity>();
            this.searchSignatures = new List<ISearchQuerySignature>();

            this.useElevatedAccess = useElevatedAccess;
        }

        public IQuickFindQueryBuilder<TEntity> SearchParent<TParent>(string fromAttribute, Action<IQuickFindParentEntity<TEntity, TParent>> expression) where TParent : Entity, new()
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(SearchParent)}";
            executionContext.Trace($"Entered {processName}");

            var linkedParent = new QuickFindParentEntity<TEntity, TParent>(fromAttribute);
            expression(linkedParent);
            linkedEntities.Add(linkedParent);

            executionContext.Trace($"Exiting {processName} - Completed.");
            return this;
        }        

        public IQuickFindQueryBuilder<TEntity> SearchChildren<TChild>(string toAttribute, Action<IQuickFindChildEntity<TEntity, TChild>> expression) where TChild : Entity, new()
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(SearchChildren)}";
            executionContext.Trace($"Entered {processName}");

            var linkedChild = new QuickFindChildEntity<TEntity, TChild>(toAttribute);
            expression(linkedChild);
            linkedEntities.Add(linkedChild);

            executionContext.Trace($"Exiting {processName} - Completed.");
            return this;
        }

        public IQuickFindQueryBuilder<TEntity> FluentQuerySearch(Action<IQueryExpressionBuilder<TEntity>> expression)
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(FluentQuerySearch)}";
            executionContext.Trace($"Entered {processName}");

            var qryExpressionBuilder = new QueryExpressionBuilder<TEntity>().Select(cols => new { cols.Id });
            expression(qryExpressionBuilder);
            linkedEntities.Add(new QuickFindQueryExpressionBuilder<TEntity>(qryExpressionBuilder));

            executionContext.Trace($"Exiting {processName} - Completed.");
            return this;
        }

        public IQuickFindQueryBuilder<TEntity> AddSearchSignature(ISearchQuerySignature signature)
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(AddSearchSignature)}";
            executionContext.Trace($"Entered - {processName}");

            searchSignatures.Add(signature);
            
            executionContext.Trace($"Exiting {processName} - Completed");
            return this;
        }

        public QueryExpression Build()
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(Build)}";
            executionContext.Trace($"Entered {processName}");

            if (sourceQueryExpresion.EntityName != targetEntityLogicalName)
            {
                executionContext.Trace($"Exiting {processName} - Query not for {targetEntityLogicalName} entity.");
                return sourceQueryExpresion;
            }

            // return input query if it does not look like a search query filter
            if (!Helpers.MatchesSearchFilterSignature(sourceQueryExpresion.Criteria, searchSignatures))
            {
                executionContext.Trace($"Exiting {processName} - Query does not match filter signature");
                return sourceQueryExpresion;
            }

            string searchTerm;

            if (!Helpers.TryGetSearchTerm(sourceQueryExpresion.Criteria, out searchTerm))
            {
                executionContext.Trace($"Exiting {processName} - Could not locate search term.");
                return sourceQueryExpresion;
            }

            var linkedEntityFilter = GenerateLinkedEntityFilter(searchTerm);

            var replacementQuery = GenerateEnhancedQuery(sourceQueryExpresion, linkedEntityFilter);

            executionContext.Trace($"Exiting {processName} - Completed.");
            return replacementQuery;
        }

        private FilterExpression GenerateLinkedEntityFilter(string searchTerm)
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(GenerateLinkedEntityFilter)}";
            executionContext.Trace($"Entered {processName}");

            var linkedEntityFilter = new FilterExpression(LogicalOperator.Or);

            foreach (var linkedEntity in linkedEntities)
            {
                var linkedIds = linkedEntity.GetLinkedIds(executionContext, searchTerm, useElevatedAccess);
                if (linkedIds.Length > 0)
                {
                    executionContext.Trace($"{processName}: Adding search condition for {linkedIds.Length} records found through expanded search.");
                    linkedEntityFilter.AddCondition(new ConditionExpression(targetEntityIdField, ConditionOperator.In, linkedIds));
                }
            }

            executionContext.Trace($"Exiting {processName} - Completed.");
            return linkedEntityFilter;
        }

        private QueryExpression GenerateEnhancedQuery(QueryExpression sourceQuery, FilterExpression linkedEntityFilter)
        {
            var processName = $"{nameof(QuickFindQueryBuilder<TEntity>)}.{nameof(GenerateEnhancedQuery)}";
            executionContext.Trace($"Entered {processName}");

            if (linkedEntityFilter is null || linkedEntityFilter.Conditions.Count <= 0)
            {
                executionContext.Trace($"Exiting {processName} - Linked Entity Filter does not contain any conditions, Returning source query.");
                return sourceQuery;
            }

            var replacementQuery = new QueryExpression()
            {
                EntityName = sourceQuery.EntityName,
                ColumnSet = sourceQuery.ColumnSet,
                Distinct = true
            };

            // copy all existing linked entities from the source query
            foreach(var linkedEntity in sourceQuery.LinkEntities)
            {
                replacementQuery.LinkEntities.Add(linkedEntity);
            }

            // copy all existing sort orders from the source query
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

            executionContext.Trace($"Exiting {processName} - Completed.");
            return replacementQuery;
        }
               
    }
}
