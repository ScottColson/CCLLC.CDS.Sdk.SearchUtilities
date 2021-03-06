﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    /// <summary>
    /// Supports the Aliased Search Pattern. Examines query supplied in the plugin context query input.
    /// If query runs against an entity of type <typeparamref name="TParent"/> the generator will
    /// modify the query to include any matching records found in the related alias table 
    /// defined by <typeparamref name="TAlias"/>.
    /// </summary>
    /// <typeparam name="TParent"></typeparam>
    /// <typeparam name="TAlias"></typeparam>
    public class AliasedSearchQueryBuilder<TParent, TAlias> : IAliasedSearchQueryBuilder<TParent, TAlias>  where TParent : Entity, new() where TAlias : Entity, new()
    {
        private ICDSExecutionContext ExecutionContext { get; }

        string aliasEntityName = null;
        string linkingAttributeName = null;
        string parentEntityName = null;
        string parentIdAttributeName = null;
        int aliasRecordLimit = 50;
        
        IDictionary<string, string> mappedSearchFields = new Dictionary<string, string>();
        IList<ISearchQuerySignature> searchSignatures = new List<ISearchQuerySignature>();

        /// <summary>
        /// Supports the Aliased Search Pattern. Examines query supplied in the plugin context query input.
        /// If query runs against an entity of type <typeparamref name="TParent"/> the generator will
        /// modify the query to include any matching records found in the related alias table 
        /// defined by <typeparamref name="TAlias"/>.
        /// </summary>
        public AliasedSearchQueryBuilder(ICDSExecutionContext executionContext)
        {
            ExecutionContext = executionContext;

            TParent parent = new TParent();
            parentEntityName = parent.LogicalName;
            parentIdAttributeName = parent.LogicalName + "id";

            TAlias alias = new TAlias();
            aliasEntityName = alias.LogicalName;

            linkingAttributeName = parentIdAttributeName;
        }

        #region FluentMethods

        public IAliasedSearchQueryBuilder<TParent, TAlias> WithLinkingAttribute(string attributeName)
        {
            linkingAttributeName = attributeName;
            return this;
        }

        public IAliasedSearchQueryBuilder<TParent, TAlias> WithMappedSearchFields(IDictionary<string, string> mappedFields)
        {
            this.mappedSearchFields = mappedFields;
            return this;
        }

        public IAliasedSearchQueryBuilder<TParent, TAlias> AddMappedSearchField(string parentFieldName, string aliasFieldName)
        {
            if (mappedSearchFields.ContainsKey(parentFieldName))
            {
                mappedSearchFields[parentFieldName] = aliasFieldName;            

            }
            else
            {
                mappedSearchFields.Add(parentFieldName, aliasFieldName);
            }

            return this;
        }

        public IAliasedSearchQueryBuilder<TParent, TAlias> AddSearchSignature(ISearchQuerySignature signature)
        {
            searchSignatures.Add(signature);
            return this;
        }

        #endregion FluentMethods

        public QueryExpression Build()
        {
            var qry = Helpers.ExtractQueryInputFromContext(this.ExecutionContext);

            // return input query if it does not target the parent entity
            if (qry.EntityName != parentEntityName)
            {
                return qry;
            }

            // return input query if it does not look like a search query filter
            if (!matchesSearchFilterSignature(qry.Criteria))
            {
                return qry;
            }

            QueryExpression aliasQuery = null;

            if (tryGenerateAliasQuery(qry, out aliasQuery))
            {
                var matchingAliasRecords =
                    ExecutionContext.OrganizationService.RetrieveMultiple(
                        aliasQuery).Entities;
               
                var matchingParentIds = matchingAliasRecords
                    .Where(e => e.Contains(linkingAttributeName))
                    .Select(e => e[linkingAttributeName] as EntityReference)
                    .ToList();

                if (matchingParentIds.Count > 0)
                {
                    //modify the existing query to include all matching part ids.
                    ExpandSearchFilter(qry, matchingParentIds);

                    //reset input parameter to qry in case passed in query started as fetchexpression
                    ExecutionContext.InputParameters["Query"] = qry;
                }
            }

            return qry;
        }

       
        

        /// <summary>
        /// Examine the filter expression for a query to see if the filter matches a quickfind 
        /// that should be expanded to include records with linked alias table search results. 
        /// </summary>
        /// <param name="filter">The CRM filter expression to examine.</param>        
        /// <returns></returns>
        private bool matchesSearchFilterSignature(FilterExpression filter, bool isParentSearchFilter = false)
        {
            if (filter == null)
            {
                return false;
            }

            bool isSearchFilter = isParentSearchFilter;

            if (!isParentSearchFilter)
            {
                IList<ISearchQuerySignature> signatureList = new List<ISearchQuerySignature>(searchSignatures);

                if (signatureList.Count <= 0)
                {
                    signatureList = Helpers.GetDefaultSearchSignatures();
                }

                foreach(var sf in signatureList)
                {
                    if (sf.Test(filter))
                    {
                        isSearchFilter = true;
                        break;
                    }
                }
            }
            

            if (isSearchFilter)
            {
                foreach (var condition in filter.Conditions)
                {
                    if (matchesSearchConditionSignature(condition))
                    {
                        return true;
                    }
                }
            }            

            //conditions in this filter did not match so recursively check any sub filters
            foreach (FilterExpression subfilter in filter.Filters)
            {
                if (matchesSearchFilterSignature(subfilter, isSearchFilter))
                {
                    return true;
                }
            }

            return false;
        }

       

        /// <summary>
        /// Examine the condition to see if it matches the standard quick find condition signature.
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private bool matchesSearchConditionSignature(ConditionExpression condition)
        {
            if (condition.Operator == ConditionOperator.Like)
            {                
                foreach (object val in condition.Values)
                {
                    if ((val is string) &&
                        ((string)val).EndsWith("%"))
                    {
                        return (true);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to generate a query to run against the alias entity to search it
        /// using the same search criteria used on the parent table. The search field names from
        /// the parent entity query are mapped over to corresponding field names on the  
        /// alias entity as needed.
        /// </summary>
        /// <param name="parentQuery"></param>
        /// <param name="aliasQuery"></param>
        /// <returns></returns>
        private bool tryGenerateAliasQuery(QueryExpression parentQuery, out QueryExpression aliasQuery)
        {           
            if (parentQuery != null && parentQuery.EntityName == parentEntityName)
            {
                FilterExpression targetFilter = null;

                if (tryGenerateAliasFilter(parentQuery.Criteria, out targetFilter))
                {
                    aliasQuery = new QueryExpression
                    {
                        EntityName = aliasEntityName,
                        Distinct = true,
                        TopCount = aliasRecordLimit,
                        ColumnSet = new ColumnSet(linkingAttributeName),  //return only the linking attribute
                        Criteria = new FilterExpression //use the mapped filter with an additional filter to only get active records
                        {
                            FilterOperator = LogicalOperator.And,
                            Filters =
                            {
                                targetFilter, 
                                new FilterExpression
                                {
                                    FilterOperator = LogicalOperator.And,
                                    Conditions =
                                    {
                                        new ConditionExpression
                                        {
                                            AttributeName = "statecode",
                                            Operator = ConditionOperator.Equal,
                                            Values = {0}
                                        }
                                    }
                                }
                            }
                        },
                        LinkEntities =
                        {
                            new LinkEntity  //inner join active records from parent entity
                            {
                                JoinOperator = JoinOperator.Inner,
                                LinkFromEntityName = aliasEntityName,
                                LinkFromAttributeName = linkingAttributeName,
                                LinkToEntityName = parentEntityName,
                                LinkToAttributeName = parentIdAttributeName,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression
                                        {
                                            AttributeName = "statecode",
                                            Operator = ConditionOperator.Equal,
                                            Values = {0}
                                        }
                                    }
                                }
                            }
                        }
                    };

                    return true;
                }
            }

            aliasQuery = null;
            return false;
        }


        /// <summary>
        /// Generates a CRM filter expression from a query targeting the parent entity to a filter expression targeting
        /// the alias entity if the underlying conditions can be mapped from parent to alias. Recursively processes 
        /// any child filters. Returns true with the generated alias filter if mapping between the parent and the alias 
        /// was successful.
        /// </summary>
        /// <param name="parentFilter">The filter expression from the contact query.</param>
        /// <param name="aliasFilter">The generated filter expression for the alternate name query.</param>
        /// <returns></returns>
        private bool tryGenerateAliasFilter(FilterExpression parentFilter, out FilterExpression aliasFilter)
        {
            if (parentFilter != null)
            {
                var isValid = false;

                aliasFilter = new FilterExpression
                {
                    FilterOperator = parentFilter.FilterOperator
                };

                //add any conditions that can be mapped
                foreach (var condition in parentFilter.Conditions)
                {
                    ConditionExpression tc = null;
                    if (tryGenerateAliasCondition(condition, out tc))
                    {
                        aliasFilter.Conditions.Add(tc);
                        isValid = true;
                    }
                }

                //add any child filters that can be mapped
                foreach (var filter in parentFilter.Filters)
                {
                    FilterExpression tf = null;
                    if (tryGenerateAliasFilter(filter, out tf))
                    {
                        aliasFilter.Filters.Add(tf);
                        isValid = true;
                    }
                }

                if (isValid)
                {
                    return true;
                }
            }

            aliasFilter = null;
            return false;
        }


        /// <summary>
        /// Generates a CRM query condition expression designed for the parent entity to a condition expression targeting
        /// the alias entity if a mapping is defined between the parent field name and the alias field name. Returns
        /// true if the condition mapped.
        /// </summary>
        /// <param name="parentCondition">The condition from the contact query</param>
        /// <param name="aliasCondition">The created condition for the customer alias query.</param>
        /// <returns></returns>
        private bool tryGenerateAliasCondition(ConditionExpression parentCondition, out ConditionExpression aliasCondition)
        {
            if (mappedSearchFields.ContainsKey(parentCondition.AttributeName))
            {
                aliasCondition = new ConditionExpression();
                aliasCondition.Operator = parentCondition.Operator;

                aliasCondition.AttributeName = mappedSearchFields[parentCondition.AttributeName];

                foreach (var v in parentCondition.Values)
                {
                    aliasCondition.Values.Add(v);
                }

                return true;
            }

            aliasCondition = null;
            return false;
        }
         

        /// <summary>
        /// Modifies the filter criteria of the passed in query with an OR clause
        /// that will match any passed in entity reference. This causes the query return
        /// all record found through the alias queries and also any records that match
        /// the original parent entity search.
        /// </summary>
        /// <param name="baseQuery"></param>
        /// <param name="matchingAliasedRecordIds"></param>
        public void ExpandSearchFilter(QueryExpression baseQuery, IList<EntityReference> matchingAliasedRecordIds)
        {
            //generate a new filter expression to match any parent record id in the matching list
            var matchingFilter = new FilterExpression
            {
                FilterOperator = LogicalOperator.Or
            };

            foreach (var item in matchingAliasedRecordIds)
            {
                matchingFilter.AddCondition(parentIdAttributeName, ConditionOperator.Equal, item.Id);
            }

            // extract base filter and remove any quickfind flags in the filter to avoid blocks based 
            // on setting of Organization.QuickFindRecordLimitEnabled flag. See
            // https://msdn.microsoft.com/en-us/library/gg328300.aspx
            var baseFilter = baseQuery.Criteria;
            ClearQuickFindFlags(baseFilter);

            // combine the modified base filter and the aliased record matching filter in an overarching
            // or filter so that the resulting query will return anything that would have originally been
            // found in addition to any records identified by the alias record search.
            baseQuery.Criteria = new FilterExpression
            {
                FilterOperator = LogicalOperator.Or,
                Filters =
                    {
                        baseFilter,
                        matchingFilter
                    }
            };

        }


        private void ClearQuickFindFlags(FilterExpression filter)
        {
            if (filter.IsQuickFindFilter)
            {
                filter.IsQuickFindFilter = false;
            }

            //process any child filters recursively
            foreach (var f in filter.Filters)
            {
                ClearQuickFindFlags(f);
            }
        }

        public IAliasedSearchQueryBuilder<TParent, TAlias> AddSearchSignature(IQueryExpressionBuilder<TAlias> signature)
        {
            throw new NotImplementedException();
        }
    }
}
