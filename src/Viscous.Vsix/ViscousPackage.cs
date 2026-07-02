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
            AppSettings.EnsureUpToDate();
            await InstallBuildSystemAsync();
            await EnsurePythonVenvAsync();
            await ReconcileToolVersionsAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            ApplicationThemeManager.Apply(UI.VSTheme.ApplicationThemeGuess);
            await SolidityProjectMenuCommands.InitializeAsync(this);
            await UI.BlockchainExplorerToolWindowCommand.InitializeAsync(this);
            await UI.DeploySolidityProjectToolWindowCommand.InitializeAsync(this);
            await UI.SolidityStaticAnalysisToolWindowCommand.InitializeAsync(this);
            
        }
        #endregion

        #region Static Methods
        internal static async Task EnsureNpmEnvironmentAsync()
        {
            // Set up ViscousDir as npm's working directory for our tool install: an .npmrc (ignore-scripts hardening)
            // and a package.json so `npm install` has a project to write into. Both live here (not the extension
            // assembly dir) so they sit with node_modules and survive extension updates. Each is write-if-absent:
            // in particular we must not clobber package.json, since npm records installed dependencies into it.
            Runtime.CreateIfDirectoryDoesNotExist(Runtime.ViscousDir);
            try
            {
                var npmrc = Path.Combine(Runtime.ViscousDir, ".npmrc");
                if (!File.Exists(npmrc))
                {
                    await File.WriteAllTextAsync(npmrc, "ignore-scripts=true\n");
                }

                var packageJson = Path.Combine(Runtime.ViscousDir, "package.json");
                if (!File.Exists(packageJson))
                {
                    await File.WriteAllTextAsync(packageJson, DefaultNpmPackageJson);
                }
            }
            catch (Exception ex)
            {
                Runtime.Error(ex, "Could not write the Viscous npm environment (.npmrc / package.json).");
            }
        }

        // Minimal private manifest so npm has a project to install into. Marked "private" so it can never be
        // published, and given a real description/name (no contentless stub shipped in the extension).
        private const string DefaultNpmPackageJson = @"{
  ""name"": ""viscous-tools"",
  ""version"": ""1.0.0"",
  ""description"": ""Local Node.js environment for the Viscous Visual Studio extension. Hosts the vscode-solidity language server and related npm dependencies."",
  ""private"": true,
  ""license"": ""MIT"",
  ""author"": ""Allister Beharry""
}
";

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

        /// <summary>
        /// Reconciles the installed external tools with the versions this extension build ships. Runs once per session
        /// at startup on a background thread — deliberately OFF the build/analyze hot paths, which keep their cheap
        /// "is it installed?" guards. Already-installed Python tools are upgraded in place when their pinned version
        /// changed; the (unpinned) language server is refreshed when the extension install changed. Fresh installs are
        /// still handled on demand by the Ensure* methods at the current pins. A no-op once nothing has changed.
        /// </summary>
        private static async Task ReconcileToolVersionsAsync()
        {
            var manifestPath = Path.Combine(Runtime.ViscousDir, "tools-manifest.json");
            ToolsManifest manifest = null;
            try
            {
                if (File.Exists(manifestPath))
                {
                    manifest = Newtonsoft.Json.JsonConvert.DeserializeObject<ToolsManifest>(File.ReadAllText(manifestPath));
                }
            }
            catch (Exception ex)
            {
                Runtime.Error(ex, "Could not read the Viscous tools manifest; treating tools as unprovisioned.");
            }
            manifest = manifest ?? new ToolsManifest();

            try
            {
                // Python tools: upgrade in place only if already installed and the pin moved. --upgrade to the exact
                // pin is a no-op when already satisfied.
                if (Directory.Exists(SolcSelectPackageDir) && manifest.SolcSelectVersion != SolcSelectVersion)
                {
                    Runtime.Info("Updating solc-select ({0} -> {1}) in the virtual environment...", manifest.SolcSelectVersion ?? "none", SolcSelectVersion);
                    await Runtime.RunCmdAsync(Runtime.VenvPython, $"-m pip install --only-binary :all: --upgrade solc-select=={SolcSelectVersion}", Runtime.ViscousDir);
                }
                if (Directory.Exists(SlitherPackageDir) && manifest.SlitherVersion != SlitherVersion)
                {
                    Runtime.Info("Updating slither-analyzer ({0} -> {1}) in the virtual environment...", manifest.SlitherVersion ?? "none", SlitherVersion);
                    await Runtime.RunCmdAsync(Runtime.VenvPython, $"-m pip install --only-binary :all: --upgrade slither-analyzer=={SlitherVersion}", Runtime.ViscousDir);
                }

                // The language server has no explicit version pin (installed as latest). Refresh it whenever the
                // extension install changes — AssemblyLocation is version-specific, so it differs after an update —
                // but only if it's already installed (a fresh install is handled on first activation).
                if (File.Exists(SolidityLanguageClient.LanguageServerPath) && manifest.ExtensionLocation != Runtime.AssemblyLocation)
                {
                    Runtime.Info("Extension install changed; refreshing the Solidity language server to the latest version...");
                    await EnsureNpmEnvironmentAsync();
                    await Runtime.RunCmdAsync("cmd.exe", "/c " + AppSettings.JSPackageManagerCmd + " install vscode-solidity-server@latest", Runtime.ViscousDir);
                }
            }
            catch (Exception ex)
            {
                Runtime.Error(ex, "Error reconciling external tool versions.");
            }

            // Record what is now provisioned (only when something actually changed, to avoid needless disk writes).
            if (manifest.ExtensionLocation != Runtime.AssemblyLocation || manifest.SolcSelectVersion != SolcSelectVersion || manifest.SlitherVersion != SlitherVersion)
            {
                manifest.ExtensionLocation = Runtime.AssemblyLocation;
                manifest.SolcSelectVersion = SolcSelectVersion;
                manifest.SlitherVersion = SlitherVersion;
                try
                {
                    File.WriteAllText(manifestPath, Newtonsoft.Json.JsonConvert.SerializeObject(manifest, Newtonsoft.Json.Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Runtime.Error(ex, "Could not write the Viscous tools manifest at {0}.", manifestPath);
                }
            }
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

        #region Types
        // Records the external-tool versions last provisioned into ViscousDir, so a session can detect what changed
        // since the last extension build and update only what's needed. Persisted as tools-manifest.json.
        private class ToolsManifest
        {
            public string ExtensionLocation { get; set; }
            public string SolcSelectVersion { get; set; }
            public string SlitherVersion { get; set; }
        }
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
