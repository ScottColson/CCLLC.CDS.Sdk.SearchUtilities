using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCLLC.Core;
using CCLLC.Core.Net;
using Microsoft.Xrm.Sdk;

namespace CCLLC.CDS.Sdk.SearchUtilities.UnitTest.Fakes
{
    public class FakeExecutionContext : ICDSExecutionContext
    {
        private IEnhancedOrganizationService Service;
        public FakeExecutionContext(IOrganizationService service)
        {
            Service = new FakeEnhancedOrganizationService(service);
        }

        public eRunAs RunAs => throw new NotImplementedException();

        public IEnhancedOrganizationService OrganizationService => Service;

        public IEnhancedOrganizationService ElevatedOrganizationService => Service;

        public ITracingService TracingService => throw new NotImplementedException();

        public IXmlConfigurationResource XmlConfigurationResources => throw new NotImplementedException();

        public Entity TargetEntity => throw new NotImplementedException();

        public EntityReference TargetReference => throw new NotImplementedException();

        public IDataService DataService => throw new NotImplementedException();

        public ICache Cache => throw new NotImplementedException();

        public IReadOnlyIocContainer Container => throw new NotImplementedException();

        public ISettingsProvider Settings => throw new NotImplementedException();

        public int Mode => throw new NotImplementedException();

        public int IsolationMode => throw new NotImplementedException();

        public int Depth => throw new NotImplementedException();

        public string MessageName => throw new NotImplementedException();

        public string PrimaryEntityName => throw new NotImplementedException();

        public Guid? RequestId => throw new NotImplementedException();

        public string SecondaryEntityName => throw new NotImplementedException();

        public ParameterCollection InputParameters => throw new NotImplementedException();

        public ParameterCollection OutputParameters => throw new NotImplementedException();

        public ParameterCollection SharedVariables => throw new NotImplementedException();

        public Guid UserId => throw new NotImplementedException();

        public Guid InitiatingUserId => throw new NotImplementedException();

        public Guid BusinessUnitId => throw new NotImplementedException();

        public Guid OrganizationId => throw new NotImplementedException();

        public string OrganizationName => throw new NotImplementedException();

        public Guid PrimaryEntityId => throw new NotImplementedException();

        public EntityImageCollection PreEntityImages => throw new NotImplementedException();

        public EntityImageCollection PostEntityImages => throw new NotImplementedException();

        public EntityReference OwningExtension => throw new NotImplementedException();

        public Guid CorrelationId => throw new NotImplementedException();

        public bool IsExecutingOffline => throw new NotImplementedException();

        public bool IsOfflinePlayback => throw new NotImplementedException();

        public bool IsInTransaction => throw new NotImplementedException();

        public Guid OperationId => throw new NotImplementedException();

        public DateTime OperationCreatedOn => throw new NotImplementedException();

        public T CreateOrganizationRequest<T>() where T : OrganizationRequest, new()
        {
            throw new NotImplementedException();
        }

        public IWebRequest CreateWebRequest(Uri address, string dependencyName = null)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public T GetRecord<T>(EntityReference recordId, TimeSpan? cacheTimeout = null) where T : Entity
        {
            throw new NotImplementedException();
        }

        public T GetRecord<T>(EntityReference recorddId, string[] columns, TimeSpan? cacheTimeout = null) where T : Entity
        {
            throw new NotImplementedException();
        }

        public void Trace(string message, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void Trace(eSeverityLevel severityLevel, string message, params object[] args)
        {
            throw new NotImplementedException();
        }

        public void TrackEvent(string name)
        {
            throw new NotImplementedException();
        }

        public void TrackException(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
