using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

namespace VsSolidity.UI
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("EE7F46DB-959E-4447-A921-D54E8F7B8D4E")]
    public class RunSmartContractToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunSmartContractToolWindow"/> class.
        /// </summary>
        public RunSmartContractToolWindow() : base(null)
        {
            this.Caption = "Run Smart Contract";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            var c = new RunSmartContractToolWindowControl();
            c.window = this;
            this.Content = c;
        }
    }
}
