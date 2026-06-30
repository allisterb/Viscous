namespace Viscous;

using Microsoft.Extensions.Configuration;
using System;
using System.IO;

/// <summary>
/// User-editable settings stored in <c>%LOCALAPPDATA%\Viscous\appsettings.json</c>.
/// Lets the user point Viscous at an alternate JavaScript runtime / package manager
/// (for example <c>pnpm</c> or <c>deno</c>) instead of the default <c>node</c> / <c>npm</c>.
/// The file is created with the defaults the first time a setting is read.
/// </summary>
public static class AppSettings
{
    #region Keys and defaults
    public const string JSPackageManagerCmdKey = "JSPackageManagerCmd";
    public const string JSRuntimeCmdKey = "JSRuntimeCmd";
    public const string PythonCmdKey = "PythonCmd";

    public const string DefaultJSPackageManagerCmd = "npm";
    public const string DefaultJSRuntimeCmd = "node";
    public const string DefaultPythonCmd = "py -3";
    #endregion

    #region Properties
    /// <summary>Full path to the settings file.</summary>
    public static string FilePath => Path.Combine(Runtime.ViscousDir, "appsettings.json");

    /// <summary>
    /// The JavaScript package manager command used to install the language server (default <c>npm</c>).
    /// May include arguments, e.g. <c>pnpm</c>.
    /// </summary>
    public static string JSPackageManagerCmd => Get(JSPackageManagerCmdKey, DefaultJSPackageManagerCmd);

    /// <summary>
    /// The JavaScript runtime command used to run the language server (default <c>node</c>).
    /// May include arguments, e.g. <c>deno run -A</c>.
    /// </summary>
    public static string JSRuntimeCmd => Get(JSRuntimeCmdKey, DefaultJSRuntimeCmd);

    /// <summary>
    /// The Python command used to create the virtual environment that hosts the
    /// solc-select and slither analysis tools (default <c>py -3</c>). Requires Python 3.8+.
    /// May be a bare command or a full path, e.g. <c>C:\Python312\python.exe</c>.
    /// Only used to bootstrap the venv; the tools themselves are run with the venv's
    /// own interpreter.
    /// </summary>
    public static string PythonCmd => Get(PythonCmdKey, DefaultPythonCmd);

    private static IConfigurationRoot Config
    {
        get
        {
            if (_config is null)
            {
                lock (_lock)
                {
                    if (_config is null)
                    {
                        EnsureFileExists();
                        _config = new ConfigurationBuilder()
                            .SetBasePath(Runtime.ViscousDir)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .Build();
                    }
                }
            }
            return _config;
        }
    }
    #endregion

    #region Methods
    private static string Get(string key, string fallback)
    {
        var v = Config[key];
        return string.IsNullOrWhiteSpace(v) ? fallback : v!.Trim();
    }

    public static void EnsureFileExists()
    {
        try
        {
            Runtime.CreateIfDirectoryDoesNotExist(Runtime.ViscousDir);
            if (!File.Exists(FilePath))
            {
                File.WriteAllText(FilePath, DefaultJson);
            }
        }
        catch (Exception ex)
        {
            Runtime.Error(ex, "Could not create the default Viscous settings file at {0}.", FilePath);
        }
    }

    private static string DefaultJson =>
        "{" + Environment.NewLine +
        "  \"" + JSPackageManagerCmdKey + "\": \"" + DefaultJSPackageManagerCmd + "\"," + Environment.NewLine +
        "  \"" + JSRuntimeCmdKey + "\": \"" + DefaultJSRuntimeCmd + "\"," + Environment.NewLine +
        "  \"" + PythonCmdKey + "\": \"" + DefaultPythonCmd.Replace("\\", "\\\\") + "\"" + Environment.NewLine +
        "}" + Environment.NewLine;
    #endregion

    #region Fields
    private static readonly object _lock = new object();
    private static IConfigurationRoot? _config;
    #endregion
}
