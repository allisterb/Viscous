namespace Viscous;

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;

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

    /// <summary>
    /// Ensures the settings file contains every key the current extension version knows about, adding any that a
    /// newer version introduced (so schema changes shipped in an update reach existing users) without overwriting
    /// values the user has already set. Safe to call once at startup; a no-op when nothing is missing.
    /// </summary>
    public static void EnsureUpToDate()
    {
        try
        {
            EnsureFileExists();
            var keys = new[] { JSPackageManagerCmdKey, JSRuntimeCmdKey, PythonCmdKey };
            if (keys.Any(k => string.IsNullOrWhiteSpace(Config[k])))
            {
                // The schema is closed (all keys are defined here), so regenerate the file from the known key set,
                // resolving each key to its existing value where present and its default where missing.
                File.WriteAllText(FilePath, BuildJson(
                    (JSPackageManagerCmdKey, JSPackageManagerCmd),
                    (JSRuntimeCmdKey, JSRuntimeCmd),
                    (PythonCmdKey, PythonCmd)));
                lock (_lock) { _config = null; } // invalidate the cache so the merged file is reloaded
            }
        }
        catch (Exception ex)
        {
            Runtime.Error(ex, "Could not update the Viscous settings file at {0}.", FilePath);
        }
    }

    private static string DefaultJson => BuildJson(
        (JSPackageManagerCmdKey, DefaultJSPackageManagerCmd),
        (JSRuntimeCmdKey, DefaultJSRuntimeCmd),
        (PythonCmdKey, DefaultPythonCmd));

    private static string BuildJson(params (string Key, string Value)[] entries)
    {
        var sb = new StringBuilder();
        sb.Append('{').Append(Environment.NewLine);
        for (int i = 0; i < entries.Length; i++)
        {
            sb.Append("  \"").Append(entries[i].Key).Append("\": \"").Append(Escape(entries[i].Value)).Append('"')
              .Append(i < entries.Length - 1 ? "," : "").Append(Environment.NewLine);
        }
        sb.Append('}').Append(Environment.NewLine);
        return sb.ToString();
    }

    // Escapes a value for embedding in a double-quoted JSON string (backslashes and quotes; our settings are simple
    // scalar strings, e.g. commands and file paths).
    private static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    #endregion

    #region Fields
    private static readonly object _lock = new object();
    private static IConfigurationRoot? _config;
    #endregion
}
