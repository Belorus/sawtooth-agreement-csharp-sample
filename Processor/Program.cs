using System;
using System.Linq;
using Sawtooth.Sdk.Processor;

namespace Processor
{
    class Program
    {
        static void Main(string[] args)
        {
            var validatorAddress = args.Any() ? args.First() : "tcp://127.0.0.1:4004";

            var processor = new TransactionProcessor(validatorAddress);
            processor.AddHandler(new AgreementHandler());
            processor.Start();

            
            Console.CancelKeyPress += delegate { processor.Stop(); };
        }
    }
}