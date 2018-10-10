using System;
using System.Globalization;
using Google.Protobuf;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement.Kdf;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;

namespace ClientCLI
{
    public static class Crypto
    {
        readonly static X9ECParameters Secp256k1 = ECNamedCurveTable.GetByName("secp256k1");
        readonly static ECDomainParameters DomainParams = new ECDomainParameters(Secp256k1.Curve, Secp256k1.G, Secp256k1.N, Secp256k1.H);

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data; 
        }

        public static byte[] FullEncrypt(byte[] publicKey, byte[] privateKeyBytes, byte[] data)
        {
            var secret = GetSharedSecret(publicKey, privateKeyBytes);
            var symKey = DeriveSymmetricKeyFromSharedSecret(secret);

            return Encrypt(data, symKey);
        }
        
        public static byte[] FullDecrypt(byte[] publicKey, byte[] privateKeyBytes, byte[] data)
        {
            var secret = GetSharedSecret(publicKey, privateKeyBytes);
            var symKey = DeriveSymmetricKeyFromSharedSecret(secret);

            return Decrypt(data, symKey);
        }
        
        public static byte[] GetSharedSecret(byte[] publicKey, byte[] privateKeyBytes)
        {
            ECCurve curve = DomainParams.Curve;
            ECPoint q = curve.DecodePoint(publicKey);
            ECPublicKeyParameters oEcPublicKeyParameters = new ECPublicKeyParameters(q, DomainParams);
            ECPrivateKeyParameters privateKey = new ECPrivateKeyParameters(new BigInteger(privateKeyBytes), DomainParams);
            IBasicAgreement aKeyAgree = AgreementUtilities.GetBasicAgreement("ECDH");
            aKeyAgree.Init(privateKey);
            BigInteger sharedSecret = aKeyAgree.CalculateAgreement(oEcPublicKeyParameters);

            return sharedSecret.ToByteArray();
        }
        
        public static byte[] DeriveSymmetricKeyFromSharedSecret(byte[] sharedSecret)
        {
            ECDHKekGenerator egH = new ECDHKekGenerator(DigestUtilities.GetDigest("SHA256"));
            egH.Init(new DHKdfParameters(NistObjectIdentifiers.Aes,sharedSecret.Length,sharedSecret));
            byte[] symmetricKey = new byte[ DigestUtilities.GetDigest("SHA256").GetDigestSize()];
            egH.GenerateBytes(symmetricKey, 0,symmetricKey.Length);   

            return symmetricKey;
        }

        public static byte[] Encrypt(byte[] dataToEncrypt, byte[] symmetricKey)
        {
            byte[] output = null;
            try
            {
                KeyParameter keyparam = ParameterUtilities.CreateKeyParameter("DES", symmetricKey);
                IBufferedCipher cipher = CipherUtilities.GetCipher("DES/ECB/ISO7816_4PADDING");
                cipher.Init(true, keyparam);
                try
                {
                    output = cipher.DoFinal(dataToEncrypt);
                    return output;
                }
                catch (System.Exception ex)
                {
                    throw new CryptoException("Invalid Data");
                }
            }
            catch (Exception ex)
            {
            }

            return output;
        }
        
        public static byte[] Decrypt(byte[] cipherData, byte[] derivedKey)
        {
            byte[] output=null;
            try
            {
                KeyParameter keyparam = ParameterUtilities.CreateKeyParameter("DES", derivedKey);
                IBufferedCipher cipher = CipherUtilities.GetCipher("DES/ECB/ISO7816_4PADDING");
                cipher.Init(false, keyparam);
                try
                {
                    output = cipher.DoFinal(cipherData);

                }
                catch (System.Exception ex)
                {
                    throw new CryptoException("Invalid Data");
                }
            }
            catch (Exception ex)
            {
            }

            return output;
        }
    }
}