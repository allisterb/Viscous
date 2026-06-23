using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;
using VsSolidity.Ethereum.Explorers;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace VsSolidity.Ethereum
{
    public class Network : Runtime
    {
        #region Constructor
        public Network(string rpcUrl, BigInteger chainid)
        {
            this.rpcUrl = rpcUrl;
            this.chainId = chainid;
            web3 = new Web3(rpcUrl);
        }
        #endregion

        #region Methods
        public async Task<BigInteger> GetBlockNoAsync() => await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

        public async Task<string[]> GetPredefinedAccountsAsync() => await web3.Eth.Accounts.SendRequestAsync();

        public async Task<BigInteger> GetBalanceAsync(string acct) => await web3.Eth.GetBalance.SendRequestAsync(acct);

        public Nethereum.Contracts.Contract GetContract(string contractAddress, string abi = null) => web3.Eth.GetContract(abi ?? "", contractAddress);

        public static Nethereum.Contracts.Contract GetContract(string rpcurl, string contractAddress, string abi = null) => new Web3(rpcurl).Eth.GetContract(abi ?? "", contractAddress);

        public static async Task<TransactionReceipt> DeployContract(string rpcurl, string bytecode, string account, string privateKey = null, string abi = null, HexBigInteger gasDeploy = default, object[] values = null)
        {
            Web3 web3;
            if (!string.IsNullOrEmpty(privateKey))
            {
                // Sign locally with the account's private key. Works against any JSON-RPC endpoint,
                // including hosted providers that don't expose the node-side personal_* API.
                var chainId = await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();
                var signer = new Nethereum.Web3.Accounts.Account(privateKey, chainId.Value);
                web3 = new Web3(signer, rpcurl);
                account = signer.Address;
            }
            else
            {
                // Fall back to a node-managed (unlocked) account, e.g. a local Ganache/Geth node.
                web3 = new Web3(rpcurl);
                if (!await web3.Personal.UnlockAccount.SendRequestAsync(account, "", new HexBigInteger(30)))
                {
                    throw new Exception("Could not unlock the account on the node. Provide the account's private key in the deploy profile, or deploy to a node that has the account unlocked.");
                }
            }

            if (gasDeploy == null)
            {
                gasDeploy = await web3.Eth.DeployContract.EstimateGasAsync(abi, bytecode, account, values);
            }
            return await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi, bytecode, account, gasDeploy, values: values);
        }

        public static async Task<string> CallContractAsync(string rpcurl, string contractAddress, string abi, string functionName, HexBigInteger gas = null, HexBigInteger value = null, params object[] functionInput)
        {
            var func = new Web3(rpcurl).Eth.GetContract(abi, contractAddress).GetFunction(functionName);
            // Decode the result against the function's output ABI. The bare CallAsync(CallInput) overload returns the
            // raw 32-byte hex of eth_call (e.g. "0x000...02a"), which displays as a meaningless value to the user.
            var outputs = await func.CallDecodingToDefaultAsync(functionInput);
            return outputs == null ? "" : string.Join(", ", outputs.Select(o => FormatCallResult(o.Result)));
        }

        // Renders a decoded ABI output value as a human-readable string. byte/bytesN values come back as byte[] and
        // are hex-encoded; everything else (BigInteger, bool, string, address) renders via its own ToString.
        private static string FormatCallResult(object value)
        {
            if (value is byte[] bytes) return "0x" + bytes.ToHex();
            return value?.ToString() ?? "";
        }

        public static async Task<string> SendContractTransactionAsync(string rpcurl, string contractAddress, string abi, string functionName, string fromAddress = null, string privateKey = null, HexBigInteger gas = null, HexBigInteger value = null, params object[] functionInput)
        {
            Web3 web3;
            if (!string.IsNullOrEmpty(privateKey))
            {
                // Sign locally with the account's private key. Works against any JSON-RPC endpoint,
                // including hosted providers that don't expose the node-side personal_* API.
                var chainId = await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();
                var signer = new Nethereum.Web3.Accounts.Account(privateKey, chainId.Value);
                web3 = new Web3(signer, rpcurl);
                fromAddress = signer.Address;
            }
            else
            {
                // Fall back to a node-managed (unlocked) account, e.g. a local Ganache/Geth node.
                web3 = new Web3(rpcurl);
                if (!await web3.Personal.UnlockAccount.SendRequestAsync(fromAddress, "", new HexBigInteger(30)))
                {
                    throw new Exception("Could not unlock the account on the node. Provide the account's private key, or use a node that has the account unlocked.");
                }
            }

            var func = web3.Eth.GetContract(abi, contractAddress).GetFunction(functionName);
            if (gas == null)
            {
                // Estimate against the actual sender (eth_estimateGas returns the minimum that just passes), then
                // pad by 50%. Some nodes (notably Ganache) revert/run out of gas at the bare estimate, which surfaces
                // as a generic "VM Exception ... revert".
                var estimate = await func.EstimateGasAsync(fromAddress, null, value, functionInput);
                gas = new HexBigInteger(estimate.Value * 3 / 2);
            }
            // Wait for the receipt so we can report real success/failure instead of just a tx hash. A reverted
            // transaction has Status 0; pull its on-chain reason so the caller sees *why* it failed.
            var receipt = await func.SendTransactionAndWaitForReceiptAsync(fromAddress, gas, value, CancellationToken.None, functionInput);
            if (receipt.HasErrors() == true)
            {
                string reason = null;
                try { reason = await web3.Eth.GetContractTransactionErrorReason.SendRequestAsync(receipt.TransactionHash); } catch { }
                throw new Exception($"Transaction {receipt.TransactionHash} reverted (gas used {receipt.GasUsed.Value})" + (string.IsNullOrEmpty(reason) ? "." : $": {reason}"));
            }
            return receipt.TransactionHash;
        }

        public static async Task<string> GetProtocolVersion(string rpcurl) => await new Web3(rpcurl).Eth.ProtocolVersion.SendRequestAsync();
        
        public static async Task<BigInteger> GetChainIdAsync(string rpcurl) => await new Web3(rpcurl).Eth.ChainId.SendRequestAsync();

        public static async Task<string> GetNetworkIdAsync(string rpcurl) => await new Web3(rpcurl).Net.Version.SendRequestAsync();

        public static async Task<(BigInteger, string, string[])> GetNetworkDetailsAsync(string rpcurl)
        {
            var web3 = new Web3(rpcurl);
            return (await web3.Eth.ChainId.SendRequestAsync(), await web3.Net.Version.SendRequestAsync(), await web3.Eth.Accounts.SendRequestAsync());
        }

        public static Task<HexBigInteger> GetBalance(string rpcurl, string address) => new Web3(rpcurl).Eth.GetBalance.SendRequestAsync(address);

        #endregion

        #region Fields
        public readonly string rpcUrl;
        public readonly BigInteger chainId;
        public readonly Web3 web3;
        #endregion
    }

}
