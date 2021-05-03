using System;
using System.Collections.Generic;
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

        public object InputTarget { get; set; }
        
        public Position CurrentPosition { get; set; } = new Position(0, 0);
        
        public bool MouseDown = false;

        public Action<MouseEventArgs> MouseMoveAction = (args) => { };
        
        public Action EndInputAction = () => { };
        
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
            Partition parent;
            PartitionMap[parent = PartitionParentMap[item]].Remove(item.Context.Id);
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
       
        public void RegisterPartitionEvents(Partition partitionA, Partition partitionB)
        {
            
            Partition[] partitions = {partitionA, partitionB};
            Partition parent = PartitionParentMap[partitionA];
            
            void ShiftPartition(Partition source, int dX, int dY)
            {
                Partition parent;
                Identity identity = PartitionIdentities[parent = PartitionParentMap[source]];

                string TemplateItem = identity == Identity.ACROSS ? "grid-template-columns" : "grid-template-rows";

                (int delta, double percent) = PartitionWidth[parent];
                
                delta = (delta - (identity == Identity.ACROSS? dX : dY));
             
                PartitionWidth[parent] = (delta, percent);

                parent.StyleContext.WithStyle(StyleOperator, parent.Context, 
                    (TemplateItem, $"min(calc({(1-percent)*100}% - {delta}px), calc(100%))"));
            }

            async void EndInputCapture()
            {
                string space = "                   ";
                PartitionSliderInputData.MouseIsNotDown();
                //PartitionSliderInputData.MouseMoveAction = null;
                
                Partition localParent = PartitionParentMap[partitionA];
                Identity identity = PartitionIdentities[localParent];
                
                var rectTaskParent = JsRuntime.InvokeAsync<Rect>("GetDimensions", localParent.Context.Id);
                var rectTaskB = JsRuntime.InvokeAsync<Rect>("GetDimensions", partitionB.Context.Id);

                bool across = identity == Identity.ACROSS;
                
                var rectParent = await rectTaskParent;
                var rectB = await rectTaskB;

                (int delta, double percent) = PartitionWidth[localParent];
                
                double offset = (((across ? rectParent.width : rectParent.height) * percent) + delta);
                double into = offset / (across ? rectParent.width : rectParent.height);
                
                PartitionWidth[localParent] = (0, into);
                Console.WriteLine(into);

                string remaining = (1 - into)*100+space;
                
                if (into >= 1) remaining = "0"+space;
                if (into < 0.009) remaining = "100"+space;
                
                remaining = remaining[0..8].Trim()+"%";
                
                Console.WriteLine(remaining);
                
                await localParent.StyleContext.WithStyle(StyleOperator, localParent.Context, 
                    (across ? "grid-template-columns" : "grid-template-rows", 
                        $"minmax({remaining}, {remaining})")
                );
            }
            
            async void StartInputCapture(MouseEventArgs mArgs)
            {
                PartitionSliderInputData.CurrentPosition = new Position((int)mArgs.ScreenX, (int)mArgs.ScreenY);
                    
                Partition localParent = PartitionParentMap[partitionA];
                (int delta, double percent) = PartitionWidth[localParent];
                    
                if (percent <= 0)
                {
                    PartitionWidth[localParent] = (0, 0);
                }
                
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
            

            //set parent event for mouseleave
            
            if (!PartitionShiftMap.TryAdd(parent, () => { EndInputCapture(); }))
            {
                PartitionShiftMap[parent] = () => { EndInputCapture(); };
            }
            
            parent.Context.AddEvent("onmouseleave", (args) =>
            {
                if (PartitionSliderInputData.InputTarget == parent)
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
                    PartitionSliderInputData.EndInputAction?.Invoke();
                    PartitionSliderInputData.MouseMoveAction = null;
                    PartitionSliderInputData.InputTarget = null;
                });
            
                p.Context.AddEvent("onmousemove", (args) =>
                {
                    MouseEventArgs mArgs = (MouseEventArgs) args;

                    //if ((mArgs.Buttons & 1) == 1 && !PartitionSliderInputData.MouseDown) StartInputCapture(mArgs);
                    if (PartitionSliderInputData.InputTarget == PartitionParentMap[p])
                    {
                        if ((mArgs.Buttons & 1) == 0)
                        {
                            //EndInputCapture();
                            PartitionSliderInputData.MouseMoveAction = null;
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

            int borderWidth = 4;
            
            PartitionWidth.TryAdd(source, (0, 0.5));
            
            string[] borderSide = identity == Identity.ACROSS ? new[] {"border-right","border-left"} : new[] {"border-bottom", "border-top"};

            partitionA.StyleContext.WithStyle(StyleOperator, partitionA.Context, 
                (borderSide[0], $"{borderWidth}px solid black"));
            
            partitionB.StyleContext.WithStyle(StyleOperator, partitionB.Context, 
                (borderSide[1], $"{borderWidth}px solid black"));
            
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
            Console.WriteLine(sCtx.Context.Id);
            SplitPartition(sCtx, identity,
                (source, partitionA, partitionB) =>
                {
                    AddElementToPartition(controlContexts[0], partitionA);
                    controlContexts[0].SetParent(partitionA.ElementNode);
                    
                    AddElementToPartition(controlContexts[1], partitionB);
                    controlContexts[1].SetParent(partitionB.ElementNode);

                    partitionA.Context.OnAfterRender += () =>
                    {
                        Console.WriteLine("HERE");
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