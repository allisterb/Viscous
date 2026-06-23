# Editing Solidity Code

When you open a `.sol` file — whether it's part of a
[Solidity project](creating-a-project.md) or in a folder you opened — VsSolidity
gives you a rich editing experience backed by the
[vscode‑solidity](https://github.com/juanfranblanco/vscode-solidity) language
server.

![The Solidity editor with highlighting and IntelliSense](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/editor.png)

## What you get in the editor

- **Syntax highlighting** for Solidity source.
- **Hover information** — hover over a symbol to see its type and documentation.
- **IntelliSense / completions** — code completion as you type, including
  suggestions from your imports.
- **Linting / diagnostics** — problems are reported inline with squiggles and in
  the **Error List**.

These features work on any file Visual Studio recognizes as Solidity content
(the `solidity` content type, which is mapped to the `.sol` extension).

## The Solidity language server

The editor features are provided by a language server that VsSolidity runs in the
background per solution or open folder.

- **Automatic install:** the first time you open a Solidity file, VsSolidity
  checks for the language server and, if it isn't present, installs it
  automatically (`vscode-solidity-server`). Progress is shown in the
  **VsSolidity** output pane. This one‑time setup needs a JavaScript runtime and
  package manager on your machine — **Node.js** and **npm** by default
  (configurable — see below).
- **Startup:** once installed, the server starts automatically and attaches to
  your `.sol` files. No configuration is required.

### Using a different JavaScript runtime or package manager

By default VsSolidity uses a globally installed **node** to run the language server and **npm** to
install it. You can point it at alternatives (for example **pnpm**) at different paths through a
settings file at:

```
%LOCALAPPDATA%\VsSolidity\appsettings.json
```

It's created automatically with the defaults the first time the extension is run:

```json
{
  "JSPackageManagerCmd": "npm",
  "JSRuntimeCmd": "node"
}
```

- **`JSPackageManagerCmd`** — the command used to install the language server,
  and to run **Install NPM packages** for your project. Change it to e.g. `pnpm`.
- **`JSRuntimeCmd`** — the command used to run the language server, and the command‑line `solc.js` compiler when compiling individual Solidity files (**Compile Solidity File**).

Each value can be a bare command on your `PATH`, or a full path to an executable —
so you can point `JSRuntimeCmd` at a **local/portable Node** build (for example
`C:\tools\node-v20\node.exe`) instead of relying on a globally‑installed one.
Changes take effect the next time the language server is installed or started.

**Supported:** package managers and runtimes that follow the standard
**`node_modules` / Node** model. **pnpm** is a drop‑in replacement for npm, and
any Node executable — global or a local/portable build — works.

**Not supported right now:** runtimes that don't use the `node_modules` model,
such as **Deno**. The install, run, and `solc.js` compile steps assume an
npm‑style `node_modules` layout and a Node‑compatible runtime, so stick to
Node‑compatible tooling.

## Resolving imports

For IntelliSense and linting to resolve imported libraries (for example
`@openzeppelin/contracts/...`), the imported packages need to be present in the
project's `node_modules` folder. Install them with **Install NPM packages** from
the project context menu — see
[Managing Solidity dependencies](creating-a-project.md#managing-solidity-dependencies-npm).

## Troubleshooting

- **No highlighting or completions:** confirm the file has a `.sol` extension and
  that your JavaScript runtime/package manager is installed and on your `PATH`
  (Node.js + npm by default — see above). Check the **VsSolidity** output pane for
  language server install/startup messages.
- **Unresolved imports:** run **Install NPM packages** so the libraries are in
  `node_modules`.
- **Stale diagnostics:** edit and save the file, or reopen it, to prompt the
  server to re‑analyze.

## Related

- [Building and compiling](building-and-compiling.md) — turn your sources into bytecode, ABI, and bindings
- [Static analysis with Slither](static-analysis.md)
