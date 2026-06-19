using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

namespace VsSolidity.UI
{
    [Guid("E32C33B5-D9B6-40CC-8546-9CB96A66E888")]
    public class DeploySolidityProjectToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeploySolidityProjectToolWindow"/> class.
        /// </summary>
        public DeploySolidityProjectToolWindow() : base(VsSolidityPackage.Instance)
        {
            this.Caption = "Deploy Solidity Project";
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.control = new DeploySolidityProjectToolWindowControl();
            this.Content = control;
        }

        public DeploySolidityProjectToolWindowControl control;        
    }

    
}
