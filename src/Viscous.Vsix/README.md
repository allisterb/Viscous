# Viscous

Viscous is a Visual Studio extension for developing, compiling, and deploying [Solidity](https://soliditylang.org/) smart contracts to EVM-compatible blockchain networks.

## Features

* Solidity project system for Visual Studio featuring Solidity compiler integration and NPM dependency management. Integrates with the Visual Studio **New Project…** and **Open Folder…** dialogs.

* Uses the  [vscode‑solidity](https://github.com/juanfranblanco/vscode-solidity) language server.for syntax highlighting, hover information, IntelliSense, and linting. 

* Solidity compiler integration with MSBuild and the Visual Studio Build command - compile Solidity projects and individual files from the IDE with errors reported in the Errors tool window.

* Generate .NET bindings to Solidity smart contracts automatically using Nethereum.

* Manage EVM networks, endpoints, accounts, deploy profiles, and deployed contracts from the **Blockchain Explorer** tool window.

* Deploy a compiled contract to a blockchain network and call its functions from inside Visual Studio.

* Find vulnerabilities and code‑quality issues with [Slither](https://github.com/crytic/slither) static analysis inside Visual Studio.

## Requirements
* Visual Studio 2022 and above
* A recent version of [Node.js](https://nodejs.org/) or compatible runtime

## User Guide
- [Creating and Opening Projects](https://github.com/allisterb/Viscous/blob/master/docs/creating-a-project.md)
- [Editing Solidity Code](https://github.com/allisterb/Viscous/blob/master/docs/editing-solidity.md)
- [Building and Compiiling](https://github.com/allisterb/Viscous/blob/master/docs/building-and-compiling.md)
- [.NET Bindings](https://github.com/allisterb/Viscous/blob/master/docs/dotnet-bindings.md)
- [Blockchain Explorer](https://github.com/allisterb/Viscous/blob/master/docs/blockchain-explorer.md)
- [Deploy a Smart Contract](https://github.com/allisterb/Viscous/blob/master/docs/deploy-smart-contract.md)
- [Run a Smart Contract](https://github.com/allisterb/Viscous/blob/master/docs/run-smart-contract.md)
- [Static Analysis with Slither](https://github.com/allisterb/Viscous/blob/master/docs/static-analysis.md)

## Getting started

1. Install the extension and open Visual Studio.
2. Edit the %LOCALAPPDATA%\Viscous\appsettings.json file to set different paths to the node and npm executables you want to use for the extension's language server. The extension's private .npmrc has `ignore-scripts=true`
2. Create a new **Solidity Project** (**File → New → Project**, set *Solidity* as the Language.) Alternatively you can just open a folder of `.sol` files to edit individual files. See [Creating and Opening Projects](docs/creating-a-project.md) 
3. Add and edit Solidity smart contract files.  Use Visual Studio's `package.json` editing support to add your smart contract dependencies and right-click on an existing package.json and select **Install NPM dependencies**.
4. Build the project to compile your contracts and generate bindings. If you just opened a folder then right-click and compile an individual Solidity contract.  See [Building and Compiling Projects](docs/building-and-compiling.md)
5. For Solidity projects, configure a network and a deploy profile in the [Blockchain Explorer](docs/blockchain-explorer.md), then [deploy](docs/deploy-smart-contract.md) and [run](docs/run-smart-contract.md) your Solidity contracts.
6. Right-click on a Solidity contract and click **Analyze Contract** to run [Slither static analysis](https://github.com/allisterb/Viscous/blob/master/docs/static-analysis.md).

## Usage Notes
* The very first time you open a Solidity file the extension will install the Node.js language server package in the extension's private `node_modules` directory. This will take a few seconds to complete and Solidity IntelliSense and hover information etc. won't be available during this time. Once the language server is installed the first time, editing Solidity contracts will be as usual.