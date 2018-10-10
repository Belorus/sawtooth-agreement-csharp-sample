using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Contract;
using Contract.State;
using Contract.Transactions;
using Google.Protobuf;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Sawtooth.Sdk;
using Sawtooth.Sdk.Client;
using Sawtooth.Sdk.Processor;

namespace Processor
{
    public class AgreementHandler : ITransactionHandler
    {
        public string FamilyName => Globals.FamilyName;
        public string Version => Globals.FamilyVersion;

        public string[] Namespaces => new[] { Globals.Prefix };

        public async Task ApplyAsync(TpProcessRequest request, TransactionContext context)
        {
            Console.WriteLine("Processing transaction by: " + request.Header.SignerPublicKey);
            
            var jsonString = request.Payload.ToStringUtf8();
            var model = JsonConvert.DeserializeObject<WrappingTransactionModel>(jsonString);

            if (model.Create != null)
            {
                await ProcessCreate(model.Create, request.Header, context);
            }
            else if (model.Agree != null)
            {
                await ProcessAgree(model.Agree, request.Header, context);
            }
            else
            {
                throw new InvalidTransactionException("Unknown transaction payload");
            }
        }

        private async Task ProcessAgree(
            AgreeModel model, 
            TransactionHeader requestHeader, 
            TransactionContext context)
        {
            var addr = Globals.Prefix + model.Id.ToByteArray().ToSha512().TakeLast(32).ToArray().ToHexString();
            var state = await context.GetStateAsync(addr);

            var agreementModel = JsonConvert.DeserializeObject<AgreementModel>(state.ToStringUtf8());
            if (agreementModel == null)
                throw new InvalidTransactionException("Unknown agreement");
            
            if (agreementModel.Id != model.Id)
                throw new InvalidTransactionException("Invalid Id");
            
            if (agreementModel.Agreed.Select(u => u.PublicKey).Contains(requestHeader.SignerPublicKey))
                throw new InvalidTransactionException("Already agreed");

            if (!agreementModel.Participants.Select(u => u.PublicKey).Contains(requestHeader.SignerPublicKey))
                throw new InvalidTransactionException("You're not part of agreement");
            
            agreementModel.Agreed = agreementModel.Agreed.Append(new UserModel(){
                Name = model.Username,
                PublicKey = requestHeader.SignerPublicKey})
                .ToArray();

            if (agreementModel.Agreed.Length > agreementModel.Participants.Length)
                agreementModel.AllAreAgree = true;
            
            var jsonString = JsonConvert.SerializeObject(agreementModel);
            await context.SetStateAsync(addr, ByteString.CopyFrom(jsonString, Encoding.UTF8));
        }

        private async Task ProcessCreate(
            CreateAgreementModel model,
            TransactionHeader requestHeader,
            TransactionContext context)
        {
            var addr = Globals.Prefix + model.Id.ToByteArray().ToSha512().TakeLast(32).ToArray().ToHexString();

            var stateModel = new AgreementModel()
            {
                Id = model.Id,
                CreatorPublicKey = requestHeader.SignerPublicKey,
                Agreed = new[] { new UserModel() {  PublicKey = requestHeader.SignerPublicKey, Name = model.CreatorUsername}},
                Participants = model.ParticipantsPublicKeys.ToArray(),
                Agreement = model.Agreement.ToArray()
            };
            
            var jsonString = JsonConvert.SerializeObject(stateModel);
            await context.SetStateAsync(addr, ByteString.CopyFrom(jsonString, Encoding.UTF8));
        }

      
    }
}