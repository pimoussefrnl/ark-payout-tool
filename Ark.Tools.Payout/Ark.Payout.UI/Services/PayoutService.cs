using Ark.Payout.UI.Helpers;
using Ark.Payout.UI.Models;
using ArkNet;
using ArkNet.Controller;
using ArkNet.Service;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ark.Payout.UI.Services
{
    public class PayoutService
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PayoutService));

        public static ArkClientIndexModel GetClientsToPay(string passPhrase, double percentToPay, Int64 amountToPay)
        {
            var returnModel = new ArkClientIndexModel();

            var accCtnrl = new AccountController(passPhrase);
            var delegateAccount = AccountService.GetByAddress(accCtnrl.GetArkAccount().Address);
            if (delegateAccount.Address == StaticProperties.ARK_ACCOUNT_NOT_FOUND)
                throw new Exception(StaticProperties.ARK_ACCOUNT_NOT_FOUND);

            var delegateAccountTotalArk = Convert.ToInt64(delegateAccount.Balance);
            if(amountToPay < delegateAccountTotalArk)
            {
                delegateAccountTotalArk = Convert.ToInt64(amountToPay);
            }
            var delegateAccountVoters = DelegateService.GetVoters(delegateAccount.PublicKey);
            var feesToPay = ArkNetApi.Instance.NetworkSettings.Fee.Send * delegateAccountVoters.Count();
            var delegateAccountTotalArkToPay = (percentToPay / 100) * (delegateAccountTotalArk - feesToPay);
            var delegateAccountTotalArkVote = delegateAccountVoters.Sum(x => Convert.ToInt64(x.Balance));

            var clientsToPay = new List<ArkClientModel>();
            foreach (var voter in delegateAccountVoters)
            {
                var voterAccount = AccountService.GetByAddress(voter.Address);
                var amountToPayVoter = (Convert.ToInt64(voterAccount.Balance) / (double)delegateAccountTotalArkVote) * (delegateAccountTotalArkToPay);

                clientsToPay.Add(new ArkClientModel(voter.Address, amountToPayVoter, Convert.ToInt64(voterAccount.Balance)));
            }

            returnModel.ArkDelegateAccount = delegateAccount;
            returnModel.ArkClients = clientsToPay;
            return returnModel;
        }

        public static ErrorIndexModel PayClients(List<ArkClientModel> arkClientModels, string passPhrase, string paymentDescription)
        {
            var returnModel = new ErrorIndexModel();
            var accCtnrl = new AccountController(passPhrase);

            foreach (var voter in arkClientModels)
            {
                var voterAccount = AccountService.GetByAddress(voter.Address);

                _log.Info(String.Format("Paying {0} to address {1}", (long)voter.AmountToBePaid, voter.Address));
                var response = accCtnrl.SendArk((long)voter.AmountToBePaid, voterAccount.Address, string.IsNullOrWhiteSpace(paymentDescription) ? string.Empty : paymentDescription, passPhrase);

                if (response.Item1 == false)
                {
                    _log.Error(String.Format("Error paying {0} to address {1}", (long)voter.AmountToBePaid, voter.Address));
                    returnModel.ErrorClients.Add(new ErrorModel(voter.Address, (long)voter.AmountToBePaid, response.Item3));
                }
                else
                {
                    _log.Info(String.Format("Finished paying {0} to addres {1}", (long)voter.AmountToBePaid, voter.Address));
                }
            }

            return returnModel;
        }
    }
}
