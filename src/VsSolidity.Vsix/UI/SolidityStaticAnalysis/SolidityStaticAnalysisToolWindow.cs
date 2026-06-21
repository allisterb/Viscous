using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VsSolidity.UI
{
    [Guid("F3414058-27BB-4D0A-8E78-EE513B1E3863")]
    public class SolidityStaticAnalysisToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolidityStaticAnalysisToolWindow"/> class.
        /// </summary>
        public SolidityStaticAnalysisToolWindow() : base(null)
        {
            this.Caption = "Solidity Static Analysis";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var c  = new SolidityStaticAnalysisToolWindowControl();
            c.window = this;
            this.Content = c;   
            this.control = c;
        }

        public SolidityStaticAnalysisToolWindowControl control;
    }
}
