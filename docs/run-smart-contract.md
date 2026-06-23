# Run a Smart Contract

Once a contract is deployed and listed in the
[Blockchain Explorer](blockchain-explorer.md), you can interact with it directly
from Visual Studio — calling its read‑only functions to inspect state, or
sending transactions to functions that change state.

![The Run Contract dialog](https://ajb.nyc3.cdn.digitaloceanspaces.com/vssolidity/docs/images/run-contract-dialog.png)

> **Where to run a contract:** running a contract is done from the
> **Blockchain Explorer**, using the **Run Contract** dialog described below.

## Opening the Run Contract dialog

In the [Blockchain Explorer](blockchain-explorer.md), expand a network's
**Contracts** folder, then either:

- right‑click the contract and choose **Run…**, or
- right‑click the contract, choose **View…**, and click **Run** in the
  **Contract details** view.

The **Run contract** dialog opens for that contract.

## What the dialog shows

- **Contract balance** — the contract's current balance (in ETH) is fetched and
  displayed at the top.
- **A Function picker** — choose a function from the **Function** combo box. Its
  parameters then appear below as one input box each, labelled with the
  parameter name and Solidity type.
- **A single Run button** — executes the selected function.

## Calling a function (read‑only)

By default the dialog is in **call** mode — the **Transact (requires gas)**
checkbox is unchecked.

1. Pick the function from the **Function** combo box.
2. Fill in any parameters that appear.
3. Click **Run**.

The function is executed as a read‑only **call**: it returns a value without
changing on‑chain state and without costing gas. The return value is shown in
the dialog and written to the **VsSolidity** output pane.

This is the right mode for `view` / `pure` functions and for reading public
state. `view` / `pure` functions are always executed as a call (and return their
decoded value) even when **Transact** is checked.

## Sending a transaction (state‑changing)

To call a function that changes state, you must send a **transaction**, which
requires an account to send from and gas to pay for it.

1. Check **Transact (requires gas)**. This enables the transaction options:
   - **Account** — pick the address to send from, from the network's saved
     accounts. It defaults to the contract's deployer.
   - **Private Key (optional)** — the sending account's key, used to sign the
     transaction locally. Required for hosted endpoints and public testnets that
     won't sign for you; for a local node with the account unlocked (e.g. Ganache)
     you can leave it blank. If the selected account already has a stored key (see
     [accounts](blockchain-explorer.md#accounts)) this box is read‑only and that
     key is used automatically; otherwise you can type one for this transaction.
   - **Gas Limit** — choose **Estimated Gas**, or select **Custom** and enter a
     specific gas limit (default `3000000`).
2. Pick the function from the **Function** combo box and fill in its parameters.
3. Click **Run**.

The transaction is submitted to the network. Because a transaction returns a
**transaction hash** rather than the function's return value, the result line
shows the hash; read‑backs of stored values should be done with a `view`
function in call mode. The result (or any error) is shown in the dialog and
logged to the **VsSolidity** output pane.

## Reading the results

Every call and transaction is recorded in the **VsSolidity** pane of the
**Output** window (**View → Output → Show output from: VsSolidity**), including:

- the contract address and endpoint,
- the function name and the arguments you passed,
- the returned value, or the failure reason if it didn't succeed.

Keep this pane open while you work so you have a running log of what was sent and
what came back.

## Tips

- If a function reports a parameter error, check that you supplied a value for
  **every** parameter and that each value matches the parameter's Solidity type
  shown in its label.
- Use a read‑only **call** first to confirm inputs and behavior, then check
  **Transact** only when you actually want to change on‑chain state.
- To run a contract you deployed from a different machine or session, make sure
  it is listed under the correct network's **Contracts** folder in the
  Blockchain Explorer, with its ABI populated.

## See also

- [Blockchain Explorer](blockchain-explorer.md)
- [Deploy a Smart Contract](deploy-smart-contract.md)
