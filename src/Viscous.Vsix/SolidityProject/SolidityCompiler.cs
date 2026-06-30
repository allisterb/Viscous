using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

using Viscous.Ethereum;
//using Viscous.Viscous.SolidityCompilerIO2;
namespace Viscous
{
    public class SolidityCompiler : Runtime
    {
        public static string TaskToolsDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CustomProjectSystems", "Solidity", "Tools");

        // solc-select installs compilers under the user profile (Python's expanduser("~")), not the Tools dir.
        public static string SolcArtifactPath(string compilerVersion) =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".solc-select", "artifacts", "solc-" + compilerVersion, "solc-" + compilerVersion);

        #region Python tool provisioning
        
        
        /// <summary>
        /// Ensures solc-select is installed in the venv (creating the venv first if needed).
        /// Call this right before a solc compiler is needed.
        /// </summary>
        public static async Task<bool> EnsureSolcSelectAsync()
        {
            if (Directory.Exists(ViscousPackage.SolcSelectPackageDir))
            {
                return true;
            }
            if (!await ViscousPackage.EnsurePythonVenvAsync())
            {
                VSUtil.LogError("Viscous", $"Could not create the Python virtual environment at {Runtime.VenvDir}. Ensure Python 3.8+ is installed and available as '{AppSettings.PythonCmd}' (configurable via the PythonCmd setting in {AppSettings.FilePath}).");
                return false;
            }
            VSUtil.LogInfo("Viscous", $"Installing solc-select {ViscousPackage.SolcSelectVersion} into the virtual environment at {Runtime.VenvDir}...");
            // Wheels-only so pip never runs an arbitrary setup.py.
            await RunCmdAsync(VenvPython, $"-m pip install --only-binary :all: solc-select=={ViscousPackage.SolcSelectVersion}", ViscousDir);
            if (!Directory.Exists(ViscousPackage.SolcSelectPackageDir))
            {
                VSUtil.LogError("Viscous", $"Could not install solc-select into {VenvDir}.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Ensures slither-analyzer is installed in the venv (creating the venv first if needed).
        /// Call this right before running a Slither analysis.
        /// </summary>        
        public static async Task<bool> EnsureSlitherAsync()
        {
            if (Directory.Exists(ViscousPackage.SlitherPackageDir))
            {
                return true;
            }
            if (!await ViscousPackage.EnsurePythonVenvAsync())
            {
                VSUtil.LogError("Viscous", $"Could not create the Python virtual environment at {Runtime.VenvDir}. Ensure Python 3.8+ is installed and available as '{AppSettings.PythonCmd}' (configurable via the PythonCmd setting in {AppSettings.FilePath}).");
                return false;
            }
            VSUtil.LogInfo("Viscous", $"Installing slither-analyzer {ViscousPackage.SlitherVersion} into the virtual environment at {Runtime.VenvDir}...");
            // Wheels-only so pip never runs an arbitrary setup.py.
            await RunCmdAsync(VenvPython, $"-m pip install --only-binary :all: slither-analyzer=={ViscousPackage.SlitherVersion}", ViscousDir);
            if (!Directory.Exists(ViscousPackage.SlitherPackageDir))
            {
                VSUtil.LogError("Viscous", $"Could not install slither-analyzer into {VenvDir}.");
                return false;
            }
            return true;
        }
        #endregion

        public static async Task CompileFileAsync(string file, string workspaceDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Viscous");
            VSUtil.LogInfo("Viscous", string.Format("Compiling {0} in {1} using solc.js compiler...", file, workspaceDir));
            await TaskScheduler.Default;
            var binfiles = Directory.GetFiles(workspaceDir, "*.bin", SearchOption.TopDirectoryOnly);
            foreach ( var binfile in binfiles ) 
            {
                File.Delete(binfile);   
            }
            var cmd = "cmd.exe";
            var solcpath = File.Exists(Path.Combine(workspaceDir, "node_modules", "solc", "solc.js")) ? Path.Combine(workspaceDir, "node_modules", "solc", "solc.js") : Path.Combine(AssemblyLocation, "node_modules", "solc", "solc.js");
            // Use the configured JS runtime (default "node"; configurable in %LOCALAPPDATA%\Viscous\appsettings.json).
            var args = "/c " + AppSettings.JSRuntimeCmd + " " + "\"" + solcpath + "\"" + " --base-path=\"" + workspaceDir + "\"" + " \"" + file + "\" --bin";
            if (Directory.Exists(Path.Combine(workspaceDir, "node_modules")))
            {
                args += " --include-path=" + Path.Combine(workspaceDir, "node_modules");
            }
            var output = await RunCmdAsync(cmd, args, workspaceDir);
            if (CheckRunCmdError(output)) 
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogError("Viscous", "Could not run process: " + cmd + " " + args + ": " + GetRunCmdError(output));
                return;
            }
            else if (output.ContainsKey("stdout"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("Viscous", (string)output["stdout"]);
                return;
            }
            else if (output.ContainsKey("stderr"))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VSUtil.LogInfo("Viscous", (string)output["stderr"]);
                return;
            }
            else
            {
                binfiles = Directory.GetFiles(workspaceDir, "*.bin", SearchOption.TopDirectoryOnly);
                if (binfiles is null || binfiles.Length == 0) 
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    VSUtil.LogError("Viscous", "Could not read Solidity compiler output. No compiler output files found.");
                    return;
                }
                else
                {
                    string b = null;
                    foreach (var binfile in binfiles)
                    {
                        if (binfile.Contains(Path.GetFileNameWithoutExtension(file)))
                        {
                            b = File.ReadAllText(binfile);                  
                        }
                        File.Delete(binfile);
                    }
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (b == null)
                    {
                        VSUtil.LogError("Viscous", "Error reading Solidity compiler output: could not find compiler output file for " + file + ".");
                    }
                    else
                    {
                        VSUtil.LogInfo("Viscous", "======= " + file + "======= " + "\nBinary: \n" + b);
                    }
                    return;
                }
            }
        }

        public static async Task InstallNPMPackagesAsync(string projectDir)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSUtil.ShowLogOutputWindowPane(ServiceProvider.GlobalProvider, "Viscous");
            VSUtil.LogInfo("Viscous", string.Format("Installing JavaScript package dependencies in {0} using {1}...", projectDir, AppSettings.JSPackageManagerCmd));
            await TaskScheduler.Default;
            // Use the configured JS package manager (default "npm"; configurable in %LOCALAPPDATA%\Viscous\appsettings.json).
            var output = await RunCmdAsync("cmd.exe", "/c " + AppSettings.JSPackageManagerCmd + " install", projectDir);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (CheckRunCmdError(output))
            {
                VSUtil.LogError("Viscous", "Could not install NPM dependencies: " + GetRunCmdError(output));
                return;
            }
            VSUtil.LogInfo("Viscous", ((string)output["stdout"]).Trim());
        }

