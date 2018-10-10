namespace Contract.State
{
    public class UserModel
    {
        public string Name;
        public string PublicKey;
    }

    public class EncryptedAgreementModel
    {
        public string PublicKey;
        public byte[] EncryptedText;
    }
    
    public class AgreementModel
    {
        public bool AllAreAgree { get; set; }
        public string CreatorPublicKey { get; set; }
        public byte[] CreatorPublicKeyBytes { get; set; }
        public UserModel[] Agreed { get; set; }
        public EncryptedAgreementModel[] Agreement { get; set; }
        public string Id { get; set; }
        public UserModel[] Participants { get; set; }
    }
}