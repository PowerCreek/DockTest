using System;
using DockTest.ExternalDeps.Classes.Operations;
using DockTest.Source.Properties;
using DockTest.Source.Properties.Vector;

namespace DockTest.Source.Operations
{
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
            ContainerTransform.Size = new Size(1000, 800);
            
            
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