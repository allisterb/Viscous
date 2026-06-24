using System;
using System.Linq;

using Viscous.Ethereum.Explorers;

namespace Viscous
{
    public static class Extensions
    {
        public static bool IsNetworkError<T>(this Result<T> result)
        {
            if (result.Exception is null)
            {
                return false;
            }
            else
            {
                var n = result.Exception.GetType().Name;
                return n.Contains("RpcClient") || n.Contains(("Http"));
            }
        }  
    }
}
