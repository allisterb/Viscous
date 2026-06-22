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
  **VsSolidity** output pane. This one‑time setup needs Node.js available on
  your machine.
- **Startup:** once installed, the server starts automatically and attaches to
  your `.sol` files. No configuration is required.

## Resolving imports

For IntelliSense and linting to resolve imported libraries (for example
`@openzeppelin/contracts/...`), the imported packages need to be present in the
project's `node_modules` folder. Install them with **Install NPM packages** from
the project context menu — see
[Managing Solidity dependencies](creating-a-project.md#managing-solidity-dependencies-npm).

## Troubleshooting

- **No highlighting or completions:** confirm the file has a `.sol` extension and
  that Node.js is installed. Check the **VsSolidity** output pane for language
  server install/startup messages.
- **Unresolved imports:** run **Install NPM packages** so the libraries are in
  `node_modules`.
- **Stale diagnostics:** edit and save the file, or reopen it, to prompt the
  server to re‑analyze.

## Related

- [Building and compiling](building-and-compiling.md) — turn your sources into bytecode, ABI, and bindings
- [Static analysis with Slither](static-analysis.md)
