using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DockTest.ExternalDeps.Classes.Management.Operations;
using DockTest.ExternalDeps.Classes.Operations;
using Microsoft.JSInterop;

namespace DockTest.Source.Operations
{
    public class ControlOperation : OperationBase
    {
        private static int _id;
        public static int NextId => _id++;
        
        public StyleOperator StyleOperator { get; set; }

        public Dictionary<ControlContext, ControlContext> ControlMap { get; } = new();

        public ControlOperation(IJSRuntime jsRuntime) : base(jsRuntime) { }
        
        public ControlContext RegisterControl(string tags)
        {
            ControlContext control = new ControlContext($"{tags}_{NextId}", JsRuntime);
            control.Add("styleOperator", StyleOperator);
            return control;
        }
    }
}