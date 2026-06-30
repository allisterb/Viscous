# Static Analysis with Slither

Viscous integrates the [Slither](https://github.com/crytic/slither) static
analyzer so you can find common vulnerabilities and code‑quality issues in your
contracts and review the findings inside Visual Studio.

## Ways analysis runs

There are two ways Slither runs:

- **On demand, for a single file** — right‑click a `.sol` file and choose
  **Analyze Solidity File**. The results open in the **Solidity Static Analysis**
  tool window.
- **As part of a build** — every project [build](building-and-compiling.md) runs
  Slither over the whole project and writes the raw findings to
  `slither-analysis.json` in the output directory.

Viscous manages the Slither tool and the matching `solc` compiler version for
you; the analyzer uses the compiler version detected from your project (falling
back to a recent version if it can't be determined).

## Prerequisites

Slither and `solc-select` are Python tools. Viscous installs them from PyPI into a
private virtual environment under `%LOCALAPPDATA%\Viscous\venv` the first time it
needs them, so you must have **Python 3.8 or later** installed. By default Viscous
uses the `py -3` launcher; if Python is installed under a different command or path,
set `PythonCmd` in `%LOCALAPPDATA%\Viscous\appsettings.json` (for example
`"PythonCmd": "C:\\Python312\\python.exe"`).

## Opening the results window

The **Solidity Static Analysis** window opens automatically when you run
**Analyze Solidity File**. You can also open it any time from
**View → Other Windows → Solidity Static Analysis**.

## Reading the results

![The Solidity Static Analysis window with Slither findings](https://ajb.nyc3.cdn.digitaloceanspaces.com/viscous/docs/images/static-analysis-window.png)

Findings are organized in a tree, grouped into folders by **impact**:

```
Analysis
├── High
├── Medium
├── Low
├── Informational
└── Optimization
```

Each finding (a Slither **detector** result) appears under its impact folder,
showing a short description of the issue. Expand a finding to see its details:

| Property | Meaning |
|----------|---------|
| **Check** | The Slither detector that produced the finding. |
| **Confidence** | Slither's confidence in the result (e.g. High / Medium / Low). |
| **File** | The source file (and location) where the issue was found. |

Use the impact grouping to triage: start with **High** and **Medium** findings,
then review **Low**, **Informational**, and **Optimization** suggestions.

## Tips

- Re‑run **Analyze Solidity File** after edits to refresh the findings for that
  file.
- Because analysis also runs on every build, the `slither-analysis.json` in your
  output directory always reflects the last build — handy for diffing or feeding
  into other tooling.
- Slither needs the project to compile. If analysis reports nothing or fails,
  confirm the project [builds](building-and-compiling.md) and that imports
  resolve (run **Install NPM packages** if needed).

## Related

- [Building and compiling](building-and-compiling.md)
- [Editing Solidity code](editing-solidity.md)
