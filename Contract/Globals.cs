using Sawtooth.Sdk;

namespace Contract
{
    public static class Globals
    {
        public const string FamilyName = "agreement";
        public const string FamilyVersion = "1.0";
        public static string Prefix => FamilyName.ToByteArray().ToSha512().ToHexString().Substring(0, 6);
    }
}