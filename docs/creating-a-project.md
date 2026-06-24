# Creating and Opening a Solidity Project

Viscous adds a first‑class **Solidity project** type to Visual Studio, so you
can create, build, and manage smart contracts the same way you would any other
project. You can also open an existing folder of `.sol` files without a project
file.

## Creating a new Solidity project

![The New Project dialog with the Solidity Project template](https://ajb.nyc3.cdn.digitaloceanspaces.com/Viscous/docs/images/new-project-dialog.png)

1. Choose **File → New → Project**.
2. In the *Create a new project* dialog, search for **Solidity** (or filter by
   the **Blockchain** project‑type tag).
3. Select **Solidity Project** — *“A project for creating Ethereum‑compatible
   Solidity smart contracts.”*
4. Give the project a name and location and click **Create**.

The new project opens in **Solution Explorer** with a starter contract and the
files needed to build. Solidity source files use the `.sol` extension; you can
add more with **Add → New Item → Solidity File**.

## Opening an existing folder

If you have a directory of Solidity sources that isn't a Visual Studio project,
use **File → Open → Folder…** and pick the folder. Viscous recognizes the
`.sol` files inside, gives you the editor experience (syntax highlighting, hover,
IntelliSense, linting), and starts the Solidity language server for that folder.

## Project properties

Right‑click the project in Solution Explorer and choose **Properties** to open
the **General** page. The settings that affect building and deployment are:

![The project Properties General page](https://ajb.nyc3.cdn.digitaloceanspaces.com/Viscous/docs/images/project-properties.png)

| Property | Meaning |
|----------|---------|
| **Solidity Compiler Version** | The `solc` version used to compile the project (e.g. `0.8.20`–`0.8.27`). Viscous downloads and installs the chosen compiler automatically the first time it is needed. |
| **EVM Version** | The target EVM version the compiler produces bytecode for (`cancun`, `shanghai`, `paris`, `london`, `berlin`, `istanbul`, `constantinople`). Match this to what your target network supports. |
| **.NET Bindings Namespace** | The namespace used for the generated C# contract bindings (defaults to `Ethereum`). See [.NET Bindings](dotnet-bindings.md). |

These values are stored in the project file and are read by the build.

## Managing Solidity dependencies (npm)

Solidity projects commonly pull in libraries (for example OpenZeppelin) through
npm into a `node_modules` folder. To install the dependencies declared in your
project's `package.json`:

- Right‑click the project (or a Solidity file) and choose **Install NPM
  packages**.

Viscous runs `npm install` in the project directory and reports progress in
the **Viscous** output pane. When you build or compile, the project's
`node_modules` folder is passed to the compiler as an include path so imports
like `@openzeppelin/contracts/...` resolve correctly.

> You can use a different package manager (for example **pnpm**) instead of npm
> by setting `JSPackageManagerCmd` in `%LOCALAPPDATA%\Viscous\appsettings.json`
> — see [Using a different JavaScript runtime or package manager](editing-solidity.md#using-a-different-javascript-runtime-or-package-manager).

## Next steps

- [Editing Solidity code](editing-solidity.md)
- [Building and compiling](building-and-compiling.md)
- [Deploy a Smart Contract](deploy-smart-contract.md)
