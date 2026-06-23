using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace VsSolidity.UI
{
    [Guid("E32C33B5-D9B6-40CC-8546-9CB96A66E888")]
    public class DeploySolidityProjectToolWindow : ToolWindowPane, IVsWindowFrameNotify3
    {
        public DeploySolidityProjectToolWindow() : base(VsSolidityPackage.Instance)
        {
            this.Caption = "Deploy Solidity Project";            
            this.control = new DeploySolidityProjectToolWindowControl();
            this.Content = control;
        }

        public DeploySolidityProjectToolWindowControl control;

        public int OnShow(int fShow)
        {
            if (fShow == (int) __FRAMESHOW3.FRAMESHOW_WinActivated)
            {                
                if (!control.IsInitializedWithProject())
                {
                    control.HideForm();
                }
                else
                {
                    control.ShowForm();
                }                
            }
            return VSConstants.S_OK;    
        }
                    
        public int OnMove(int x, int y, int w, int h) => VSConstants.S_OK;

        public int OnSize(int x, int y, int w, int h) => VSConstants.S_OK;

        public int OnDockableChange(int fDockable, int x, int y, int w, int h) => VSConstants.S_OK;

        public int OnClose(ref uint pgrfSaveOptions) => VSConstants.S_OK;
    }

    
}
