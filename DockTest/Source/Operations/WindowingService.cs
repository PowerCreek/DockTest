using System;
using System.Text.Json;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Properties;
using DockTest.Source.Properties.Vector;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{

    public record Rect
    {
        public int x { get; set; }
        public int y { get; set; }
        public int top { get; set; }
        public int left { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
    
    public class WindowingService
    {
        public ControlOperation ControlOperation { get; set; }
        public ControlContext ContainerControl { get; set; }

        private Transform ContainerTransform { get; set; }

        public WindowingService()
        {
            
        }
        
        public void RegisterContainer(ControlContext container)
        {
            
            ContainerControl = container;
            ContainerControl.Add("transform", ContainerTransform = new Transform());
            ContainerControl.WithAttribute("style", out StyleContext containerStyle);
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
            ContainerControl.WithContent(out ScrollContent slider);

            ControlContext mainPartition;
            ContainerControl.Add(nameof(mainPartition), mainPartition = ControlOperation.RegisterControl("mainPartition"));
            mainPartition.cssClass = "partition";
            ContainerControl.AddChild(mainPartition);
            //ContainerControl.WithContent(out DockGroup dockgroup);
        }
    }
}