using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DockTest.ExternalDeps.Classes;
using DockTest.ExternalDeps.Classes.Management;
using DockTest.ExternalDeps.Classes.Management.Operations;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Properties;
using DockTest.Source.Properties.Vector;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{

    public record Rect
    {
        public double x { get; set; }
        public double y { get; set; }
        public double top { get; set; }
        public double left { get; set; }
        public double right { get; set; }
        public double bottom { get; set; }
        public double width { get; set; }
        public double height { get; set; }
    }
    
    public class WindowingService
    {
        public ControlOperation ControlOperation { get; set; }
        public ControlContext ContainerControl { get; set; }
        private Transform ContainerTransform { get; set; }

        public PartitionService PartitionService { get; set; }
        
        public WindowingService()
        {
            
        }
        
        public async Task RegisterContainer(ControlContext container)
        {
            ContainerControl = container;
            ContainerControl.Add("transform", ContainerTransform = new Transform());
            ContainerControl.WithStyles(out StyleContext containerStyle);
            
            await containerStyle.WithStyle(ControlOperation.StyleOperator, ContainerControl,
                ("display", "flex"),
                ("flex-direction", "column"));
            
            ContainerControl.WithBounds();

            ContainerTransform.OnMove = (transform, position) =>
            {
                containerStyle.WithStyle(ControlOperation.StyleOperator, ContainerControl,
                    ("left", $"{position.X}px"), 
                    ("top", $"{position.Y}px"));
            };
            
            ContainerTransform.OnResize = (transform, size) =>
            {
                containerStyle.WithStyle(ControlOperation.StyleOperator, ContainerControl,
                    ("width", $"{size.Width}px"), 
                    ("height", $"{size.Height}px")
                );
            };
            
            ContainerTransform.Position = new Position(0,0);

            ContainerControl.cssClass = "container1 windowing";
            ContainerControl.WithContent(out MenuContent menu);

            ControlContext spaceControl = new ControlContext($"{ContainerControl.Id}", ContainerControl.JsRuntime);
            spaceControl.WithStyles(out StyleContext spaceStyle);
            
            await spaceStyle.WithStyle(ControlOperation.StyleOperator, spaceControl, 
                ("width","100%"),
                ("height","100%"),
                ("background-color","yellow")
            );

            ContainerControl.AddChild(spaceControl);

            spaceControl.WithContent(out Partition spacePartition);
            
            PartitionService.CreateSubPartition(spacePartition, out Partition partitionTop);
            PartitionService.CreateSubPartition(spacePartition, out Partition partitionCenter);
            PartitionService.CreateSubPartition(spacePartition, out Partition partitionBottom);
            
            await PartitionService.SetPartionIdentity(spacePartition, 2);

            await partitionTop.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionTop.Context, 
                ("background-color","darkgray"),
                ("height","50px"),
                ("border-bottom","2px solid black")
            );
            
            await partitionCenter.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionCenter.Context, 
                ("background-color","white"),
                ("height","100%")
            );
            
            await partitionBottom.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionBottom.Context, 
                ("background-color","darkgray"),
                ("height","50px"),
                ("border-top","2px solid black")
            );
            
            
            PartitionService.CreateSubPartition(partitionCenter, out Partition partitionLeft);
            PartitionService.CreateSubPartition(partitionCenter, out Partition partitionMiddle);
            PartitionService.CreateSubPartition(partitionCenter, out Partition partitionRight);
            
            await partitionLeft.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionLeft.Context, 
                ("background-color","darkgray"),
                ("width","50px"),
                ("border-right","2px solid black")
            );
            
            await partitionMiddle.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionMiddle.Context, 
                ("background-color","white"),
                ("grid-auto-columns","1fr"),
                ("grid-auto-rows","1fr"),
                ("width","100%")
            );
            
            await partitionRight.StyleContext.WithStyle(ControlOperation.StyleOperator, partitionRight.Context, 
                ("background-color","darkgray"),
                ("width","50px"),
                ("border-left","2px solid black")
            );

            Task.Run(async () =>
            {
                ControlContext testElementA = new ControlContext("testA", ControlOperation.JsRuntime);
                testElementA.SetHtml("HELLO WORLD A");
                
                ControlContext ContentControlA = PartitionService.RegisterPartitionContent(testElementA);
                
                PartitionService.SetPartitionContent(partitionMiddle, ContentControlA);
                PartitionService.SetPartionIdentity(partitionMiddle, 1);
                
                ContentControlA.WithStyles(out StyleContext contentStyleA);
                await contentStyleA.WithStyle(ControlOperation.StyleOperator, ContentControlA, 
                    ("background-color", "darkgray"));

                ControlContext testElementB = new ControlContext("testB", ControlOperation.JsRuntime);
                testElementB.SetHtml("HELLO WORLD B");

                ControlContext ContentControlB = PartitionService.RegisterPartitionContent(testElementB);
                
                ContentControlB.WithStyles(out StyleContext contentStyleB);
                await contentStyleB.WithStyle(ControlOperation.StyleOperator, ContentControlB, 
                    ("background-color", "darkgray"));
                
                ControlContext testElementC = new ControlContext("testC", ControlOperation.JsRuntime);
                testElementC.SetHtml("HELLO WORLD C");
                
                ControlContext ContentControlC = PartitionService.RegisterPartitionContent(testElementC);
                
                ContentControlC.WithStyles(out StyleContext contentStyleC);
                await contentStyleC.WithStyle(ControlOperation.StyleOperator, ContentControlC, 
                    ("background-color", "darkgray"));
                
                ControlContext testElementD = new ControlContext("testD", ControlOperation.JsRuntime);
                testElementD.SetHtml("HELLO WORLD D");
                
                ControlContext ContentControlD = PartitionService.RegisterPartitionContent(testElementD);
                
                ContentControlD.WithStyles(out StyleContext contentStyleD);
                await contentStyleD.WithStyle(ControlOperation.StyleOperator, ContentControlD, 
                    ("background-color", "darkgray"));
                
                ControlContext testElementE = new ControlContext("testE", ControlOperation.JsRuntime);
                testElementE.SetHtml("HELLO WORLD E");
                
                ControlContext ContentControlE = PartitionService.RegisterPartitionContent(testElementE);
                
                ContentControlE.WithStyles(out StyleContext contentStyleE);
                await contentStyleE.WithStyle(ControlOperation.StyleOperator, ContentControlE, 
                    ("background-color", "darkgray"));
                
                ControlContext testElementF = new ControlContext("testF", ControlOperation.JsRuntime);
                testElementF.SetHtml("HELLO WORLD F");
                
                ControlContext ContentControlF = PartitionService.RegisterPartitionContent(testElementF);
                
                ContentControlF.WithStyles(out StyleContext contentStyleF);
                await contentStyleF.WithStyle(ControlOperation.StyleOperator, ContentControlF, 
                    ("background-color", "darkgray"));

                int count = 0;
                int interval = 100;
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlA, ContentControlB, 2);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlA, ContentControlD, 2);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlB, ContentControlC, 1);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlB, ContentControlA, 1);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlC, ContentControlB, 4);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlB, ContentControlA, 1);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlD, ContentControlE, 1);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlB, ContentControlC, 4);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                PartitionService.AppendOverElement(ContentControlC, ContentControlE, 1);
                partitionMiddle.Context.SurrogateReference?.ChangeState();
                await Task.Delay(interval);
                Console.WriteLine("count: "+count++);
                // PartitionService.AppendOverElement(ContentControlC, ContentControlF, 4);
                // await Task.Delay(500);
                // Console.WriteLine("count: "+count++);
                // PartitionService.AppendOverElement(ContentControlA, ContentControlF, 1);
                // await Task.Delay(500);
                // Console.WriteLine("count: "+count++);
                
                // await Task.Delay(100);
                // PartitionService.AppendOverElement(ContentControlA, ContentControlB, 1);
                // await Task.Delay(100);
                // PartitionService.AppendOverElement(ContentControlA, ContentControlC, 1);
                // await Task.Delay(100);
                // PartitionService.AppendOverElement(ContentControlA, ContentControlD, 1);
                // await Task.Delay(100);

                partitionMiddle.Context.SurrogateReference?.ChangeState();

            });
        }

   
    }
}