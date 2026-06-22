# Blockchain Explorer

The **Blockchain Explorer** is the central place where VsSolidity stores
everything it needs to talk to a blockchain: the **networks** you connect to,
their **RPC endpoints**, the **accounts** you transact with, the **deploy
profiles** used to sign and pay for transactions, and the **contracts** you have
deployed.

Your configuration is saved per user and persists between Visual Studio
sessions, so you only need to set up a network once.

![The Blockchain Explorer tool window](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/blockchain-explorer.png)

## Opening the Blockchain Explorer

Choose **View → Other Windows → Blockchain Explorer**.

The window shows a tree. The first time you open it, VsSolidity tries to reach a
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

Accounts and contracts are shown using their **label** if you set one;
otherwise they appear as a shortened address (e.g. `0x1234…ABCD`). Hover a
network node to see its chain id.

## Working with the tree

Right‑click a node to act on it. The available commands depend on what kind of
node you click:

| Right‑click on… | Commands |
|-----------------|----------|
| **EVM Networks** (root) or a user folder | **Add Network…**, **Add Folder…** *(folders also have **Delete**)* |
| A **Network** | **Add Account…**, **Add Deploy Profile…**, **Delete** |
| **Endpoints** folder | **Add Endpoint…** |
| An **Endpoint** | **Delete** *(disabled if it is the network's only endpoint)* |
| **Accounts** → an **Account** | **Copy Address**, **Edit…**, **Delete** |
| **Deploy Profiles** folder | **Add Deploy Profile…** |
| A **Deploy Profile** | **Edit…**, **Delete** |
| **Contracts** → a **Contract** | **Edit…**, **Run…**, **Delete** |

**Double‑click** shortcuts:

- Double‑click an **account** to open its **Edit Account** dialog.
- Double‑click a **contract** to open its **Edit contract** dialog.

## Adding a network

![The Add EVM Network dialog](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/add-network-dialog.png)

1. Right‑click **EVM Networks** (or a folder) and choose **Add Network…**.
2. Enter a **Name**, the network's **JSON‑RPC URL**, and optionally a
   **chain id**.
3. Click **Save**. VsSolidity connects to the endpoint to confirm it is
   reachable and reads back the network's chain id, network id, and accounts.
   - If you left the chain id blank, it is filled in automatically.
   - If you entered a chain id that does not match what the endpoint reports,
     you are warned and the network is not added.

The new network is created with its **Endpoints**, **Accounts**, **Contracts**,
and **Deploy Profiles** folders. Any accounts exposed by the endpoint are added
under **Accounts**.

## Endpoints

A network can have more than one JSON‑RPC endpoint. Right‑click the
**Endpoints** folder and choose **Add Endpoint…** to add another URL. You cannot
delete a network's last remaining endpoint.

## Accounts

Right‑click a **Network** and choose **Add Account…** to add an account by its
public key, with an optional **label** to make it easy to recognize.

- **Copy Address** copies the account's address to the clipboard.
- **Edit…** lets you change the label.

## Deploy profiles

A **deploy profile** bundles together the **endpoint** and **account** that
should be used when deploying a contract or sending a transaction, plus an
optional **private key** to sign with.

1. Right‑click a **Network** (or its **Deploy Profiles** folder) and choose
   **Add Deploy Profile…**.
2. Enter a **Name**, pick an **Endpoint** and an **Account** from the network,
   and optionally supply the account's **Private Key**.
3. Click **Save**.

Deploy profiles are what the [Deploy](deploy-smart-contract.md) window lists in
its profile picker, so you need at least one before you can deploy.

> **Private keys** are held encrypted in memory (protected to your Windows
> logon) and are only needed for profiles that must sign transactions.

## Contracts

You normally don't add contracts by hand — when you
[deploy a contract](deploy-smart-contract.md), it is recorded automatically
under the target network's **Contracts** folder, along with its address, the
deploying account, the transaction hash, the deployment time, and its ABI.

For a contract node:

- **Edit…** shows its details — address, label, creator, transaction hash,
  deployment date, and ABI. From this dialog you can also click **Run** to jump
  straight to running it.
- **Run…** opens the **Run Contract** dialog — see
  [Run a Smart Contract](run-smart-contract.md).
- **Delete** removes the contract from the Explorer. (This only removes it from
  your local list; it does not affect the contract on‑chain.)

## See also

- [Deploy a Smart Contract](deploy-smart-contract.md)
- [Run a Smart Contract](run-smart-contract.md)
