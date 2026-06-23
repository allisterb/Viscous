# VsSolidity

**VsSolidity** is a Visual Studio extension for developing, compiling, and deploying [Solidity](https://soliditylang.org/) smart contracts to EVM-compatible blockchain networks.


![Blockchain Explorer](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/blockchain-explorer.png)

## Features

- **Solidity project system** — First‑class Visual Studio project type with Solidity
  compiler integration and npm dependency management.
- Integrates with the Visual Studio **New Project…** and **Open Folder…**
  experiences.
- - **Rich editing** — syntax highlighting, hover information, IntelliSense, and
  linting. Uses the  [vscode‑solidity](https://github.com/juanfranblanco/vscode-solidity) language
  server.
- **Integration with Visual Studio Build command and MSBuild** — compile Solidity projects and individual files from the IDE or on
  the command line with MSBuild, with errors reported in the Error List.
- **Blockchain Explorer** — manage EVM networks, endpoints, accounts, deploy
  profiles, and deployed contracts.
- **Deploy & run** — deploy a compiled contract to a network and call its
  functions or send transactions, all from the IDE.
- - **.NET bindings** — strongly‑typed C# contract bindings generated on every
  build (via Nethereum).
- **Static analysis** — find vulnerabilities and code‑quality issues with
  [Slither](https://github.com/crytic/slither), shown inside Visual Studio.

## Documentation

Full user documentation lives in the [`docs/`](docs/README.md) folder.

**Develop**

- [Creating and Opening a Project](docs/creating-a-project.md)
- [Editing Solidity Code](docs/editing-solidity.md)
- [Building and Compiling](docs/building-and-compiling.md)
- [.NET Bindings](docs/dotnet-bindings.md)
- [Static Analysis with Slither](docs/static-analysis.md)

**Connect, deploy, and run**

- [Blockchain Explorer](docs/blockchain-explorer.md)
- [Deploy a Smart Contract](docs/deploy-smart-contract.md)
- [Run a Smart Contract](docs/run-smart-contract.md)

## Getting started

1. Install the extension and open Visual Studio.
2. Create a new **Solidity Project** (**File → New → Project**, search
   *Solidity*) or open a folder of `.sol` files — see
   [Creating and Opening a Project](docs/creating-a-project.md).
3. Build the project to compile your contracts and generate bindings — see
   [Building and Compiling](docs/building-and-compiling.md).
4. Configure a network and a deploy profile in the
   [Blockchain Explorer](docs/blockchain-explorer.md), then
   [deploy](docs/deploy-smart-contract.md) and
   [run](docs/run-smart-contract.md) your contract.

### Prerequisites

- **Visual Studio** with the extension installed.
- **Node.js** — used for the language server and npm packages.
- **.NET SDK** (`dotnet`) — used to generate the C# contract bindings.
- A reachable EVM JSON‑RPC endpoint to deploy/run against (a local
  [Ganache](https://trufflesuite.com/ganache/) node is detected automatically).

## Building the extension from source

VsSolidity targets **.NET Framework 4.7.2 / C# 7.4** for Visual Studio
compatibility. The solution is under [`src/`](src/); the main projects are
described in [CLAUDE.md](CLAUDE.md). Build the VSIX with Visual Studio's
`MSBuild.exe` (the VSSDK projects don't build with `dotnet build`).
