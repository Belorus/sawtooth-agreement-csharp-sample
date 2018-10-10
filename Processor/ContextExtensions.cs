using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Sawtooth.Sdk.Processor;

namespace Processor
{
    public static class ContextExtensions
    {
        public static async Task<string> SetStateAsync(this TransactionContext context, string address, ByteString data)
        {
            var map = new Dictionary<string, ByteString>();
            map[address] = data;
            var result = await context.SetStateAsync(map);
            return result.Single();
        }

        public static async Task<ByteString> GetStateAsync(this TransactionContext context, string address)
        {
            var map = await context.GetStateAsync(new[] {address});
            var byteString = map.FirstOrDefault();

            return byteString.Value;
        }
    }
}