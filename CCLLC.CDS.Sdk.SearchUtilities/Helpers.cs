using System;
using System.Collections.Generic;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.Utilities.Search
{
    public static class Helpers
    {
        public static QueryExpression ExtractQueryInputFromContext(ICDSExecutionContext executionContext)
        {
            _ = executionContext ?? throw new ArgumentNullException(nameof(executionContext));

            var inputParameters = executionContext.InputParameters;

            if (!inputParameters.Contains("Query") || inputParameters["Query"] == null)
            {
                throw new ArgumentException("Context does not contain a valid query input argument.");
            }

            if (inputParameters["Query"] is QueryExpression)
            {
                return inputParameters["Query"] as QueryExpression;
            }


            if (inputParameters["Query"] is FetchExpression)
            {
                var fetchExpression = inputParameters["Query"] as FetchExpression;

                var conversionRequest = new FetchXmlToQueryExpressionRequest
                {
                    FetchXml = fetchExpression.Query
                };

                var conversionResponse = (FetchXmlToQueryExpressionResponse)executionContext.OrganizationService.Execute(conversionRequest);

                return conversionResponse.Query;
            }

            throw new Exception("Could not extract a valid query expression from the execution context input parameters.");
        }

        /// <summary>
        /// Examine the filter expression for a query to see if the filter matches a quickfind 
        /// that should be expanded to include records with linked alias table search results. 
        /// </summary>
        /// <param name="filter">The CRM filter expression to examine.</param>        
        /// <param name="searchSignatures"></param>
        /// <returns></returns>
        public static bool MatchesSearchFilterSignature(FilterExpression filter, IList<ISearchQuerySignature> searchSignatures, bool isParentSearchFilter = false)
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

                foreach (var sf in signatureList)
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
                    if (MatchesSearchConditionSignature(condition))
                    {
                        return true;
                    }
                }
            }

            //conditions in this filter did not match so recursively check any sub filters
            foreach (FilterExpression subfilter in filter.Filters)
            {
                if (MatchesSearchFilterSignature(subfilter, searchSignatures, isSearchFilter))
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
        public static bool MatchesSearchConditionSignature(ConditionExpression condition)
        {
            if (condition.Operator == ConditionOperator.Like)
            {
                foreach (object val in condition.Values)
                {
                    if ((val is string) &&
                        ((string)val).EndsWith("%", StringComparison.Ordinal))
                    {
                        return (true);
                    }
                }
            }

            return false;
        }

        public static IList<ISearchQuerySignature> GetDefaultSearchSignatures()
        {
            IList<ISearchQuerySignature> list = new List<ISearchQuerySignature>();
                        
            list.Add(new OrClauseWithQuickFind());
            return list;
        }

        public static bool TryGetQuickFindFilter(FilterExpression parentFilter, out FilterExpression quickFindFilter)
        {
            quickFindFilter = null;
            if (parentFilter is null) return false;

            if (parentFilter.IsQuickFindFilter && parentFilter.FilterOperator == LogicalOperator.Or)
            {
                quickFindFilter = parentFilter;
                return true;
            }

            // recursively search child filters
            foreach(var childFilter in parentFilter.Filters)
            {   
                if (TryGetQuickFindFilter(childFilter, out quickFindFilter)) return true;
            }

            return false;
        } 

        public static bool TryGetSearchTerm(FilterExpression filterExpression, out string searchTerm)
        {           
            foreach (var condition in filterExpression.Conditions)
            {
                if (condition.Operator == ConditionOperator.Like
                    && condition.Values.Count > 0)                   
                {
                    var value = (string)condition.Values[0];

                    searchTerm = value.TrimEnd('*', '%');
                    return true;
                }
            }

            foreach (var filter in filterExpression.Filters)
            {
                if (TryGetSearchTerm(filter, out searchTerm))
                {
                    return true;
                }
            }

            searchTerm = default;
            return false;
        }

        public static void ClearQuickFindFlags(FilterExpression filter)
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
    }
}
