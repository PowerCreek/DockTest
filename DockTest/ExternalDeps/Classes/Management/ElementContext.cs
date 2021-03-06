using System;
using System.Collections.Generic;
using DockTest.ExternalDeps.Classes.ElementProps;
using DockTest.Razor;
using Microsoft.AspNetCore.Components;

// ReSharper disable CollectionNeverUpdated.Global

namespace DockTest.ExternalDeps.Classes.Management
{

    public class ElementContext : ElementProperties
    {
        private static int _id;

        public string Key { get; set; }
        
        public string cssClass { get; set; }
        
        public ElementContext(string id) : base(id = $"{id}_{_id++}")
        {
            Key = id;
        }
        
        public ElementReference ElementReference { get; set; }
        public Surrogate SurrogateReference { get; set; }
        
        public Dictionary<string, EventCallback> EventMap { get; } = new();
        public Dictionary<string, Action<object>> ActionMap { get; } = new();
        public List<string> PreventDefaults { get; } = new();
        public List<string> StopPropagations { get; } = new();
        public RenderFragment HTML { get; set; }

        public void SetHtml(string html)
        {
            HTML = Surrogate.CreateElement(html);
        }

        public EventCallback GetEvent(string name) => EventMap.TryGetValue(name, 
            out EventCallback item) 
            ? item : default;
        
        public void AddEvent(string name, Action<object> action)
        {
            if (!ActionMap.TryAdd(name, action))
            {
                ActionMap[name] += action;
            }
            if (!EventMap.TryAdd(name, EventCallback.Factory.Create(this, ActionMap[name])))
            {
                EventMap[name] = EventCallback.Factory.Create(this, ActionMap[name]);
            }

            SurrogateReference?.ChangeState();
        }

    }
}