        public static SolidityCompilerIO2.SolidityCompilerOutput ParseOutputFile(string file) =>
            JsonConvert.DeserializeObject<SolidityCompilerIO2.SolidityCompilerOutput>(File.ReadAllText(file));

        public static string TryGetSolcVersion(string filepath, string projectDir)
        {
            var packagejsonFilePath = Path.Combine(projectDir, "package.json");
            if (File.Exists(packagejsonFilePath))
            {
                var packagejson = PackageJsonFile.Parse(File.ReadAllText(packagejsonFilePath));
                if (packagejson.Dependencies.ContainsKey("solc"))
                {
                    return packagejson.Dependencies["solc"];
                }
            }
            var solidityversion = SolidityFileParser.GetSolidityVersionRange(filepath);
            if ( solidityversion.StartsWith("^"))
            {
                return solidityversion.Substring(1);
            }
            else
            {
                VSUtil.LogError("Viscous", $"Could not parse Solidity file version {solidityversion}.");
                return null;
            }

        }
        public static async Task<bool> InstallSolcCompilerAsync(string compilerVersion)
        {
            string solcPath = SolcArtifactPath(compilerVersion);
            if (File.Exists(solcPath))
            {
                return true;
            }
            if (!await EnsureSolcSelectAsync())
            {
                return false;
            }
            var output = await RunCmdAsync(VenvPython, $"{SolcSelectInvoke} install {compilerVersion}", TaskToolsDir);
            if ((CheckRunCmdOutput(output, $"Version '{compilerVersion}' installed", true) || (CheckRunCmdOutput(output, $"Version '{compilerVersion}' is already installed, skipping...")) && File.Exists(solcPath)))
            {
                VSUtil.LogInfo("Viscous", $"solc {compilerVersion} compiler installed at {solcPath}.");
                return true;
            }
            else
            {
                VSUtil.LogError("Viscous", $"Could not install solc {compilerVersion} compiler: " + GetRunCmdError(output));
                return false;
            }
        }

        public static async Task<SoliditySlitherAnalysis> AnalyzeAsync(string filePath, string projectDir, string outputDir, string compilerVersion = null)
        {
            VSUtil.LogInfo("Viscous", $"Starting Slither analysis of {filePath}.");
            if (!await EnsureSlitherAsync())
            {
                VSUtil.LogError("Viscous", "Could not install Slither. Aborting analysis.");
                return null;
            }
            compilerVersion = compilerVersion ?? TryGetSolcVersion(filePath, projectDir);
            if (compilerVersion is null)
            {
                VSUtil.LogError("Viscous", "Could not determine solc version. Falling back to 0.8.27.");
                compilerVersion = "0.8.27";
            }
            if (!await InstallSolcCompilerAsync(compilerVersion))
            {
                VSUtil.LogError("Viscous", $"Could not install solc {compilerVersion} compiler.");
                return null;    
            }

            string solcPath = SolcArtifactPath(compilerVersion);
            string slitherargs = $"-m slither \"{filePath}\" --compile-force-framework solc --solc \"{solcPath}\" --solc-args \"--base-path {projectDir} --include-path {Path.Combine(projectDir, "node_modules")} \" --json -";
            var slithercmdrun = await RunCmdAsync(VenvPython, slitherargs, projectDir);
            var stdout = slithercmdrun.ContainsKey("stdout") ? (string)slithercmdrun["stdout"] : "";
            var stderr = slithercmdrun.ContainsKey("stderr") ? (string)slithercmdrun["stderr"] : "";
            if (stdout.Contains("\"success\": true"))
            {
                var r = JsonConvert.DeserializeObject<SoliditySlitherAnalysis>(stdout);
                VSUtil.LogInfo("Viscous", $"Slither analysis of {filePath} completed successfully.");
                return r;
            }
            else
            {
                VSUtil.LogError("Viscous", $"Slither analysis of {filePath} did not complete successfully. {stdout} {stderr}\nCheck if any build errors are present.");
                return null;
            }
        }
    }
}
