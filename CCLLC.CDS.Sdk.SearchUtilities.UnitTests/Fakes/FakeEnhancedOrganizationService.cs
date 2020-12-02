using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest.Fakes
{
    public class FakeEnhancedOrganizationService : IEnhancedOrganizationService
    {
        IOrganizationService Service;

        public FakeEnhancedOrganizationService(IOrganizationService service)
        {
            Service = service;
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Service.Associate(entityName, entityId, relationship, relatedEntities);
        }

        public Guid Create(Entity entity)
        {
            return Service.Create(entity);
        }

        public void Delete(string entityName, Guid id)
        {
            Service.Delete(entityName, id);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            Service.Disassociate(entityName, entityId, relationship, relatedEntities);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            return Service.Execute(request);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return Service.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            return Service.RetrieveMultiple(query);
        }

        public void Update(Entity entity)
        {
            Service.Update(entity);
        }
    }
}
