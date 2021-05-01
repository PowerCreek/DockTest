using DockTest.ExternalDeps.Classes.Management.Operations;

namespace DockTest.ExternalDeps.Classes.Management
{

    public interface IServiceData
    {
        
        public OperationManager OperationManager { get; }
        
    }
    
    public class ServiceData : IServiceData
    {
        public OperationManager OperationManager { get; }
        
        public ServiceData(OperationManager operationManager)
        {
            OperationManager = operationManager;
        }
        
    }
}