using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Management.Operations;
using DockTest.ExternalDeps.Classes.Operations;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{

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

        public Dictionary<Partition, Partition> PartitionParentMap = new Dictionary<Partition, Partition>();

        public Dictionary<Partition, Identity> PartitionIdentities = new Dictionary<Partition, Identity>();

        public Dictionary<object, Partition> ElementPartitionPair = new Dictionary<object, Partition>();
        
        public void CreateSubPartition(Partition source, out Partition partition)
        {
            partition = new Partition($"{source.Context.Id}", source.JsRuntime);
            List<string> values = null;

            PartitionIdentities.TryAdd(source, Identity.ACROSS);
            
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
        
        public async Task SplitPartition(Partition source, Identity identity, Action<Partition, Partition,Partition> callback)
        {
            CreateSubPartition(source, out Partition partitionA);
            CreateSubPartition(source, out Partition partitionB);
         
            await SetPartionIdentity(source, identity);   
            callback(source, partitionA, partitionB);
        }
        
        public ControlContext RegisterPartitionContent(ControlContext content)
        {
            ControlContext contentControl = new ControlContext("partitionContent", JsRuntime);

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