# About this project
VsSolidity is a Visual Studio extension that provides support for developing, building, and deploying Solidity smart contracts inside Visual Studio.

# Features
* Solidity project system featuring support for managing smart contract dependencies and Solidity compiler integration with MSBuild
* Integrates with Visual Studio solutions and "New Project..." dialog
* Integrates with Visual Studio "Open Folder..." feature
* Uses [vscode-solidity](https://github.com/juanfranblanco/vscode-solidity) language server
* Syntax highlighting and hover information
* Intellisense
* Linting
* Compile Solidity projects and files from inside Visual Studio or on the command-line using MSBuild with error reporting
* Generate .NET bindings to Solidity smart contracts from inside Visual Studio
* Blockchain Explorer to expolore and manage EVM-compatible blockchains
* Deploy compiled Solidity project to a blockchain from inside Visual Studio
* Analyze Solidity project using Slither and display the results inside Visual Studio

# Project implementation
	
## Project design and architecture

VsSolidity is written in .NET and C# targetting .NET Framework 4.7.2 for compatibility with the Visual Studio extension system. There are 7 main projects:
- VsSolidity.Base at src/VsSolidity.Base provides global base types and features like logging for all other projects.
- VsSolidity.BuildTasks at src/VsSolidity.BuildTasks provides the custom MSBuild task for building Solidity projects
- VsSolidity.Ethereum at src/VsSolidity.Ethereum provides an interface to the Solidity compiler and a Solidity language parser as well as clients to interact with EVM blockchains and for different cloud blockchain data services. 
- VsSolidity.SolidityFileItemTemplate at src/VsSolidity.SolidityFileItemTemplate provides the Visual Studio file template for the Solidity *.sol file type
- VsSolidity.SolidityProjectTemplate at src/VsSolidity.SolidityProjectTemplate provides the Visual Studio template for Solidity projects
- VsSolidity.Vsix at src/VsSolidity.Vsix provides the main Visual Studio extension project for VsSolidity

## Project coding instructions:
When generating new C# code, please follow the existing coding style.
- All code should be compatible with .NET 4.7.2 and C# 7.4.
- Prefer functional programming paradigms and constructs where appropriate.
- Prefer concise code over more verbose constructs.
- Avoid modifying external library code located in the @ext directory. Changes should be limited to the code in the @src directory only whenever possible.

## Project coding style:
- Use the existing #regions in a file to organize class constructors, indexers, events, properties, methods, fields, and child types. When making changes try to keep different class element types like fields and methods in the specified regions.
- Use 4 spaces for indentation.
- Use camel-case for method and property names. Method and property names should begin with a capital letter.
- Use camel-case for class fields. Field names should begin with lower-case letters unless they are backing fields for properties which should begin with an underscore.

## Common Commands
```bash
dotnet build <csproj_file>                                     # Build a project
dotnet test <csproj_file>                                      # Run unit tests in project.
```