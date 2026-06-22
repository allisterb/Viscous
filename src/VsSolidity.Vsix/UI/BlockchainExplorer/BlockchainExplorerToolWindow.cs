using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Wpf.Ui.Controls;

namespace VsSolidity.UI
{
    [Guid("d284f7d7-ba72-4287-991d-18821ddf9b91")]
    public class BlockchainExplorerToolWindow : ToolWindowPane
    {
        public BlockchainExplorerToolWindow() : base(VsSolidityPackage.Instance)
        {
            this.Caption = "Blockchain Explorer";
            this.BitmapImageMoniker = KnownMonikers.NeuralNetwork;
            var control = new BlockchainExplorerToolWindowControl();
            
            this.Content = control;
            control.window = this;
            
        }
    }
}
