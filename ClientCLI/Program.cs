using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Contract;
using Contract.State;
using Contract.Transactions;
using Google.Protobuf;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement.Kdf;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Sawtooth.Sdk;
using Sawtooth.Sdk.Client;
using Encoder = Sawtooth.Sdk.Client.Encoder;

namespace ClientCLI
{
    class Program
    {
        private static string name;
        private static byte[] publicKey;
        private static byte[] privateKey;
        private static string publicKeyString;

        static async Task Main(string[] args)
        {
            Authenticate();

            while (true)
            {
                Console.WriteLine("1 - get all agreements");
                Console.WriteLine("2 - create new agreement");
                Console.WriteLine("3 - agree with something");
                Console.WriteLine("4 - relogin");

                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.D1:
                        await ShowAllAgreements();
                        break;
                    case ConsoleKey.D2:
                        CreateNewAgreement();
                        break;
                    case ConsoleKey.D3:
                        Agree();
                        break;
                    case ConsoleKey.D4:
                        Authenticate();
                        break;
                }
            }
        }

        private static void Agree()
        {
            Console.WriteLine("Enter agreement Id");
            var model = new AgreeModel()
            {
                Id = Console.ReadLine(),
                Username = name
            };

            var wrapper = new WrappingTransactionModel()
            {
                Agree = model
            };

            Send(wrapper);
        }

        private static void CreateNewAgreement()
        {
            var obj = new CreateAgreementModel()
            {
                Id = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                CreatorUsername = name
            };

            Console.WriteLine("Enter agreement text");
            string agreementText = Console.ReadLine();
            var agreementData = Encoding.UTF8.GetBytes(agreementText);

            Console.WriteLine("Please enter people names (finish with empty line)");
            while (true)
            {
                var userName = Console.ReadLine();
                if (string.IsNullOrEmpty(userName))
                    break;

                var pubKeyBytes = GetUserPublicKey(userName);
                var pubKey = BitConverter.ToString(pubKeyBytes).Replace("-", "").ToLower();
                obj.ParticipantsPublicKeys.Add(new UserModel()
                {
                    Name = userName,
                    PublicKey = pubKey
                });

                obj.Agreement.Add(new EncryptedAgreementModel()
                {
                    PublicKey = pubKey,
                    EncryptedText = Crypto.FullEncrypt(pubKeyBytes, privateKey, agreementData),
                });
            }

            var wrapper = new WrappingTransactionModel()
            {
                Create = obj
            };

            Send(wrapper);
        }

        private static byte[] GetUserPublicKey(string userName)
        {
            return new Signer(Encoding.UTF8.GetBytes(userName)).GetPublicKey();
        }

        private static async Task ShowAllAgreements()
        {
            string jsonResponse =
                await new HttpClient().GetStringAsync("http://localhost:8008/state?address=" + Globals.Prefix);
            RestResponse response = JsonConvert.DeserializeObject<RestResponse>(jsonResponse);

            foreach (var s in response.data)
            {
                var json = ByteString.FromBase64(s.data).ToStringUtf8();
                var jsonObj = JsonConvert.DeserializeObject(json);
                var agreement = JsonConvert.DeserializeObject<AgreementModel>(json);
                foreach (var myAgreement in agreement.Agreement.Where(a => a.PublicKey == publicKeyString))
                {
                    var text = Crypto.FullDecrypt(publicKey, privateKey, myAgreement.EncryptedText);

                    Console.WriteLine(Encoding.UTF8.GetString(text));
                }

                Console.WriteLine(jsonObj);
            }
        }

        private static void Authenticate()
        {
            Console.WriteLine("Please enter your name");

            name = Console.ReadLine();
            privateKey = new Signer(Encoding.UTF8.GetBytes(name)).GetPrivateKey();
            publicKey = new Signer(Encoding.UTF8.GetBytes(name)).GetPublicKey();
            publicKeyString = BitConverter.ToString(publicKey).Replace("-", "").ToLower();
        }

        private static void Send(WrappingTransactionModel wrapper)
        {
            var jsonString = JsonConvert.SerializeObject(wrapper);

            var payload = GetEncoder().EncodeSingleTransaction(Encoding.UTF8.GetBytes(jsonString));

            var content = new ByteArrayContent(payload);
            content.Headers.Add("Content-Type", "application/octet-stream");

            var httpClient = new HttpClient();

            var response = httpClient.PostAsync("http://localhost:8008/batches", content).Result;
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }

        private static Encoder GetEncoder()
        {
            var signer = new Signer(Encoding.UTF8.GetBytes(name));

            var settings = new EncoderSettings()
            {
                BatcherPublicKey = signer.GetPublicKey().ToHexString(),
                SignerPublickey = signer.GetPublicKey().ToHexString(),
                FamilyName = Globals.FamilyName,
                FamilyVersion = Globals.FamilyVersion
            };
            settings.Inputs.Add(Globals.Prefix);
            settings.Outputs.Add(Globals.Prefix);
            return new Encoder(settings, signer.GetPrivateKey());
        }
        
     
    }
}