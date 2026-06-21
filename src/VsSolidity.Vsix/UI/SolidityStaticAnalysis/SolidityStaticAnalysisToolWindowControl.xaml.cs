using System.Windows.Controls;

using Microsoft.VisualStudio.Shell;

using Wpf.Ui.Controls;
using Hardcodet.Wpf.GenericTreeView;

using VsSolidity.UI.ViewModel;
using VsSolidity;

namespace VsSolidity.UI
{
    public partial class SolidityStaticAnalysisToolWindowControl : UserControl
    {
        #region Constructor
        public SolidityStaticAnalysisToolWindowControl()
        {
            var _ = new Wpf.Ui.Markdown.Controls.MarkdownViewer();
            this.InitializeComponent();

            // Items, TreeNodeStyle, TreeStyle, NodeSortDescriptions, SelectNodesOnRightClick and IsLazyLoading are
            // declared on the generic base TreeViewBase<SolidityStaticAnalysisInfo>. Setting them in XAML makes WPF BAML
            // resolve the generic base type and throw NotImplementedException, so assign them here. Set styling/sort
            // first, then bind Items last so the tree's first render already has them applied.
            SolidityStaticAnalysisTree.IsLazyLoading = false;
            SolidityStaticAnalysisTree.SelectNodesOnRightClick = true;
            SolidityStaticAnalysisTree.TreeStyle = (System.Windows.Style)FindResource("TreeViewStyle");
            SolidityStaticAnalysisTree.TreeNodeStyle = (System.Windows.Style)FindResource("TreeViewItemStyle");
            SolidityStaticAnalysisTree.NodeSortDescriptions = (System.Collections.Generic.IEnumerable<System.ComponentModel.SortDescription>)FindResource("AscendingNames");
            SolidityStaticAnalysisTree.SetBinding(TreeViewBase<SolidityStaticAnalysisInfo>.ItemsProperty,
                new System.Windows.Data.Binding(nameof(SolidityStaticAnalysisViewModel.Objects)) { Source = (SolidityStaticAnalysisViewModel)Resources["StaticAnalysis"] });
#if IS_VSIX
            VSTheme.WatchThemeChanges();
            instance = this;
#endif
        }
        #endregion

        #region Methods
        public void AnalyzeProjectFileItem(string filePath, string projectDir, SoliditySlitherAnalysis analysis)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var root = SolidityStaticAnalysisTree.RootItem;
            root.Data["Label"] = Runtime.GetWindowsRelativePath(filePath, projectDir); ;
            var vm = (SolidityStaticAnalysisViewModel)TryFindResource("StaticAnalysis");
            vm.ClearAnalysis();
            if (analysis.results.detectors != null && analysis.results.detectors.Length > 0)
            {
                foreach (var d in analysis.results.detectors)
                {
                    vm.AddDetectorResult(d);
                }
            }
            SolidityStaticAnalysisTree.Refresh();
        }

        private SolidityStaticAnalysisInfo GetSelectedItem(object sender)
        {
            var window = (SolidityStaticAnalysisToolWindowControl)sender;
            var tree = window.SolidityStaticAnalysisTree;
            return tree.SelectedItem;
        }

        #region Event handlers
        private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<SolidityStaticAnalysisInfo> e)
        {
            if (sender is SolidityStaticAnalysisTree tree && tree.SelectedItem != null)
                if (tree.SelectedItem.Kind == SolidityStaticAnalysisInfoKind.Detector)
                {
                    StaticAnalysisMarkdownViewer.Markdown = (string)e.NewItem.Data["Markdown"];
                }
        }
        #endregion
        
        #endregion

        #region Fields
        internal ToolWindowPane window;
        internal SolidityStaticAnalysisToolWindowControl instance;
        #endregion

        
    }
}
