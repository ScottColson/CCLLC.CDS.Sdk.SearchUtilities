using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest
{
    using Proxy;
    using CCLLC.CDS.Sdk.Utilities.Search;
    using DLaB.Xrm.Test;
    using Builders;

    [TestClass]
    public class SearchTests
    {
        [TestMethod]
        public void Test_Search_Should_MatchOnParentField()
        {
            new Search_Should_MatchOnParentField().Test();
        }

        private class Search_Should_MatchOnParentField : TestMethodClassBase
        {
            private struct TestData
            {
                public static readonly string AccountNumber = "1234";
                public static readonly string SearchTerm = AccountNumber + "*";
            }

            private struct Ids
            {
                public static readonly Id<Account> Account = new Id<Account>("{D6808B80-5AB6-4466-94F0-DE7CDDB75F5C}");
                public static readonly Id<Contact> Contact = new Id<Contact>("{EF1EE47D-B668-4A89-890F-C2CA45D2FBCB}");
                public static readonly Id<Contact> Contact2 = new Id<Contact>("{7CC266B6-78AC-4DE1-B6A4-12CAC6487E9B}");
            }

            protected override void InitializeTestData(IOrganizationService service)
            {
                new CrmEnvironmentBuilder()
                    .WithBuilder<ContactBuilder>(Ids.Contact, b => b
                        .WithParentCustomer(Ids.Account))
                    .WithBuilder<AccountBuilder>(Ids.Account, b => b
                        .WithAccountNumber(TestData.AccountNumber))
                    .WithEntities<Ids>()
                    .Create(service);
            }

            protected override void Test(IOrganizationService service)
            {
                var executionContext = new Fakes.FakeExecutionContext(service);
                var contact = service.GetRecord(Ids.Contact);

                // generate a qry that looks like a quickfind query
                var qry = new QueryExpressionBuilder<Contact>()
                    .Select(cols => new { cols.Id, cols.FullName })
                    .WhereAll(e => e                        
                        .IsActive()                        
                        .WhereAny(e2 => e2
                            .QuickFind()
                            .Attribute(Contact.Fields.FullName).IsLike(TestData.SearchTerm)))
                    .Build();
               
                var modifiedQuery = new QuickFindQueryBuilder<Contact>(executionContext, qry)
                    .SearchParent<Account>(Contact.Fields.ParentCustomerId, p => p
                        .SearchFields(cols => new { cols.AccountNumber }))
                    .Build();

                Assert.AreEqual(qry.ColumnSet, modifiedQuery.ColumnSet);
                Assert.IsFalse(modifiedQuery.Criteria.IsQuickFindFilter);
                
                Assert.AreEqual(LogicalOperator.Or, modifiedQuery.Criteria.FilterOperator);
                Assert.AreEqual(2, modifiedQuery.Criteria.Filters.Count);

                var linkedFilter = modifiedQuery.Criteria.Filters[1];
                Assert.AreEqual(LogicalOperator.Or, linkedFilter.FilterOperator);
                Assert.AreEqual(1, linkedFilter.Conditions.Count);
                Assert.AreEqual(ConditionOperator.In, linkedFilter.Conditions[0].Operator);


                //verify query parses correctly
                var records = service.RetrieveMultiple(modifiedQuery);
                
            }
        }


        [TestMethod]
        public void Test_Search_Should_MatchOnChildField()
        {
            new Search_Should_MatchOnChildField().Test();
        }

        private class Search_Should_MatchOnChildField : TestMethodClassBase
        {
            private struct TestData
            {
                public static readonly string FirstName = "TestName";
                public static readonly string SearchTerm = FirstName + "*";
            }

            private struct Ids
            {
                public static readonly Id<Account> Account = new Id<Account>("{0228ED71-01BC-40F5-98DF-F9BE3957B590}");
                public static readonly Id<Contact> Contact = new Id<Contact>("{CCD504F3-044D-4A6F-B1F0-81215B2739B7}");                
            }

            protected override void InitializeTestData(IOrganizationService service)
            {
                new CrmEnvironmentBuilder()
                    .WithBuilder<ContactBuilder>(Ids.Contact, b => b
                        .WithParentCustomer(Ids.Account)
                        .WithFirstName(TestData.FirstName))
                    
                    .WithEntities<Ids>()
                    .Create(service);
            }

            protected override void Test(IOrganizationService service)
            {
                var executionContext = new Fakes.FakeExecutionContext(service);
                var contact = service.GetRecord(Ids.Contact);

                // generate a qry that looks like a quickfind query
                var qry = new QueryExpressionBuilder<Account>()
                    .Select(cols => new { cols.Id })
                    .WhereAll(e => e                        
                        .IsActive()
                        .WhereAny(e2 => e2
                            .QuickFind()
                            .Attribute(Account.Fields.Name).IsLike(TestData.SearchTerm)))
                    .Build();

                var modifiedQuery = new QuickFindQueryBuilder<Account>(executionContext, qry)
                    .SearchChildren<Contact>(Contact.Fields.ParentCustomerId, p => p
                        .SearchFields(cols => new { cols.FirstName, cols.LastName, cols.FullName }))
                    .LimitSearchToParentFilter()
                    .Build();

                
                Assert.AreEqual(qry.ColumnSet, modifiedQuery.ColumnSet);
                Assert.IsFalse(modifiedQuery.Criteria.IsQuickFindFilter);

                // Top level filter not modified
                Assert.AreEqual(LogicalOperator.And, modifiedQuery.Criteria.FilterOperator);
                Assert.AreEqual(1, modifiedQuery.Criteria.Conditions.Count);
                Assert.AreEqual(1, modifiedQuery.Criteria.Filters.Count);

                // Quick find child filter conditions not modified
                var quickFindFilter = modifiedQuery.Criteria.Filters[0];
                Assert.AreEqual(LogicalOperator.Or, quickFindFilter.FilterOperator);
                Assert.AreEqual(1, quickFindFilter.Conditions.Count);
                Assert.AreEqual(ConditionOperator.Like, quickFindFilter.Conditions[0].Operator);

                // Linked entity filter added to quick find filter
                var linkedFilter = quickFindFilter.Filters[0];
                Assert.AreEqual(LogicalOperator.Or, linkedFilter.FilterOperator);
                Assert.AreEqual(1, linkedFilter.Conditions.Count);
                Assert.AreEqual(ConditionOperator.In, linkedFilter.Conditions[0].Operator);

                //verify query parses correctly
                var records = service.RetrieveMultiple(modifiedQuery);

            }
        }
    }
}
