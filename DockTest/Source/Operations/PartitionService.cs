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
        
        public Partition InputTarget { get; set; }
        
        public Partition SourceTarget { get; set; }
        
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
        
        public Dictionary<Partition, List<IElementNode>> PartitionMap = new Dictionary<Partition, List<IElementNode>>();

        public Dictionary<Partition, int> PartitionIdentities = new Dictionary<Partition, int>();

        public Dictionary<object, Partition> ElementPartitionPair = new Dictionary<object, Partition>();
        
        public Dictionary<object, Partition> SubPartitionRegistry = new Dictionary<object, Partition>();

        public Dictionary<object, double> PercentMap = new Dictionary<object, double>();
      
        public InputData PartitionSliderInputData { get; set; } = new InputData();
        public Dictionary<Partition, (int delta, double percentA, double percentB, double max)> PartitionWidth = new Dictionary<Partition, (int delta, double percentA, double percentB, double max)>();
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
            
            if (content is not null)
            {
                p.ElementNode.Add(content.ElementNode);
            }
            
            PercentMap.TryAdd(p, -1);
            
            RegisterPartitionEvents(p);
            
            return p;
        }

        int borderSize = 8;
        
        public const int minSize = 8;

        async Task ShiftPartition(Partition partition, int dX, int dY)
        {
            try
            {
            Partition neighbor = SubPartitionRegistry[partition.ElementNode.PreviousSibling.Value];
            
            int identity = PartitionIdentities[PartitionSliderInputData.InputTarget] % 2;
            identity = identity == 0 ? 2: identity;
            
            string[] sizeItems = identity == 1 ? new []{"min-width", "max-width"} : new []{"min-height", "max-height"};

            (int delta, double percentA, double percentB, double max) = PartitionWidth[PartitionSliderInputData.InputTarget];

            delta = (delta - (identity == 1? dX : dY));

            PartitionWidth[PartitionSliderInputData.InputTarget] = (delta, percentA, percentB, max);
            //Console.WriteLine(percentA+":"+percentB);
            string propertyStringA = $"clamp({minSize}px, calc({(percentA)*100}% - {delta}px), calc(min({100}%, {max}px) - {minSize}px))";
            string propertyStringB = $"clamp({minSize}px, calc({(percentB)*100}% + {delta}px), calc(min({100}%, {max}px) - {minSize}px))";
            
            neighbor.Context.WithAttribute("style", out StyleContext sctxA);
            var T1 =  sctxA.WithStyle(StyleOperator, neighbor.Context,
                (sizeItems[0], propertyStringA),
                (sizeItems[1], propertyStringA)
                );
            
            partition.Context.WithAttribute("style", out StyleContext sctxB);
            var T2 =  sctxB.WithStyle(StyleOperator, partition.Context,
                (sizeItems[0], propertyStringB),
                (sizeItems[1], propertyStringB)
                );
            
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async void InsertPartition(IElementNode into, IElementNode target)
        {
            try
            {
                int identity = PartitionIdentities[ElementPartitionPair[target]] % 2;
                identity = identity == 0 ? 2 : identity;
                bool across = identity == 1;
                string[] sizeItems = across ? new[] {"min-width", "max-width"} : new[] {"min-height", "max-height"};

                var tParent = target.ElementNode.Parent;
                var iParent = into.ElementNode.Parent;
                bool intoIsBefore = true;
                LinkMember adjacent = (intoIsBefore = tParent.PreviousSibling == iParent) ? iParent.PreviousSibling : tParent.PreviousSibling;
                
                int sizeA = adjacent is not null && SubPartitionRegistry.ContainsKey(adjacent.Value) ? borderSize: 0;

                Partition A = SubPartitionRegistry[tParent.Value];
                Partition B = null;
                if (intoIsBefore)
                {
                    B = A;
                    A = SubPartitionRegistry[iParent.Value];

                }
                else
                {
                    B = SubPartitionRegistry[iParent.Value];
                }
                
                double percent = PercentMap[SubPartitionRegistry[tParent.Value]];
                Console.WriteLine(percent +"%");
                
                if (percent >= 0)
                {
                    double rem = (percent / 2);
                    PercentMap[A] = rem;
                    PercentMap[B] = rem;
                }
                
                Console.WriteLine(ElementPartitionPair[target].Context.Id);
                //83
                Console.WriteLine(244+" [line]");
                if (ElementPartitionPair[target].ElementNode.GetChildren().FirstOrDefault() is not null)
                {
                    Console.WriteLine(PartitionMap[ElementPartitionPair[target]].IndexOf(target));
                    if (PartitionMap[ElementPartitionPair[target]].IndexOf(into) > 1)
                    {
                        AddBar(PartitionMap[ElementPartitionPair[into]][0].ElementNode, PartitionIdentities[ElementPartitionPair[into]], true);
                    }

                    //Console.WriteLine(((ElementContext)ElementPartitionPair[target].ElementNode[0].Parent?.Value)?.Id+":val");
                    AdjustPercents(ElementPartitionPair[target], ElementPartitionPair[target].ElementNode[0]);
                }
                else
                {
                    AddBar(ElementPartitionPair[target].ElementNode, identity);
                    Console.WriteLine("AAAAA");
                    //AdjustPercents(ElementPartitionPair[target], ElementPartitionPair[target].ElementNode[0]);
                }


            }catch (Exception e)
            {
                Console.WriteLine(e);   
            } 
        }

        async Task AssignDimensions(Partition partition, Partition target = null)
        {
            if (
                !SubPartitionRegistry.TryGetValue(partition.ElementNode.Parent.Value, out Partition parentPartition)
                || partition.ElementNode.PreviousSibling is null
                || !SubPartitionRegistry.ContainsKey(partition.ElementNode.PreviousSibling.Value)
            )
                return;
            try
            {
                Partition neighbor = SubPartitionRegistry[partition.ElementNode.PreviousSibling.Value];
                int identity = PartitionIdentities[target??parentPartition] % 2;
                
                identity = identity == 0 ? 2 : identity;
                bool across = identity == 1;
                string[] sizeItems = across ? new[] {"min-width", "max-width"} : new[] {"min-height", "max-height"};

                var parentRect = JsRuntime.InvokeAsync<Rect>("GetDimensions", parentPartition.Context.Id);
                var neighborRect = JsRuntime.InvokeAsync<Rect>("GetDimensions", neighbor.Context.Id);
                var sourceRect = JsRuntime.InvokeAsync<Rect>("GetDimensions", partition.Context.Id);

                double divisor = (across ? (await parentRect).width : (await parentRect).height);
                double offsetA = (across ? (await neighborRect).width : (await neighborRect).height);
                double offsetB = (across ? (await sourceRect).width : (await sourceRect).height);

                int sizeA = neighbor.ElementNode.PreviousSibling is not null && SubPartitionRegistry.ContainsKey(neighbor.ElementNode.PreviousSibling.Value) ? borderSize: 0;
                
                double intoA = (sizeA+offsetA) / divisor;
                double intoB = (offsetB+borderSize) / divisor;

                PercentMap[neighbor] = intoA;
                PercentMap[partition] = intoB;
                
                Console.WriteLine($"{neighbor.Context.Id} {intoA}");
                Console.WriteLine($"{partition.Context.Id} {intoB}");
                
                double remainingA = (intoA) * 100;
                double remainingB = (intoB) * 100;

                await neighbor.StyleContext.WithStyle(StyleOperator, neighbor.Context,
                    (sizeItems[0], $"clamp({minSize/2}px, {remainingA}% - {sizeA}px, calc(100% - {minSize/2}px))"),
                    (sizeItems[1], $"clamp({minSize/2}px, {remainingA}% - {sizeA}px, calc(100% - {minSize/2}px))")
                );

                await partition.StyleContext.WithStyle(StyleOperator, partition.Context,
                    (sizeItems[0], $"clamp({minSize/2}px, {remainingB}% - {borderSize}px, calc(100% - {minSize/2}px))"),
                    (sizeItems[1], $"clamp({minSize/2}px, {remainingB}% - {borderSize}px, calc(100% - {minSize/2}px))")
                );            
                
                Console.WriteLine("here");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
        async void EndInputCapture(Partition partition, bool selfFire)
        {
            try
            {
                if (
                    PartitionSliderInputData.InputTarget is null
                )
                    return;

                PartitionSliderInputData.State =
                    PartitionSliderInputData.State == 1 ? 0 : PartitionSliderInputData.State;

                if (PartitionSliderInputData.State == 2)
                {
                    Console.WriteLine("EndCapture state 2");
                    return;
                }
                Console.WriteLine("EndCapture state 0");

                PartitionSliderInputData.MouseIsNotDown();
                
                await AssignDimensions(partition, PartitionSliderInputData.InputTarget);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }
        
        async Task StartInputCapture(Partition partition, MouseEventArgs mArgs)
        {
            PartitionSliderInputData.State = 1;
                
            Console.WriteLine("starting capture: "+partition.Context.Id);

            if (!SubPartitionRegistry.TryGetValue(partition.ElementNode.Parent.Value, out Partition parentPartition))
                return;

            PartitionSliderInputData.InputTarget = parentPartition;
            PartitionSliderInputData.SourceTarget = partition;
            
            int identity = PartitionIdentities[parentPartition] % 2;
            identity = identity == 0 ? 2 : identity;
            bool across = identity == 1;
            
            Partition neighbor = SubPartitionRegistry[partition.ElementNode.PreviousSibling.Value];
            
            var parentRect = await JsRuntime.InvokeAsync<Rect>("GetDimensions", parentPartition.Context.Id);
            var neighborRect = await JsRuntime.InvokeAsync<Rect>("GetDimensions", neighbor.Context.Id);
            var sourceRect = await JsRuntime.InvokeAsync<Rect>("GetDimensions", partition.Context.Id);

            double parentSize = across ? (parentRect).width : (parentRect).height;
            double neighborSize = across ? (neighborRect).width : (neighborRect).height;
            double sourceSize = across ? (sourceRect).width : (sourceRect).height;
            //Console.WriteLine(parentSize+":"+neighborSize+":"+sourceSize);
            //Console.WriteLine(parentPartition.Context.Id+":"+neighbor.Context.Id+":"+partition.Context.Id);
            
            PartitionWidth[parentPartition] = (0, neighborSize / parentSize, sourceSize / parentSize, (neighborSize+sourceSize));
                
            PartitionSliderInputData.CurrentPosition = new Position((int) mArgs.ScreenX, (int) mArgs.ScreenY);
                
            PartitionSliderInputData.MouseIsDown();
            PartitionSliderInputData.MouseMoveAction = (a) =>
            {
                Position cPos = new Position((int) a.ScreenX, (int) a.ScreenY);
                        
                ShiftPartition(partition, cPos.X - PartitionSliderInputData.CurrentPosition.X,
                    cPos.Y - PartitionSliderInputData.CurrentPosition.Y);
                        
                PartitionSliderInputData.CurrentPosition = cPos;
            };
            
            PartitionSliderInputData.EndInputAction = EndInputCapture;
        }
        
        public void RegisterPartitionEvents(Partition partition)
        {
            SubPartitionRegistry.Add(partition.ElementNode.Value, partition);
            
            LinkMember node = partition.ElementNode;
            
            ElementContext ctx = (ElementContext) partition.Context;
            
            ctx.StopPropagations.Add("onmousedown");
                
            ctx.AddEvent("onmouseleave", (args) =>
            {
                if (node.Parent == PartitionSliderInputData.InputTarget?.ElementNode && !PartitionSliderInputData.MouseDown)
                {
                    PartitionSliderInputData.EndInputAction?.Invoke(partition, true);
                }
            });
            
            ctx.AddEvent("onmousedown", (args) =>
            {
                if (SubPartitionRegistry.TryGetValue(partition.ElementNode.Parent.Value, out Partition parentPartition))
                    PartitionWidth.TryAdd(parentPartition, (0, 0, 0, 0));
                
                StartInputCapture(partition, (MouseEventArgs) args);
            });
        
            ctx.AddEvent("onmouseup", (args) =>
            {
                PartitionSliderInputData.EndInputAction?.Invoke(PartitionSliderInputData.SourceTarget, node.Parent == PartitionSliderInputData.InputTarget?.ElementNode);
                PartitionSliderInputData.MouseMoveAction = null;
                PartitionSliderInputData.InputTarget = null;
            });
        
            ctx.AddEvent("onmousemove", (args) =>
            {
                MouseEventArgs mArgs = (MouseEventArgs) args;

                if ((mArgs.Buttons & 1) == 0)
                {
                    if (PartitionSliderInputData.State == 0)
                    {
                        PartitionSliderInputData.State = 2;
                        PartitionSliderInputData.EndInputAction?.Invoke(partition, true);
                        PartitionSliderInputData.MouseMoveAction = null;
                        PartitionSliderInputData.EndInputAction = null;
                        PartitionSliderInputData.MouseMoveAction?.Invoke((MouseEventArgs) args);
                        return;
                    }

                    if (PartitionSliderInputData.State == 1)
                    {
                        PartitionSliderInputData.EndInputAction?.Invoke(PartitionSliderInputData.SourceTarget, PartitionSliderInputData.SourceTarget == partition);
                        PartitionSliderInputData.MouseMoveAction = null;
                        PartitionSliderInputData.EndInputAction = null;
                        PartitionSliderInputData.MouseMoveAction?.Invoke((MouseEventArgs) args);
                        return;
                    }
                }
                else if((mArgs.Buttons & 1) == 1 && node.Parent == PartitionSliderInputData.InputTarget?.ElementNode)
                {
                    if (PartitionSliderInputData.State == 0)
                    {
                        PartitionSliderInputData.MouseMoveAction?.Invoke((MouseEventArgs) args);
                        StartInputCapture(partition, (MouseEventArgs) args);
                        return;
                    }
                }
                else if(SubPartitionRegistry.ContainsKey(node.Parent) && node.Parent != PartitionSliderInputData.InputTarget?.ElementNode)
                {
                    Console.WriteLine(((ElementContext)node.Parent.Value).Id);
                }

                if (PartitionSliderInputData.InputTarget?.ElementNode == node.Parent)
                {
                    if ((mArgs.Buttons & 1) == 0)
                    {
                        PartitionSliderInputData.MouseMoveAction = null;
                        PartitionSliderInputData.EndInputAction = null;
                    }
                }
                
                PartitionSliderInputData.MouseMoveAction?.Invoke((MouseEventArgs) args);
            });
            
        }
        
        public void RemoveBar(LinkMember container)
        {
            if (container.GetChildren().FirstOrDefault()?.Value is ElementContext firstChild)
            {
                Console.WriteLine("Removing here::::"+(firstChild).Id);
                if ((firstChild.cssClass??"").Contains("handle"))
                {
                    container[0].Pop();
                }
            }
        }
        
        public void AddBar(LinkMember container, int dir, bool remove = true)
        {
            bool across = dir % 2 == 1;

            ElementContext ctx = (ElementContext) container.Value;
            
            PartitionHandle handle = new PartitionHandle((ctx).Id, JsRuntime, StyleOperator);
            handle.Context.cssClass += $" {(across ? "across" : "vertical")}";
            
            string[] dim = {"left", "top", "width", "height"};
            
            string[] size = across
                ? 
                new[] {$"-{borderSize}px", "0px", $"{borderSize}px", "100%"} : 
                new[] {"0px",$"-{borderSize}px", "100%", $"{borderSize}px"};
            
            handle.SetStyles(
                ("position", "absolute"),
                (dim[0], size[0]),
                (dim[1], size[1]),
                (dim[2], size[2]),
                (dim[3], size[3])
            );
            if(remove) RemoveBar(container);
            
            container.Prepend(handle.ElementNode);
        }

        public void SetCss(ElementContext context, string css)
        {
            context.cssClass = 
                (context.cssClass ?? "").Replace($" across", "").Replace(" vertical", "") + $" {css}";
        }

        public void MoveElements(Partition p, List<IElementNode> items, int identity)
        {
            Console.WriteLine("moving");
            
            items.ForEach(e =>
            {
                if (typeof(Partition) == e.GetType())
                {
                    Console.WriteLine("Is Partition: "+PartitionMap.ContainsKey((Partition)e));
                }
                Console.WriteLine("Is ElementPaired: "+ElementPartitionPair.ContainsKey(e));

                Console.WriteLine(e+"::moved");
            });
        }

        async void AdjustPercents(Partition parent, LinkMember items)
        {
            //return;
            int identity = PartitionIdentities[parent] % 2 == 0? 2: 1;
            var parentRect = JsRuntime.InvokeAsync<Rect>("GetDimensions", parent.Context.Id);
            
            //Console.WriteLine("old parent: "+((ElementContext) items.Parent.Value).Id);

            var elements = parent.ElementNode.GetChildren().Where(e =>
                SubPartitionRegistry.ContainsKey(e.Value)).ToList();

            if (items is not null)
            {
                elements.Remove(items.Parent);
            }

            Console.WriteLine("children: "+string.Join(", ", elements.Select(e=>((ElementContext) e.Value).Id)));

            var percents = elements.Select(e =>
            {
                Console.WriteLine(((ElementContext)e.Value).Id+"<<<");
                Partition hold = null;
                try
                {
                    hold = SubPartitionRegistry[e.Value];
                }
                catch (Exception err)
                {
                    Console.WriteLine(err);
                }

                return PercentMap[hold];
            }).ToArray();

            double PERCENT_100 = 1;
            int unsetCount = percents.Count(e => e < 0);

            Action<double> adjust = (d) =>
            {
                
            };

            double rebuild = 0;
            
            string[] sizeItems = identity == 1 ? new[] {"min-width", "max-width"} : new[] {"min-height", "max-height"};
            
            for(int i = 0; i < percents.Count(); i++)
            {
                double perc = percents[i];
                Partition ePart = SubPartitionRegistry[elements[i].Value];
                int bSize = i == 0 ? 0 : borderSize;
                if (perc >= 0)
                {
                    PERCENT_100 -= perc;
                    adjust += (d) =>
                    {
                        double adjusted = (d + perc)*100;
                        ePart.StyleContext.WithStyle(StyleOperator, ePart.Context,
                            (sizeItems[0], $"clamp({minSize/2}px, {adjusted}% - {bSize}px, calc(100% - {minSize/2}px))"),
                            (sizeItems[1], $"clamp({minSize/2}px, {adjusted}% - {bSize}px, calc(100% - {minSize/2}px))")
                        );    
                    };
                }
            }

            int amount = percents.Length;
            
            if (unsetCount == 0)
            {
                
            }

            double remaining = PERCENT_100 / (percents.Length);
            
            Console.WriteLine("remaining: "+remaining);

            adjust(remaining);

        }

        public async Task AppendOverElement(IElementNode target, IElementNode into, int placement)
        {
            try
            {
                Partition parent = ElementPartitionPair[target];

                Partition targetContainer = null;
                
                if (!PartitionMap.TryGetValue(parent, out var items))
                {
                    PartitionMap.Add(parent, items = new List<IElementNode>());
                    items.Add(target);
                    parent.ElementNode.Add((targetContainer = SetupPartitionContainer(target)).ElementNode);
                }

                int identity = PartitionIdentities[parent];
                
                bool removedInto = false;
                LinkMember immediateSibling = into.ElementNode.PreviousSibling??into.ElementNode.NextSibling;
                
                if (ElementPartitionPair.TryGetValue(into, out var intoParent))
                    if (PartitionMap.TryGetValue(intoParent, out var intoList))
                    {
                        int intoIndex = intoList.IndexOf(into);
                        if ((removedInto = intoList.Remove(into)) && intoList.Count == 0)
                        {
                            intoParent?.Destroy();
                            Console.WriteLine("destroyed::::");
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

                    ElementPartitionPair.TryGetValue(into, out Partition previousIntoParent);

                    if (!ElementPartitionPair.TryAdd(into, parent)) ElementPartitionPair[into] = parent;

                    if (removedInto)
                    {
                        Console.WriteLine(661+" [line]");
                        Console.WriteLine(intoParent.Context.Id);
                        Console.WriteLine(PartitionIdentities[intoParent]);
                        Console.WriteLine(((ElementContext)PartitionMap[intoParent][0].ElementNode.Value).Id);
                        AdjustPercents(intoParent, immediateSibling);
                    }

                    bool removed = items.Remove(into);

                    Partition intoPartition = null;
                    
                    LinkMember p = removed ? into.ElementNode.Parent : (intoPartition = SetupPartitionContainer(into)).ElementNode;
                    if (intoPartition is not null)
                    {
                        SetCss(intoPartition.Context, cssVal);
                    }

                    Console.WriteLine("removed: "+removed);
                    
                    if (previousIntoParent is not null && PartitionMap.ContainsKey(previousIntoParent) && PartitionMap[previousIntoParent].Count == 1)
                    {
                        var root = SubPartitionRegistry[previousIntoParent.ElementNode.Parent.Value];

                        var onlyList = PartitionMap[previousIntoParent];
                        var childItem = onlyList[0];
                        var onlyChild = (ElementContext)childItem.ElementNode.Value;
                        
                        int rootIdentity = PartitionIdentities[root] % 2 == 0 ? 2 : 1;
                        
                        Partition search;
                        bool searched = SubPartitionRegistry.TryGetValue(onlyChild, out search) ||
                            ElementPartitionPair.TryGetValue(onlyChild, out search);
                        
                        Console.WriteLine(PartitionIdentities.ContainsKey(search)+"in registry");
                        int onlyIdentity = PartitionIdentities[search] % 2 == 0 ? 2 : 1;

                        if (rootIdentity == onlyIdentity)
                        {
                            if (onlyList.Count > 0)
                            {
                                PartitionMap.Remove(previousIntoParent);
                                PartitionMap.Remove(SubPartitionRegistry[onlyChild]);
                                SubPartitionRegistry.Remove(previousIntoParent);
                                SubPartitionRegistry.Remove(onlyChild);

                                var childItems = childItem.ElementNode.GetChildren().ToArray();
                                previousIntoParent.ElementNode.Replace(previousIntoParent.ElementNode, childItems[0]);
                                for (int i = 1; i < childItems.Length; i++)
                                {
                                    childItems[i-1].InsertAfter(childItems[i]);
                                }
                            }
                        }

                        Console.WriteLine("...");
                    }
                    //Console.WriteLine(((ElementContext)target.ElementNode.Value).Id);
                    
                    items.Insert(items.IndexOf(target) + (placement <= 2 ? 0 : 1), into);

                    var tConvert = (ElementContext) target.ElementNode.Value;
                    var iConvert = (ElementContext) into.ElementNode.Value;
                    
                    if (placement <= 2)
                    {
                        target.ElementNode.Parent.InsertBefore(p);

                        if (items[1] == target)
                        {
                            AddBar(target.ElementNode.Parent, placement % 2);
                            tConvert.cssClass = (tConvert.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }
                        else
                        {
                            AddBar(into.ElementNode.Parent, placement % 2);
                            iConvert.cssClass = (iConvert.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }
                    }
                    else
                    {
                        
                        target.ElementNode.Parent.InsertAfter(p);

                        if (items[0] == target)
                        {
                            tConvert.cssClass = (tConvert.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                        }

                        Console.WriteLine(((ElementContext)into.ElementNode.Value).Id);
                        Console.WriteLine("Prev: "+removedInto);
                        Console.WriteLine("Prev: "+previousIntoParent?.Context.Id);
                        Console.WriteLine("Prev: "+(removedInto?string.Join(", ",previousIntoParent?.ElementNode.GetChildren().Select(
                            e =>
                            {
                                return ((ElementContext) e.Value).Id;
                            })):""));
                        
                        AddBar(into.ElementNode.Parent, placement % 2);
                        if (removedInto)
                        {
                            
                            //AddBar(previousIntoParent.ElementNode, PartitionIdentities[previousIntoParent] % 2 == 0? 2: 1);
                        }

                        //AddBar(previousIntoParent.ElementNode, placement % 2, false);
                        Console.WriteLine("BBBBBB");
                        iConvert.cssClass = (iConvert.cssClass ?? "pad").Replace($" across", "").Replace(" vertical", "") + $" {cssVal}";
                    }
                    
                    intoControlparent?.Pop();
                    InsertPartition(into, target);
                }
                else
                {
                    var targetParent = target.ElementNode.Parent;
                    var hold = new LinkMember(null);

                    Partition generatedPartition = SetupPartitionContainer();

                    SetCss(generatedPartition.Context, cssVal);
                        
                    await SetPartionIdentity(generatedPartition, placement);

                    var children = targetParent.GetChildren().ToList();
                    Console.WriteLine(((ElementContext)children[0].Value).Id+"::::count");
                    
                    Console.WriteLine(((ElementContext) target.ElementNode.Value).Id);
                    
                    targetParent.Replace(targetParent, generatedPartition.ElementNode);

                    children.ForEach(e => generatedPartition.ElementNode.Add(e));
                    
                    items[items.IndexOf(target)] = generatedPartition;
                    
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