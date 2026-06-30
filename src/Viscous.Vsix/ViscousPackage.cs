using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Task = System.Threading.Tasks.Task;

using Microsoft.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Wpf.Ui.Appearance;
using static Microsoft.VisualStudio.VSConstants.UICONTEXT;

namespace Viscous
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "0.1.0.9", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideUIContextRule(SolidityFileUIContextRule,
        name: "Solidity Project Files",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:.sol$" })]
    [ProvideUIContextRule(SolidityProjectFileUIContextRule,
        name: "Solidity Project Configuration File",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:.solproj$" })]
    [ProvideUIContextRule(NPMFileUIContextRule,
        name: "NPM Configuration Files",
        expression: "(SingleProject | MultipleProjects) & Solidity",
        termNames: new[] { "SingleProject", "MultipleProjects", "Solidity" },
        termValues: new[] { SolutionHasSingleProject_string, SolutionHasMultipleProjects_string, "HierSingleSelectionName:package.json$" })]
    [ProvideToolWindow(typeof(UI.BlockchainExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    [ProvideToolWindow(typeof(UI.DeploySolidityProjectToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    [ProvideToolWindow(typeof(UI.SolidityStaticAnalysisToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindSolutionExplorer)]
    public sealed partial class ViscousPackage : AsyncPackage, IVsSolutionEvents7, IVsSolutionEvents
    {
        #region Constructors
        static ViscousPackage()
        {
            Runtime.WithFileLogging("Viscous", "VS", false, Runtime.ViscousDir);
        }
        #endregion

        #region Methods

        #region IVsSolutionEvents7 members

        public void OnBeforeCloseFolder(string folderPath) {}

        public void OnAfterCloseFolder(string folderPath) {}

        public void OnQueryCloseFolder(string folderPath, ref int s) {}
        
        public void OnAfterOpenFolder(string folderPath)
        {
            Runtime.Info("Opened solution folder {f}.", folderPath);
        }

        public void OnAfterLoadAllDeferredProjects() {}
        #endregion

        #region IVsSolutionEvents members

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (pHierarchy.IsCapabilityMatch("CPS") && pHierarchy.IsCapabilityMatch(SolidityUnconfiguredProject.UniqueCapability))
            {
                var unconfiguredProject = VSUtil.GetUnconfiguredProject(pHierarchy);
                InstallSolidityProjectDataFlowSinks(unconfiguredProject);   
            }
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);            
            Instance = this;
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!VSUtil.VSServicesInitialized)
            {
                if (VSUtil.InitializeVSServices(ServiceProvider.GlobalProvider))
                {
                    VSUtil.LogInfo("Viscous", $"Extension assembly directory is {Runtime.AssemblyLocation}. Viscous package services initialized.");
                }
                else
                {
                    Runtime.Error("Could not initialize Viscous package services.");
                    return;
                }
            }
            IVsSolution solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            solution.AdviseSolutionEvents(this, out var c);
          
            await TaskScheduler.Default;
            AppSettings.EnsureFileExists();            
            await InstallBuildSystemAsync();
            await EnsurePythonVenvAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();                        
            ApplicationThemeManager.Apply(UI.VSTheme.ApplicationThemeGuess);
            await SolidityProjectMenuCommands.InitializeAsync(this);
            await UI.BlockchainExplorerToolWindowCommand.InitializeAsync(this);
            await UI.DeploySolidityProjectToolWindowCommand.InitializeAsync(this);
            await UI.SolidityStaticAnalysisToolWindowCommand.InitializeAsync(this);
            
        }
        #endregion

        #region Static Methods
        internal static async Task EnsureNpmRcAsync()
        {
            var path = Path.Combine(Runtime.AssemblyLocation, ".npmrc");
            try
            {
                if (!File.Exists(path))
                {
                    await File.WriteAllTextAsync(path, "ignore-scripts=true\n");
                }
            }
            catch (Exception ex)
            {
                Runtime.Error(ex, "Could not write the extension's .npmrc (ignore-scripts).");
            }
        }

        private static async Task InstallBuildSystemAsync()
        {
            if (!Directory.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity")))
            {
                Directory.CreateDirectory(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity"));
            }
            await File.WriteAllTextAsync(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "extdir.txt"), Runtime.AssemblyLocation);
            
            // Always refresh the build system directory and build task assembly so extension upgrades pick up the latest files.
            await Runtime.CopyDirectoryAsync(Runtime.AssemblyLocation.CombinePath("BuildSystem"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity"), true);

            if (!Directory.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools")))
            {
                Directory.CreateDirectory(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools"));
            }
            await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("Viscous.BuildTasks.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "Viscous.BuildTasks.dll"));
            if (!File.Exists(Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll")))
            {
                await Runtime.CopyFileAsync(Runtime.AssemblyLocation.CombinePath("CompactJson.dll"), Runtime.LocalAppDataDir.CombinePath("CustomProjectSystems", "Solidity", "Tools", "CompactJson.dll"));
            }
            // The Python analysis tools (solc-select, slither) are provisioned on demand, right before a build
            // or analysis needs them (see SolidityCompiler.EnsureSolcSelectAsync / EnsureSlitherAsync and the
            // build task), so a build triggered before package init finished still waits for the install.
        }

        /// <summary>
        /// Creates the private Python virtual environment under <c>%LOCALAPPDATA%\Viscous\venv</c> if it
        /// does not yet exist. Idempotent; safe to await on every build/analyze.
        /// </summary>
        public static async Task<bool> EnsurePythonVenvAsync()
        {
            if (File.Exists(Runtime.VenvPython))
            {
                return true;
            }            
            await Runtime.RunCmdAsync("cmd.exe", "/c " + AppSettings.PythonCmd + " -m venv \"" + Runtime.VenvDir + "\"", Runtime.ViscousDir);
            if (!File.Exists(Runtime.VenvPython))
            {
                Runtime.Error($"Could not create the Python virtual environment at {Runtime.VenvDir}. Ensure Python 3.8+ is installed and available as '{AppSettings.PythonCmd}' (configurable via the PythonCmd setting in {AppSettings.FilePath}).");
                return false;
            }
            return true;
        }

        private void InstallSolidityProjectDataFlowSinks(UnconfiguredProject unconfiguredProject)
        {
            var subscriptionService = unconfiguredProject.Services.ActiveConfiguredProjectSubscription;
            
            var receivingBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(u =>
            {
                
                //await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                //VSUtil.LogInfo("Viscous", "update");
            });
            subscriptionService.JointRuleSource.SourceBlock.LinkTo(receivingBlock, new JointRuleDataflowLinkOptions() { PropagateCompletion = true}); 
        }
        #endregion

        #endregion

        #region Fields
        public static ViscousPackage Instance { get; private set; }
        #endregion

        #region Constants
        public const string PackageGuidString = "724F436A-F472-4DDE-81D8-2544C883F574";

        public const string SolidityFileUIContextRule = "EFAF43FC-845F-40DF-AE69-34EAD5ED8F4C";

        public const string SolidityProjectFileUIContextRule = "8D4CA0F3-610A-4BC5-90D2-CA8C486B6EE8";

        public const string NPMFileUIContextRule = "333C0751-5D5E-47A8-804D-6763A1363906";

        public const string SolcSelectVersion = "1.2.0";
        public const string SlitherVersion = "0.10.3";

        // The package directories under the venv act as "already installed" markers.
        public static string SolcSelectPackageDir => Path.Combine(Runtime.VenvDir, "Lib", "site-packages", "solc_select");

        public static string SlitherPackageDir => Path.Combine(Runtime.VenvDir, "Lib", "site-packages", "slither");
        #endregion

    }
}
