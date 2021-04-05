using System;
using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Properties;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{

    public enum ControlAttribute
    {
        SCROLL_CONTENT,
        NODE,
        TRANSFORM,
        DRAGGABLE,
        MENU,
        DOCK
    }
    
    public class ControlContext : ElementContext
    {
        public LinkMember ElementNode { get; }
        public IJSRuntime JsRuntime { get; init; }

        public ControlContext(string id, IJSRuntime jsRuntime) : base(id)
        {
            JsRuntime = jsRuntime;
            ElementNode = new LinkMember(this);
        }

        public void AddChild(ControlContext child) => ElementNode.Add(child.ElementNode);
        public void SetParent(LinkMember parent) => parent.Add(ElementNode);
        
        public ControlContext WithDragging()
        {
            WithAttribute("draggable", out AttributeString drag);
            drag.Value = "true";
            Add(ControlAttribute.DRAGGABLE, drag);
            return this;
        }

        public ControlContext WithContent<T>(out T content) where T: BaseContent
        {
            content = (T) Activator.CreateInstance(typeof(T), Id, JsRuntime);
            Add(content.ControlAttribute, content);
            ElementNode.Add(content.ElementNode);
            return this;
        }
        
    }

    public class DockGroup : BaseContent
    {
        public DockGroup(string id, IJSRuntime jsRuntime) : base($"{id}_dock")
        {
            ControlAttribute = ControlAttribute.DOCK;
            Context.cssClass = "dockgroup";
        }
    }

    public class MenuContent: BaseContent
    {
            
        public MenuContent(string id, IJSRuntime jsRuntime) : base($"{id}_menu")
        {
            ControlAttribute = ControlAttribute.MENU;
            Context.cssClass = "menu";
        }
    }

    public class ScrollContent: BaseContent
    {
        public string Directions { get; set; } = "VH";

        public StyleContext StyleContext {
            get {
                Context.WithAttribute("style", out StyleContext style);
                return style;
            }
        }

        public ScrollContent(string id, IJSRuntime jsRuntime) : base($"{id}_slider")
        {
            ControlAttribute = ControlAttribute.SCROLL_CONTENT;
            Context.cssClass = "slider";
            Transform.OnMove = (transform, position) =>
            {
                StyleContext.WithStyle(new StyleOperator(jsRuntime), Context,
                    ("margin-left",$"{position.X}px"),
                    ("margin-top",$"{position.Y}px"));
            };
        }
    }
}