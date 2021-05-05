using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using BlazorWebLib.Properties;
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
        
        public Position CurrentPosition { get; set; } = new Position(0, 0);
        
        public bool MouseDown = false;

        public Action<MouseEventArgs> MouseMoveAction = (args) => { };
        
        public Action<bool> EndInputAction = (a) => { };
        
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
        
        public Dictionary<Partition, List<string>> PartitionMap = new Dictionary<Partition, List<string>>();

        //child > parent
        public Dictionary<Partition, Partition> PartitionParentMap = new Dictionary<Partition, Partition>();

        public Dictionary<Partition, Identity> PartitionIdentities = new Dictionary<Partition, Identity>();

        public Dictionary<object, Partition> ElementPartitionPair = new Dictionary<object, Partition>();
        
        public void CreateSubPartition(Partition source, out Partition partition)
        {
            partition = new Partition($"{source.Context.Id}", source.JsRuntime);
            List<string> values = null;

            PartitionIdentities.TryAdd(source, Identity.ACROSS);
            PartitionIdentities.TryAdd(partition, Identity.ACROSS);
            
            if (!PartitionParentMap.TryAdd(partition, source))
            {
                if (PartitionMap.ContainsKey(source))
                {
                    PartitionMap[source].Remove(partition.Context.Id);
                }

                PartitionParentMap[partition] = source;
            }
            
            if (!PartitionMap.TryAdd(source, values = new List<string>(new[] {partition.Context.Id})))
            {
                PartitionMap[source].AddRange(values);
            }

            source.ElementNode.Add(partition.ElementNode);

            source.StyleContext.WithStyle(StyleOperator, (ElementContext)source.ElementNode.Value, 
                ("width","100%"),
                ("height","100%"));
            
            //AdjustGridAreas(source);
        }

        public void RemoveSubPartition(Partition item)
        {
            PartitionMap[PartitionParentMap[item]].Remove(item.Context.Id);
            PartitionMap.Remove(item);
            PartitionIdentities.Remove(item);
        }
        
        public async Task SetPartionIdentity(Partition source, Identity identity = Identity.ACROSS)
        {
            if (!PartitionIdentities.TryAdd(source, identity))
            {
                PartitionIdentities[source] = identity;
            }
            
            await AdjustGridAreas(source);
        }
        
        public async Task AdjustGridAreas(Partition source)
        {
            string gridStyle = "";

            switch (PartitionIdentities[source])
            {
                case Identity.ACROSS:
                    gridStyle = $"\"{string.Join(" ", PartitionMap[source])}\"";
                    break;
                case Identity.VERTICAL:
                    gridStyle = string.Join(" ",PartitionMap[source].Select(e => $"\"{e}\""));
                    break;
            }

            await source.StyleContext.WithStyle(StyleOperator, (ElementContext)source.ElementNode.Value,
                ("grid-template-areas", gridStyle));
            Console.WriteLine(gridStyle);
        }

        public void AddElementToPartition(object element, Partition partition)
        {
            if (!ElementPartitionPair.TryAdd(element, partition)) ElementPartitionPair[element] = partition;
        }
        
        public Partition GetElementPartition(object element)
        {
            return ElementPartitionPair[element];
        }

        public ControlContext SetPartitionContent(Partition source, ControlContext content)
        {
            AddElementToPartition(content, source);
            content.SetParent(source.ElementNode);
            
            source.Context.SurrogateReference?.ChangeState();
            return content;
        }

        public InputData PartitionSliderInputData { get; set; } = new InputData();
        public Dictionary<Partition, (int delta, double percent)> PartitionWidth = new Dictionary<Partition, (int delta, double percent)>();
        public Dictionary<Partition, Action> PartitionShiftMap = new Dictionary<Partition, Action>();
        public Dictionary<Partition, int> HandleState = new Dictionary<Partition, int>();

        public const int minSize = 10;
        
        public void RegisterPartitionEvents(Partition partitionA, Partition partitionB)
        {
            
            Partition[] partitions = {partitionA, partitionB};
            Partition parent = PartitionParentMap[partitionA];
            
            async void ShiftPartition(Partition source, int dX, int dY)
            {
                Partition parent;
                Identity identity = PartitionIdentities[parent = PartitionParentMap[source]];

                string TemplateItem = identity == Identity.ACROSS ? "grid-template-columns" : "grid-template-rows";

                (int delta, double percent) = PartitionWidth[parent];
                
                delta = (delta - (identity == Identity.ACROSS? dX : dY));
             
                PartitionWidth[parent] = (delta, percent);

                await parent.StyleContext.WithStyle(StyleOperator, parent.Context, 
                    (TemplateItem, $"min(max({minSize}px,calc({(1-percent)*100}% - {delta}px)), calc(100% - {minSize}px))"));
            }

            async void EndInputCapture(bool selfFire)
            {

                Partition localParent = PartitionParentMap[partitionA];
                
                (int delta, double percent) = PartitionWidth[localParent];
                
                PartitionSliderInputData.State = PartitionSliderInputData.State == 1? 0:PartitionSliderInputData.State;
                
                if (PartitionSliderInputData.State == 2)
                {
                    return;
                }
                
                string space = "                   ";
                PartitionSliderInputData.MouseIsNotDown();
                
                Identity identity = PartitionIdentities[localParent];
                var rectTaskParent = JsRuntime.InvokeAsync<Rect>("GetDimensions", localParent.Context.Id);
                var rectTaskB = JsRuntime.InvokeAsync<Rect>("GetDimensions", partitionB.Context.Id);

                bool across = identity == Identity.ACROSS;
                
                var rectParent = await rectTaskParent;
                var rectB = await rectTaskB;

                double sizeValue = across ? rectParent.width : rectParent.height;
                
                double offset = ((sizeValue * percent) + delta);
                
                double divisor = (across ? rectParent.width : rectParent.height);
                
                double into = offset / divisor;

                PartitionWidth[localParent] = (0, into);

                string remaining = (1 - into)*100+space;

                double offTrim = (minSize / divisor);

                Console.WriteLine(offset +" : "+sizeValue+" : "+minSize +":"+percent);
                Console.WriteLine("... "+ (sizeValue - minSize));
                Console.WriteLine("...");
                if (offset > (sizeValue - minSize))
                {
                    PartitionWidth[localParent] = (0, 1 - (into = offTrim));
                    remaining = (into)*100+space;
                    remaining = $"{minSize}px";
                    Console.WriteLine("1");
                    HandleState[parent] = -1;
                }else
                if (offset < minSize)
                {
                    PartitionWidth[localParent] = (0, (into = offTrim));
                    remaining = (1-into)*100+space;
                    remaining = $"calc(100% - {minSize}px)";
                    Console.WriteLine("2");
                    HandleState[parent] = 1;
                }
                else
                {
                    remaining = $"calc({remaining[0..8].Trim()+"%"})";
                    Console.WriteLine("3   " + into);
                    HandleState[parent] = 0;
                }
                
                Console.WriteLine("end capture");
                
                await localParent.StyleContext.WithStyle(StyleOperator, localParent.Context, 
                    (across ? "grid-template-columns" : "grid-template-rows", 
                        $"clamp(10px, {remaining}, calc(100% - 10px)) ")
                );
            }
            
            async void StartInputCapture(MouseEventArgs mArgs)
            {
                PartitionSliderInputData.State = 1;
                
                PartitionSliderInputData.CurrentPosition = new Position((int)mArgs.ScreenX, (int)mArgs.ScreenY);
                    
                Partition localParent = PartitionParentMap[partitionA];
                (int delta, double percent) = PartitionWidth[localParent];

                delta = 0;
                
                Console.WriteLine("start: "+delta);
                
                PartitionWidth[localParent] = (delta, Math.Clamp(percent, 0,1));
                HandleState[localParent] = (0);

                PartitionSliderInputData.MouseIsDown();
                PartitionSliderInputData.MouseMoveAction = (a) =>
                {
                    Position cPos = new Position((int) a.ScreenX, (int) a.ScreenY);
                        
                    ShiftPartition(partitionA, cPos.X - PartitionSliderInputData.CurrentPosition.X,
                        cPos.Y - PartitionSliderInputData.CurrentPosition.Y);
                        
                    PartitionSliderInputData.CurrentPosition = cPos;
                };

                PartitionSliderInputData.EndInputAction = EndInputCapture;
            }
            
            if (!PartitionShiftMap.TryAdd(parent, () => { EndInputCapture(parent == PartitionSliderInputData.InputTarget); }))
            {
                PartitionShiftMap[parent] = () => { EndInputCapture(parent == PartitionSliderInputData.InputTarget); };
            }
            
            parent.Context.AddEvent("onmouseleave", (args) =>
            {
                if (PartitionSliderInputData.InputTarget == parent && PartitionSliderInputData.MouseDown)
                {
                    PartitionShiftMap[parent]?.Invoke();
                }
            });

            foreach (Partition p in partitions)
            {
                p.Context.StopPropagations.Add("onmousedown");
                
                p.Context.AddEvent("onmousedown", (args) =>
                {
                    PartitionSliderInputData.InputTarget = PartitionParentMap[p];
                    StartInputCapture((MouseEventArgs) args);
                });
            
                p.Context.AddEvent("onmouseup", (args) =>
                {
                    Console.WriteLine("here");
                    PartitionSliderInputData.EndInputAction?.Invoke(parent == PartitionSliderInputData.InputTarget);
                    PartitionSliderInputData.MouseMoveAction = null;
                    PartitionSliderInputData.InputTarget = null;
                });
            
                p.Context.AddEvent("onmousemove", (args) =>
                {
                    MouseEventArgs mArgs = (MouseEventArgs) args;

                    if ((mArgs.Buttons & 1) == 0)
                    {
                        if (PartitionSliderInputData.State == 0)
                        {
                            PartitionSliderInputData.State = 2;
                            PartitionSliderInputData.EndInputAction?.Invoke(true);
                            PartitionSliderInputData.MouseMoveAction = null;
                            PartitionSliderInputData.EndInputAction = null;
                        }
                    }

                    //if ((mArgs.Buttons & 1) == 1 && !PartitionSliderInputData.MouseDown) StartInputCapture(mArgs);
                    if (PartitionSliderInputData.InputTarget == PartitionParentMap[p])
                    {
                        if ((mArgs.Buttons & 1) == 0)
                        {
                            
                            PartitionSliderInputData.MouseMoveAction = null;
                            PartitionSliderInputData.EndInputAction = null;
                        }
                        else if((mArgs.Buttons & 1) == 1 && !PartitionSliderInputData.MouseDown)
                        {
                            //StartInputCapture(mArgs);
                            //return;
                        }
                    }
                    else
                    {
                        if ((mArgs.Buttons & 1) == 0)
                        {
                            //PartitionSliderInputData.MouseMoveAction = null;
                            //PartitionSliderInputData.EndInputAction = null;
                        }
                    }
                    
                    PartitionSliderInputData.MouseMoveAction?.Invoke((MouseEventArgs) args);
                });
            }
        }
        
        public async Task SplitPartition(Partition source, Identity identity, Action<Partition, Partition,Partition> callback)
        {
            CreateSubPartition(source, out Partition partitionA);
            CreateSubPartition(source, out Partition partitionB);
            
            
            int borderWidth = 8;
            
            PartitionWidth.TryAdd(source, (0, 0.5));

            bool across = identity == Identity.ACROSS;
            
            partitionA.Context.cssClass += $" {(across? "":"")}";
            partitionB.Context.cssClass += $" {(across? "across":"vertical")}";
                
            source.StyleContext.WithStyle(StyleOperator, source.Context, (across? "grid-template-columns" : "grid-template-rows", "50% 50%"));
            
            PartitionHandle handleA = new PartitionHandle(partitionA.Context.Id, JsRuntime, StyleOperator);
            handleA.Context.cssClass += $" {(across ? "across" : "vertical")}";
            PartitionHandle handleB = new PartitionHandle(partitionB.Context.Id, JsRuntime, StyleOperator);
            handleB.Context.cssClass += $" {(across ? "across" : "vertical")}";
            
            string[] dim = {"left", "top", "width", "height"};

            string margin = across ? "" : "";
            
            string[][] size = across
                ? 
                new[] {
                    new[] {$"calc(100% - {borderWidth}px)", "0px", $"{borderWidth}px", "100%"},
                    new[] {"0px", "0px", $"{borderWidth}px", "100%"}
                } : 
                new[] {
                    new[] {"0px", $"calc(100% - {borderWidth}px)", "100%", $"{borderWidth}px"},
                    new[] {"0px", "0px", "100%", $"{borderWidth}px"}
                };
            
            handleA.SetStyles(
                ("display", "none"),
                ("position", "absolute"),
                (dim[0], size[0][0]),
                (dim[1], size[0][1]),
                (dim[2], size[0][2]),
                (dim[3], size[0][3]),
                ("background-color","#f8001e22"),
                (margin, $"{borderWidth}px")
            );
            
            handleB.SetStyles(
                //("display", "none"),
                ("position", "absolute"),
                (dim[0], size[1][0]),
                (dim[1], size[1][1]),
                (dim[2], size[1][2]),
                (dim[3], size[1][3]),
                ("background-color","#f8001e22"),
                (margin, $"{borderWidth}px")
            );
                
            partitionA.ElementNode.Add(handleA.ElementNode);
            partitionB.ElementNode.Add(handleB.ElementNode);
            
            RegisterPartitionEvents(partitionA, partitionB);

            await SetPartionIdentity(source, identity);   
            callback(source, partitionA, partitionB);
        }
        
        public ControlContext RegisterPartitionContent(ControlContext content)
        {
            ControlContext contentControl = new ControlContext("partitionContent", JsRuntime);
            
            contentControl.StopPropagations.Add("onmousedown");
            
            content.SetParent(contentControl.ElementNode);
            
            return contentControl;
        }
        
        public void PerformPartitionSplit(ControlContext sourceContext, ControlContext[] controlContexts, Identity identity)
        {
            var sCtx = GetElementPartition(sourceContext);
            
            SplitPartition(sCtx, identity,
                (source, partitionA, partitionB) =>
                {
                    AddElementToPartition(controlContexts[0], partitionA);
                    controlContexts[0].SetParent(partitionA.ElementNode);
                    
                    AddElementToPartition(controlContexts[1], partitionB);
                    controlContexts[1].SetParent(partitionB.ElementNode);

                    partitionA.Context.OnAfterRender += () =>
                    {
                    };

                    sCtx.Context.SurrogateReference?.ChangeState();
                    ((ElementContext) sCtx.ElementNode.Parent.Value).SurrogateReference?.ChangeState();
                });
        }
        
        public void OutputData()
        {
            Console.WriteLine(string.Join(", ", PartitionMap.Select(e => $"{e.Key} : [{string.Join(", ", e.Value)}]")));
        }
    }
}