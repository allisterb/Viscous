# Viscous User Guide

Viscous is a Visual Studio extension for developing, building, and deploying
Solidity smart contracts to EVM‑compatible blockchains without leaving the IDE.

This guide covers the tool windows, project features, and context‑menu commands
you use to develop, build, and deploy Solidity smart contracts.

**Develop**

| Guide | What it covers |
|-------|----------------|
| [Creating and Opening a Project](creating-a-project.md) | Start a new Solidity project or open an existing folder, set the compiler/EVM version, and manage npm dependencies. |
| [Editing Solidity Code](editing-solidity.md) | Syntax highlighting, hover, IntelliSense, and linting via the Solidity language server. |
| [Building and Compiling](building-and-compiling.md) | Compile with MSBuild, read build errors, and find the compiler output. Works in the IDE and on the command line. |
| [.NET Bindings](dotnet-bindings.md) | The strongly‑typed C# contract bindings generated on each build. |
| [Static Analysis with Slither](static-analysis.md) | Find vulnerabilities and code‑quality issues and review them in Visual Studio. |

**Connect, deploy, and run**

| Guide | What it covers |
|-------|----------------|
| [Blockchain Explorer](blockchain-explorer.md) | Manage networks, endpoints, accounts, deploy profiles, and deployed contracts. This is the central place where all of your blockchain configuration lives. |
| [Deploy a Smart Contract](deploy-smart-contract.md) | Build and deploy a compiled contract from your Solidity project to a network. |
| [Run a Smart Contract](run-smart-contract.md) | Call read‑only functions and send state‑changing transactions to a deployed contract. |

## How the pieces fit together

A typical end‑to‑end workflow looks like this:

1. **Configure a network** in the **Blockchain Explorer** — add the network's
   JSON‑RPC endpoint, the accounts you want to use, and at least one
   **deploy profile** (an endpoint + account pairing used to sign and pay for
   transactions).
2. **Deploy** your contract with the **Deploy** window. After a successful
   deployment the contract is recorded automatically under the network's
   **Contracts** folder in the Blockchain Explorer.
3. **Run** the deployed contract from the Blockchain Explorer to read its state
   or send transactions to it.

## Before you start

- **Node.js** must be installed. Viscous uses it to run the Solidity language
  server (editor features) and to install npm packages; it is fetched/installed
  on first use into your local app data.
- The **.NET SDK** (`dotnet`) is needed to generate the C# contract
  [bindings](dotnet-bindings.md) during a build.
- To deploy or run a contract you need a running EVM node or a reachable
  JSON‑RPC endpoint. You can use [Ganache](https://trufflesuite.com/ganache/) node
  with endpoint typically at `http://127.0.0.1:7545`
- To deploy, your Solidity project must build successfully — Viscous compiles
  it as part of the deploy step.

## Where output goes

Deployments and contract calls write their results — transaction hashes,
contract addresses, return values, and errors — to the **Viscous** pane of
the Visual Studio **Output** window (**View → Output**, then pick *Viscous*
in the *Show output from* list). Keep it open while you deploy or run contracts.
