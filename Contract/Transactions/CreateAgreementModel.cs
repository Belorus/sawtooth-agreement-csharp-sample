using System.Collections.Generic;
using Contract.State;

namespace Contract.Transactions
{
    public class CreateAgreementModel
    {
        public List<UserModel> ParticipantsPublicKeys = new List<UserModel>();

        public List<EncryptedAgreementModel> Agreement = new List<EncryptedAgreementModel>();

        public string CreatorUsername;
        
        public string Id;
    }
}