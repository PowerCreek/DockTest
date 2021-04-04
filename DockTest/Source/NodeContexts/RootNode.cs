using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Operations;

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
 
        public RootNode(IServiceData serviceData)
        {
            ServiceData = serviceData;
            var elementContext = (RootElement = new ElementContext("test"));
            elementContext.Add("node", Node = new LinkMember(elementContext));
            RootElement.SetHtml("what");
            
        }
    }
}