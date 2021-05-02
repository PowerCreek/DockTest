using System;
using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Operations;

namespace DockTest.Source.NodeContexts
{
    public interface IRootElement
    {
        public ElementContext RootElement { get; init; }
        public IServiceData ServiceData { get; }
        
        public NodeBase NodeBase { get; }
    }
    
    public class RootNode : NodeBase, IRootElement
    {
        public ElementContext RootElement { get; init; }
        public LinkMember Node { get; set; }
        
        public IServiceData ServiceData { get; }
        public StyleOperator StyleOperator { get; }
        public NodeBase NodeBase => this;
        
        private WindowingService WindowingService { get; }
        public ControlOperation ControlOperation { get; }
        
        public RootNode(IServiceData serviceData)
        {
            ServiceData = serviceData;
            ControlOperation = ServiceData.OperationManager.GetOperation<ControlOperation>();

            StyleOperator = 
                ControlOperation.StyleOperator = ServiceData.OperationManager.GetOperation<StyleOperator>();

            WindowingService = new()
            {
                ControlOperation = ControlOperation,
                PartitionService = new PartitionService(ControlOperation)
            };
            
            var elementContext = (RootElement = new ElementContext("root"));
            elementContext.Add("node", Node = new LinkMember(elementContext));
            elementContext.cssClass = "root";

            var Container = ControlOperation.RegisterControl("container");
            
            WindowingService.RegisterContainer(Container);
            
            Container.SetParent(Node);
        }
    }
}