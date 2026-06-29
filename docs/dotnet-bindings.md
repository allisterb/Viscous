# .NET Bindings

When you build a Solidity project, Viscous automatically generates **C# (.NET)
bindings** for your contracts — strongly‑typed classes that let you deploy and
call your contracts from .NET code (via [Nethereum](https://nethereum.com/))
without hand‑writing ABI plumbing.

![The generated bindings folder](https://ajb.nyc3.cdn.digitaloceanspaces.com/viscous/docs/images/dotnet-bindings.png)

## How bindings are generated

Bindings are produced as a step of the [build](building-and-compiling.md), right
after compilation:

1. The build compiles each contract to its `.abi` and `.bin` files.
2. It ensures the **Nethereum.Generator.Console** .NET tool is installed locally
   in the project (a one‑time `dotnet tool` setup, recorded in the project's
   `.config/dotnet-tools.json`).
3. For each contract it runs the generator over the contract's ABI and bytecode
   and writes the generated C# into a **`bindings`** folder in the project
   directory, under a subfolder per source file.

You don't need to invoke anything separately — just build the project.

## Choosing the namespace

The generated classes are placed in the namespace set by the
**.NET Bindings Namespace** project property (default: `Ethereum`). To change it:

1. Right‑click the project → **Properties → General**.
2. Set **.NET Bindings Namespace** to the namespace you want (for example
   `MyDapp.Contracts`).
3. Rebuild.

See [Project properties](creating-a-project.md#project-properties).

## What you get

For each contract the generator typically emits a service class plus the
associated function/event message and DTO types, all under your chosen
namespace. You can reference these from a .NET project to:

- deploy the contract,
- send transactions to its functions,
- query its state and decode return values and events,

using Nethereum's `Web3` client.

## Notes

- Bindings are regenerated on each build from the current ABI/bytecode, so they
  stay in sync with your contracts.
- Generation requires the .NET SDK (`dotnet`) to be available, since it uses a
  local `dotnet` tool. If the tool can't be installed, the build logs an error
  for the bindings step but the compiled `.abi`/`.bin` output is still produced.

## Related

- [Building and compiling](building-and-compiling.md)
- [Deploy a Smart Contract](deploy-smart-contract.md)
