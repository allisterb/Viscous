# About

VsSolidity is a Visual Studio extension for developing, compiling, and deploying [Solidity](https://soliditylang.org/) smart contracts to EVM-compatible blockchain networks.

![Blockchain Explorer](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/blockchain-explorer.png)

## Features

* Solidity project system for Visual Studio featuring Solidity compiler integration and NPM dependency management. Integrates with the Visual Studio **New Project…** and **Open Folder…** dialogs.

* Uses the  [vscode‑solidity](https://github.com/juanfranblanco/vscode-solidity) language server.for syntax highlighting, hover information, IntelliSense, and linting. 

* Solidity compiler integration with MSBuild and the Visual Studio Build command - compile Solidity projects and individual files from the IDE with errors reported in the Error List tool window.

* Generate .NET bindings to Solidity smart contracts automatically using Nethereum.

* Manage EVM networks, endpoints, accounts, deploy profiles, and deployed contracts from the **Blockchain Explorer** tool window.

* Deploy a compiled contract to a blockchain network and call its functions from inside Visual Studio.

* Find vulnerabilities and code‑quality issues with [Slither](https://github.com/crytic/slither) static analysis inside Visual Studio.

## Requirements
* Visual Studio 2022 and above
* A recent version of [Node.js](https://nodejs.org/) or compatible runtime

## User Guide
- [Creating and Opening Projects](https://github.com/allisterb/VsSolidity/blob/master/docs/creating-a-project.md)
- [Editing Solidity Code](https://github.com/allisterb/VsSolidity/blob/master/docs/editing-solidity.md)
- [Building and Compiiling](https://github.com/allisterb/VsSolidity/blob/master/docs/building-and-compiling.md)
- [.NET Bindings](https://github.com/allisterb/VsSolidity/blob/master/docs/dotnet-bindings.md)
- [Blockchain Explorer](https://github.com/allisterb/VsSolidity/blob/master/docs/blockchain-explorer.md)
- [Deploy a Smart Contract](https://github.com/allisterb/VsSolidity/blob/master/docs/deploy-smart-contract.md)
- [Run a Smart Contract](https://github.com/allisterb/VsSolidity/blob/master/docs/run-smart-contract.md)
- [Static Analysis with Slither](https://github.com/allisterb/VsSolidity/blob/master/docs/static-analysis.md)

## Getting started

1. Build the extension inside Visual Studio
2. Run the extension in the Visual Studio Experimental Instance
3. Edit the %LOCALAPPDATA%\VsSolidity\appsettings.json file to set different paths to the node or npm executables you want to use with the extension.

## Usage Notes
The very first time you open a Solidity file the extension will install the Node.js language server package in the extension's private `node_modules` directory. This will take a few seconds to complete and Solidity IntelliSense and hover information etc. won't be available during this time. Once the language server is installed the first time, editing Solidity contracts will be as usual.
