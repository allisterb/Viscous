# Blockchain Explorer

The **Blockchain Explorer** is the central place where Viscous stores
everything it needs to talk to a blockchain: the **networks** you connect to,
their **RPC endpoints**, the **accounts** you transact with, the **deploy
profiles** used to sign and pay for transactions, and the **contracts** you have
deployed.

Your configuration is saved per user and persists between Visual Studio
sessions, so you only need to set up a network once.

![The Blockchain Explorer tool window](https://ajb.nyc3.cdn.digitaloceanspaces.com/viscous/docs/images/blockchain-explorer.png)

## Opening the Blockchain Explorer

Choose **View → Other Windows → Blockchain Explorer**.

The window shows a tree. The first time you open it, Viscous tries to reach a
local [Ganache](https://trufflesuite.com/ganache/) node at
`http://127.0.0.1:7545`. If the node responds, a **Ganache** network (chain id
`1337`) is created for you, pre‑populated with the node's accounts and a
**Deploy locally** deploy profile so you can deploy immediately.

## The tree structure

```
EVM Networks                 ← root folder
└── <Network>                ← e.g. Ganache, Sepolia, Polygon…
    ├── Endpoints            ← one or more JSON-RPC URLs for the network
    ├── Accounts             ← public keys (optionally labelled)
    ├── Contracts            ← contracts you have deployed to this network
    └── Deploy Profiles      ← endpoint + account pairings used to deploy/transact
```

You can also create your own **folders** under **EVM Networks** to group
networks however you like (for example *Local*, *Testnets*, *Mainnets*).

An **account** you've labelled shows its label; otherwise accounts and contracts
appear as a shortened address (e.g. `0x1234…ABCD`). Hover a network node to see
its chain id.

## Working with the tree

Right‑click a node to act on it. The available commands depend on what kind of
node you click:

| Right‑click on… | Commands |
|-----------------|----------|
| **EVM Networks** (root) or a user folder | **Add Network…**, **Add Folder…** *(user folders also have **Delete**)* |
| A **Network** | **Add Endpoint…**, **Add Account…**, **Add Deploy Profile…**, **Delete** |
| An **Endpoint** | **Delete** *(disabled if it is the network's only endpoint)* |
| An **Account** | **Copy Address**, **Edit…**, **Delete** |
| A **Deploy Profile** | **Edit…**, **Delete** |
| A **Contract** | **View…**, **Run…**, **Delete** |

> The predefined sub‑folders — **Endpoints**, **Accounts**, **Contracts**, and
> **Deploy Profiles** — don't have a menu of their own. Every "Add…" action lives
> on the parent **Network** node.

**Double‑click** shortcuts:

- Double‑click an **account** to open its **Edit Account** dialog.
- Double‑click a **contract** to open its read‑only **Contract details** view.

## Adding a network

![The Add EVM Network dialog](https://ajb.nyc3.cdn.digitaloceanspaces.com/viscous/docs/images/add-network-dialog.png)

1. Right‑click **EVM Networks** (or a folder) and choose **Add Network…**.
2. Enter a **Name**, the network's **JSON‑RPC URL**, and optionally a
   **chain id**.
3. Click **Save**. Viscous connects to the endpoint to confirm it is
   reachable and reads back the network's chain id, network id, and accounts.
   - If you left the chain id blank, it is filled in automatically.
   - If you entered a chain id that does not match what the endpoint reports,
     you are warned and the network is not added.

The new network is created with its **Endpoints**, **Accounts**, **Contracts**,
and **Deploy Profiles** folders. Any accounts exposed by the endpoint are added
under **Accounts**.

## Endpoints

A network can have more than one JSON‑RPC endpoint. Right‑click the **Network**
and choose **Add Endpoint…** to add another URL. You cannot delete a network's
last remaining endpoint.

## Accounts

Right‑click a **Network** and choose **Add Account…** to add an account by its
address, with an optional **label** to make it easy to recognize and an
optional **private key** (64 hex characters, optionally `0x`‑prefixed) to sign
with.

- **Copy Address** copies the account's address to the clipboard.
- **Edit…** lets you change the label or set/replace the private key.

### Private keys 
For a local node with unlocked accounts (such as Ganache) you can don't need private keys for transactions. For real-world networks there are two ways of entering private keys for signing transactions for deploying and executing smart contracts: store with the account or on-demand.
When an account stores a private key, Viscous signs all deployments and transactions with it when that account is used. **Keys are stored encrypted on disk using Windows DPAPI, scoped to your user account, in the
Blockchain Explorer's saved data (your Visual Studio user settings).** 

To change a saved key, open the account's **Edit…** dialog and type a new one (leaving the field blank keeps the existing key). 

You don't need to store private keys with your account since you can just enter it on-demand in the **Deploy Profile** and **Run contract**
dialogs. The private key box in those dialogs will read‑only when the selected account already has a stored key — that key is used automatically.

However you enter your private keys, a key is used only locally, to sign transactions on your machine. It is never sent to an RPC endpoint or anywhere else; only the resulting signed transaction goes
to the network.

### Private keys storage
Viscous encrypts private keys with the **Windows Data Protection API (DPAPI)** — `ProtectedData.Protect` scoped to **`CurrentUser`** — and stores only the encrypted blob alongside the account in the
Blockchain Explorer's saved data in the Visual Studio user settings. What that means in practice:

- The key can only be decrypted by the **same Windows user account on the same
  machine**. The encryption is tied to your Windows login by the operating system, so
  another user — or the settings copied to a different machine — cannot decrypt it.
 
- To **remove** a stored key, open the account's **Edit…** dialog, clear the Private
  Key field, and save (or delete the account).

- DPAPI `CurrentUser` protects against *other* users on the machine and against someone
  reading the raw settings offline. It does **not** protect against malicious software
  running under **your own** Windows account — such code can ask DPAPI to decrypt the
  key exactly as Viscous does. Your machine's own security is the trust boundary.

- For that reason, prefer **dedicated development / testnet accounts** over high‑value
  or treasury keys, especially while the deploy/sign features are still maturing. For a
  local node with unlocked accounts (e.g. Ganache) you don't need to store a key at all.

## Deploy profiles

A **deploy profile** bundles together the **endpoint** and **account** that
should be used when deploying a contract or sending a transaction. The signing
key comes from the selected **account** (see [Accounts](#accounts)) — profiles
don't store a key of their own.

1. Right‑click a **Network** and choose **Add Deploy Profile…**.
2. Enter a **Name**, pick an **Endpoint** and an **Account** from the network.
   The **Private Key** box is read‑only and shows the selected account's stored
   key (masked), or is blank when the account has none — to change it, edit the
   account.
3. Click **Save**.

Deploy profiles are what the [Deploy](deploy-smart-contract.md) window lists in
its profile picker, so you need at least one before you can deploy.

## Contracts

You normally don't add contracts by hand — when you
[deploy a contract](deploy-smart-contract.md), it is recorded automatically
under the target network's **Contracts** folder, along with its address, the
deploying account, the transaction hash, the deployment time, and its ABI.

For a contract node:

- **View…** opens a read‑only **Contract details** view — address, label,
  creator, transaction hash, deployment date, and ABI. From there you can click
  **Run** to jump straight to running it, or **Close**. (These details are
  recorded at deploy time and aren't edited here.)
- **Run…** opens the **Run Contract** dialog — see
  [Run a Smart Contract](run-smart-contract.md).
- **Delete** removes the contract from the Explorer. (This only removes it from
  your local list; it does not affect the contract on‑chain.)

## See also

- [Deploy a Smart Contract](deploy-smart-contract.md)
- [Run a Smart Contract](run-smart-contract.md)
