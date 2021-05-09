using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlazorWebLib.Properties;
using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Management.Operations;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Properties.Vector;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{

    public class InputData
    {

        public int State = 0;
        
        public object InputTarget { get; set; }
        
        public object SourceTarget { get; set; }
        public object AdjacentTarget { get; set; }
        
        public Position CurrentPosition { get; set; } = new Position(0, 0);
        
        public bool MouseDown = false;

        public Action<MouseEventArgs> MouseMoveAction = (args) => { };
        
        public Action<Partition, bool> EndInputAction = (a,b) => { };
        
        public void MouseIsDown()
        {
            MouseDown = true;
        }
        public void MouseIsNotDown()
        {
            MouseDown = false;
        }
    }

    public enum Identity
    {
        ACROSS,
        VERTICAL,
    }

    public class PartitionService
    {

        public StyleOperator StyleOperator { get; }
        public IJSRuntime JsRuntime { get; }
        
        public PartitionService(ControlOperation controlOperation)
        {
            StyleOperator = controlOperation.StyleOperator;
            JsRuntime = controlOperation.JsRuntime;
        }
        
        public Dictionary<Partition, List<ElementContext>> PartitionMap = new Dictionary<Partition, List<ElementContext>>();

        public Dictionary<Partition, int> PartitionIdentities = new Dictionary<Partition, int>();

        public Dictionary<object, Partition> ElementPartitionPair = new Dictionary<object, Partition>();
      
        public InputData PartitionSliderInputData { get; set; } = new InputData();
        public Dictionary<Partition, (int delta, double percentA, double percentB)> PartitionWidth = new Dictionary<Partition, (int delta, double percentA, double percentB)>();
        public Dictionary<Partition, Action> PartitionShiftMap = new Dictionary<Partition, Action>();
        
        public async Task SetPartionIdentity(Partition source, int identity = 1)
        {
            if (!PartitionIdentities.TryAdd(source, identity)) PartitionIdentities[source] = identity;
            
            await source.StyleContext.WithStyle(StyleOperator, source.Context, ("flex-direction", identity % 2 == 1? "row" : "column"));
        }
        
        public ControlContext SetPartitionContent(Partition source, ControlContext content)
        {
            AddElementToPartition(content, source);
            content.SetParent(source.ElementNode);
            
            source.Context.SurrogateReference?.ChangeState();
            return content;
        }

        public ControlContext RegisterPartitionContent(ControlContext content)
        {
            ControlContext contentControl = new ControlContext("partitionContent", JsRuntime);
            contentControl.cssClass = "partition-content";
            contentControl.WithStyles(out StyleContext style);
            style.WithStyle(StyleOperator, contentControl, 
                ("flex-grow", "1"));
            
            contentControl.StopPropagations.Add("onmousedown");
            content.SetParent(contentControl.ElementNode);
            
            return contentControl;
        }
        
        public void AddElementToPartition(object element, Partition partition)
        {
            if (!ElementPartitionPair.TryAdd(element, partition)) ElementPartitionPair[element] = partition;
        }
        
        public void CreateSubPartition(Partition source, out Partition partition)
        {
            partition = new Partition($"{source.Context.Id}", source.JsRuntime);
            List<Partition> values = null;

            PartitionIdentities.TryAdd(source, 1);
            PartitionIdentities.TryAdd(partition, 1);
            
            // if (!PartitionParentMap.TryAdd(partition, source))
            // {
            //     PartitionParentMap[partition] = source;
            // }

            source.ElementNode.Add(partition.ElementNode);

            source.Context.cssClass = source.Context.cssClass.Replace(" fill", "");
            source.Context.cssClass += " fill";
            
            source.StyleContext.WithStyle(StyleOperator, (ElementContext)source.ElementNode.Value, 
                ("flex-grow", "1"));
            
            // if (!PartitionShiftMap.TryAdd(source, () => { EndInputCapture(source, source == PartitionSliderInputData.InputTarget); }))
            // {
            //     PartitionShiftMap[source] = () => { EndInputCapture(source, source == PartitionSliderInputData.InputTarget); };
            // }
            //
            source.Context.AddEvent("onmouseleave", args =>
            {
                if (PartitionSliderInputData.InputTarget == source && PartitionSliderInputData.MouseDown)
                    PartitionShiftMap[source]?.Invoke();
            });
        }

        Partition SetupPartitionContainer(dynamic content = null)
        {
            Partition p = new Partition("sub-partition", JsRuntime);
            p.Context.cssClass = p.Context.cssClass.Replace(" fill", "").Replace(" sub", "");
            p.Context.cssClass += " fill sub";

            p.Context.StopPropagations.Add("onmousedown");
            
            p.Context.AddEvent("onmousedown", (args) =>
            {
                Console.WriteLine("doing click: "+p.Context.Id);
            });
            
            if (content is not null)
            {
                p.ElementNode.Add(content.ElementNode);
            }
            return p;
        }

        int borderWidth = 8;

        async Task ShiftPartition(ElementContext source, int dX, int dY)
        {
                
        }
        
        async Task StartInputCapture(ElementContext source, MouseEventArgs mArgs)
        {
                
        }
        async Task EndInputCapture(ElementContext source, bool selfFire)
        {
                
        }
        
        public void RegisterPartitionEvents(ElementContext ctx)
        {

            ctx.StopPropagations.Add("onmousedown");
            
            ctx.AddEvent("onmousedown", args =>
            {
                PartitionSliderInputData.InputTarget = ElementPartitionPair[ctx];
                StartInputCapture(ctx, (MouseEventArgs) args);
            });
            
            ctx.AddEvent("onmousemove", args =>
            {
                MouseEventArgs mArgs = (MouseEventArgs) args;
                PartitionSliderInputData.InputTarget = ElementPartitionPair[ctx];
                StartInputCapture(ctx, (MouseEventArgs) args);
            });
            
        }
        
        public void RemoveBar(LinkMember container)
        {
            if (container.GetChildren().FirstOrDefault()?.Value is ElementContext firstChild)
            {
                if ((firstChild.cssClass??"").Contains("handle"))
                {
                    container[0].Pop();
                }
            }
        }
        
        public void AddBar(LinkMember container, int dir)
        {
            bool across = dir % 2 == 1;

            ElementContext ctx = (ElementContext) container.Value;
            
            PartitionHandle handle = new PartitionHandle((ctx).Id, JsRuntime, StyleOperator);
            handle.Context.cssClass += $" {(across ? "across" : "vertical")}";
            
            string[] dim = {"left", "top", "width", "height"};
            
            string[] size = across
                ? 
                new[] {"-8px", "0px", $"{borderWidth}px", "100%"} : 
                new[] {"0px", "-8px", "100%", $"{borderWidth}px"};
            
            handle.SetStyles(
                ("position", "absolute"),
                (dim[0], size[0]),
                (dim[1], size[1]),
                (dim[2], size[2]),
                (dim[3], size[3])
            );
            
            RemoveBar(container);
            
            container.Prepend(handle.ElementNode);
        }

        public void SetCss(ElementContext context, string css)
        {
            context.cssClass = 
                (context.cssClass ?? "").Replace($" across", "").Replace(" vertical", "") + $" {css}";
        }

        public async Task AppendOverElement(ControlContext target, ControlContext into, int placement)
        {
            try
            {
                Partition parent = ElementPartitionPair[target];

                Partition targetContainer = null;
                
                if (!PartitionMap.TryGetValue(parent, out var items))
                {
                    PartitionMap.Add(parent, items = new List<ElementContext>());
                    items.Add(target);
                    parent.ElementNode.Add((targetContainer = SetupPartitionContainer(target)).ElementNode);
                }

                int identity = PartitionIdentities[parent];
                
                if (ElementPartitionPair.TryGetValue(into, out var intoParent))
                    if (PartitionMap.TryGetValue(intoParent, out var intoList))
                    {
                        bool removedInto;
                        int intoIndex = intoList.IndexOf(into);
                        if ((removedInto = intoList.Remove(into)) && intoList.Count == 0)
                        {
                            intoParent.Destroy();
                            Console.WriteLine("destroyed");
                        }
                        else if (removedInto && intoIndex == 0)
                        {
                            RemoveBar(into.ElementNode.Parent.NextSibling);
                        }
                    }

                var intoControlparent = into.ElementNode.Parent;

                string cssVal = placement % 2 == 1 ? "across" : "vertical";

                if (identity % 2 == placement % 2)
                {

                    string targetCss;
                    if ((targetCss = (targetContainer?.Context.cssClass)) is not null)
                    {
                        if (!targetCss.Contains("across") && !targetCss.Contains("vertical"))
                        {
                            SetCss(targetContainer.Context, cssVal);
                        }
                    }
                    
                    if (!ElementPartitionPair.TryAdd(into, parent)) ElementPartitionPair[into] = parent;

                    bool removed = items.Remove(into);

                    Partition intoPartition = null;
                    
                    LinkMember p = removed ? into.ElementNode.Parent : (intoPartition = SetupPartitionContainer(into)).ElementNode;
                    if (intoPartition is not null)
                    {
                        SetCss(intoPartition.Context, cssVal);
                    }

                    items.Insert(items.IndexOf(target) + (placement <= 2 ? 0 : 1), into);
                    
                    if (placement <= 2)
                    {
                        target.ElementNode.Parent.InsertBefore(p);

                        if (items[1] == target)
                        {
                            AddBar(target.ElementNode.Parent, placement % 2);
                            target.cssClass = (target.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }
                        else
                        {
                            AddBar(into.ElementNode.Parent, placement % 2);
                            into.cssClass = (into.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }
                    }
                    else
                    {

                        target.ElementNode.Parent.InsertAfter(p);

                        if (items[0] == target)
                        {
                            target.cssClass = (target.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }

                        AddBar(into.ElementNode.Parent, placement % 2);

                        into.cssClass = (into.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                    }
                    
                    intoControlparent?.Pop();

                }
                else
                {
                    var targetParent = target.ElementNode.Parent;
                    var hold = new LinkMember(null);

                    Partition generatedPartition = SetupPartitionContainer();

                    SetCss(generatedPartition.Context, cssVal);
                        
                    await SetPartionIdentity(generatedPartition, placement);

                    var children = targetParent.GetChildren().ToList();
                    targetParent.Replace(targetParent, generatedPartition.ElementNode);

                    children.ForEach(e => generatedPartition.ElementNode.Add(e));
                    items[items.IndexOf(target)] = generatedPartition.Context;
                    
                    ElementPartitionPair[target] = generatedPartition;

                    await AppendOverElement(target, into, placement);
                }
                
                ((ElementContext)parent.ElementNode.Parent.Value).SurrogateReference?.ChangeState();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
    }
}