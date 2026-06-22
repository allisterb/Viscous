# Building and Compiling

VsSolidity compiles Solidity through a custom **MSBuild task**, so building a
Solidity project works exactly like building any other project in Visual Studio —
and the same build works on the command line and in CI.

## Building a project

Build a Solidity project the usual ways:

- **Build → Build Solution** (or **Build → Build *\<project\>***)
- Right‑click the project in Solution Explorer → **Build**
- Press the usual build shortcut

On build, the MSBuild task:

1. **Installs the compiler** — if the `solc` version set in the project's
   [Solidity Compiler Version](creating-a-project.md#project-properties)
   property isn't already present, it is downloaded and installed automatically.
2. **Compiles all contracts** with `solc` using the standard‑JSON interface,
   targeting the project's **EVM Version**, with the project directory and
   `node_modules` on the include path so imports resolve.
3. **Reports errors and warnings** (see below).
4. **Writes compiler output** to the project's output directory.
5. **Generates .NET bindings** for each contract — see
   [.NET Bindings](dotnet-bindings.md).
6. **Runs Slither static analysis** over the project — see
   [Static analysis](static-analysis.md).

## Error and warning reporting

Compiler diagnostics are surfaced as native Visual Studio build messages:

![Compiler diagnostics in the Output window and Error List](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/build-errors.png)

- **Errors** and **warnings** appear in the **Error List** and the **Output**
  window, each with the **file, line, and column** of the offending source so you
  can double‑click to jump straight to it.
- A build with any compiler **error** fails; warnings do not stop the build.

## Build output files

For each compiled contract, the build writes these files to the project's output
directory (e.g. `bin\Debug`):

| File | Contents |
|------|----------|
| `<file>.<contract>.bin` | The compiled EVM **bytecode** (used when deploying). |
| `<file>.<contract>.abi` | The contract **ABI** (used for deploying and for [running](run-smart-contract.md) the contract). |
| `<file>.<contract>.gas.json` | Gas estimates for the contract's functions. |
| `<file>.<contract>.opcodes.txt` | The disassembled opcodes. |
| `<file>.<contract>.ast.json` | The generated‑sources AST, when available. |
| `compileroutput.json` | The full raw compiler output. |
| `slither-analysis.json` | Results of the Slither run (see [Static analysis](static-analysis.md)). |

The [Deploy](deploy-smart-contract.md) window uses the `.bin` and `.abi` files
produced here, which is why it builds the project before deploying.

## Compiling a single file

To compile just one contract without a full project build, right‑click a `.sol`
file and choose **Compile Solidity File**. This runs the `solc` compiler over
that file and writes the result to the **VsSolidity** output pane. It's a quick
way to check that a single file compiles.

## Building from the command line / CI

Because compilation is an MSBuild task, you can build a Solidity project outside
Visual Studio with MSBuild — for example:

```
msbuild MyContracts.solproj /t:Build
```

The same compiler install, error reporting, output files, bindings generation,
and Slither analysis run as they do inside the IDE, which makes Solidity projects
straightforward to build in an automated pipeline.

> The build relies on a few external tools (Node.js for the language server and
> npm packages, and the `solc`/Slither tooling VsSolidity manages under your
> local app data). Make sure Node.js is available on the build machine.

## Related

- [Creating a project](creating-a-project.md) — compiler/EVM version settings
- [.NET Bindings](dotnet-bindings.md)
- [Static analysis with Slither](static-analysis.md)
- [Deploy a Smart Contract](deploy-smart-contract.md)
