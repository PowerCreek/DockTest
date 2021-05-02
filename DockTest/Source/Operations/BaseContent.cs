using System;
using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.Razor;
using DockTest.Source.Properties;

namespace DockTest.Source.Operations
{
    public class BaseContent
    {
        public ControlAttribute ControlAttribute { get; init; }
        public ElementContext Context { get; init; }
        public LinkMember ElementNode { get; init; }
        public Transform Transform { get; init; }

        public BaseContent(string id)
        {
            Context = new ElementContext(id);
            ElementNode = new LinkMember(Context);
            Transform = new Transform();
            
            Context.Add(ControlAttribute.NODE, ElementNode);
            Context.Add(ControlAttribute.TRANSFORM, Transform);
        }
        
        public void Destroy()
        {
            Action ChangeState = ((ElementContext) ElementNode.Parent.Value).SurrogateReference.ChangeState;
            ElementNode.Pop();
            ChangeState();
        }
    }
}