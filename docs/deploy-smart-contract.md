# Deploy a Smart Contract

The **Deploy** tool window builds your Solidity project and deploys one of its
compiled contracts to a network, using a **deploy profile** you have configured
in the [Blockchain Explorer](blockchain-explorer.md).

![The Deploy Solidity Project tool window](https://ajb.nyc3.cdn.digitaloceanspaces.com/viscous/docs/images/deploy-window.png)

## Before you deploy

- You have a Solidity project open that **builds successfully** — the Deploy
  window compiles it as part of deploying.
- You have created at least one **deploy profile** in the Blockchain Explorer
  for the network you want to deploy to. The profile supplies the endpoint and
  account used to send (and pay for) the deployment transaction.

## Opening the Deploy window

In **Solution Explorer**, right‑click the Solidity project you want to deploy
and choose **Deploy…**. The **Deploy Solidity Project** tool window opens,
already targeting that project.

> The Deploy window is opened from the project's context menu — it is not listed
> under **View → Other Windows**.

## The Deploy window, field by field

| Field | Description |
|-------|-------------|
| **Project EVM version** | The EVM version your project is configured to compile for, shown for reference. Make sure the target network supports this EVM version, or the deployment will fail. |
| **Select the contract to deploy** | The contract from your project to deploy. |
| **Constructor parameters** | Appears only when the selected contract's constructor takes arguments. One input is shown per parameter, labelled with its name and Solidity type. Fill these in before deploying. |
| **Select the deploy profile** | The [deploy profile](blockchain-explorer.md#deploy-profiles) to deploy with. The list contains every profile defined across your networks in the Blockchain Explorer. |
| **Private Key (optional)** | The signing key for the profile's account. If that [account](blockchain-explorer.md#accounts) already has a stored key, this box shows it (masked) and is read‑only; otherwise you can type a key to sign this deployment, or leave it blank for a node‑managed account. |
| **Gas Limit** | Choose **Estimated Gas** to let the network estimate the gas, or **Custom** to enter a specific gas limit. |
| **Value** | An optional amount (in wei) to send with the deployment, for contracts whose constructor is payable. Leave it `0` otherwise. |

## Deploying

1. Choose the **contract** and fill in any **constructor parameters**.
2. Choose the **deploy profile** for the target network. If its account has no
   stored key, optionally type a **private key** to sign with.
3. Pick a **gas** option and set a **value** if needed.
4. Click **Deploy**.

Viscous then:

1. **Builds** the project. If the build fails, deployment stops — check the
   build output for errors.
2. Reads the contract's compiled bytecode (`bin`) and ABI from the build output.
3. **Deploys** the contract using the deploy profile's endpoint and account.

> **Signing:** the key is taken from the profile's **account** if it has a stored
> one, otherwise from the **Private Key** box in the deploy dialog. With a key the
> deployment is signed locally, so you can deploy to hosted endpoints and public
> testnets (Infura, Alchemy, Sepolia, …). Without any key, Viscous asks the
> node to sign with an unlocked account — which works for a local node like
> Ganache. See [accounts](blockchain-explorer.md#accounts).

A status line at the bottom of the window shows progress, then either a success
message or an error. Click **Cancel** to dismiss the current status.

## After a successful deployment

- The contract is **recorded automatically** in the
  [Blockchain Explorer](blockchain-explorer.md) under the target network's
  **Contracts** folder, together with its address, creator account, transaction
  hash, deployment time, and ABI.
- The transaction hash and contract address are written to the **Viscous**
  pane of the **Output** window.

From there you can [run the contract](run-smart-contract.md) to call its
functions or send it transactions.

## Troubleshooting

| Message | Likely cause / fix |
|---------|--------------------|
| *Select a Solidity smart contract to deploy… and a deploy profile to use.* | A contract or deploy profile wasn't selected. Pick both. |
| *Build failed. Please check the build output for errors.* | The project did not compile. Fix the build errors and try again. |
| *No bin/abi file found…* | The compiler output is missing the bytecode/ABI for the contract. Confirm the contract compiles and produces output. |
| *Could not retrieve deploy profile…* | The selected profile could not be found. Re‑check it in the Blockchain Explorer. |
| *Error deploying contract: …* | The network rejected the deployment (for example, out of gas, unsupported EVM version, or a connection problem). The detailed reason is in the **Viscous** output pane. |

## See also

- [Blockchain Explorer](blockchain-explorer.md) — set up networks, accounts, and deploy profiles
- [Run a Smart Contract](run-smart-contract.md)
