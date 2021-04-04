using DockTest.ExternalDeps.Interfaces;

namespace DockTest.ExternalDeps.Classes
{
    public class AttributeString : IAttribute, IRenderableItem
    {
        public string Value { get; set; }
        public object GetRenderOutput() => Value;
    }
}