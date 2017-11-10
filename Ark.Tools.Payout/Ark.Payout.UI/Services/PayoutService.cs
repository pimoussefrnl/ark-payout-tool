using Ark.Payout.UI.Helpers;
using Ark.Payout.UI.Models;
using ArkNet;
using ArkNet.Controller;
using ArkNet.Model;
using ArkNet.Model.Account;
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

        public static async Task<ArkClientIndexModel> GetClientsToPay(string passPhrase, double percentToPay, long amountToPay)
        {
            var returnModel = new ArkClientIndexModel();

            var delegateAccount = await GetAccount(passPhrase);
            if (delegateAccount == null)
                throw new Exception(StaticProperties.ARK_ACCOUNT_NOT_FOUND);

            var delegateAccountTotalArk = delegateAccount.Balance;
            if (amountToPay < delegateAccountTotalArk)
            {
                delegateAccountTotalArk = amountToPay;
            }
            var delegateAccountVoters = await DelegateService.GetVotersAsync(delegateAccount.PublicKey);
            var feesToPay = ArkNetApi.Instance.NetworkSettings.Fee.Send * delegateAccountVoters.Accounts.Count();
            var delegateAccountTotalArkToPay = (percentToPay / 100) * (delegateAccountTotalArk - feesToPay);
            var delegateAccountTotalArkVote = delegateAccountVoters.Accounts.Sum(x => x.Balance);

            var clientsToPay = new List<ArkClientModel>();
            foreach (var voter in delegateAccountVoters.Accounts)
            {
                var voterAccount = await AccountService.GetByAddressAsync(voter.Address);
                var amountToPayVoter = (voterAccount.Account.Balance / (double)delegateAccountTotalArkVote) * (delegateAccountTotalArkToPay);

                clientsToPay.Add(new ArkClientModel(voter.Address, amountToPayVoter, voterAccount.Account.Balance));
            }

            returnModel.ArkDelegateAccount = delegateAccount;
            returnModel.ArkClients = clientsToPay;
            return returnModel;
        }

        public static async Task<ErrorIndexModel> PayClient(ArkClientModel arkClientModel, string passPhrase, string paymentDescription)
        {
            return await PayClients(new List<ArkClientModel> { arkClientModel }, passPhrase, paymentDescription);
        }

        public static async Task<ErrorIndexModel> PayClients(List<ArkClientModel> arkClientModels, string passPhrase, string paymentDescription)
        {
            var returnModel = new ErrorIndexModel();
            var accCtnrl = new AccountController(passPhrase);

            foreach (var voter in arkClientModels)
            {
                var voterAccount = await AccountService.GetByAddressAsync(voter.Address);

                _log.Info(String.Format("Paying {0}({1}) to address {2}", (voter.AmountToBePaid / StaticProperties.ARK_DIVISOR), (long)voter.AmountToBePaid, voter.Address));
                var response = await accCtnrl.SendArkUsingMultiBroadCastAsync((long)voter.AmountToBePaid, voterAccount.Account.Address, string.IsNullOrWhiteSpace(paymentDescription) ? string.Empty : paymentDescription);

                if (response <= 0)
                {
                    _log.Error(String.Format("Error paying {0}({1}) to address {2}", (voter.AmountToBePaid / StaticProperties.ARK_DIVISOR), (long)voter.AmountToBePaid, voter.Address));
                    returnModel.ErrorClients.Add(new ErrorModel(voter.Address, (long)voter.AmountToBePaid, "transaction failed"));
                }
                else
                {
                    _log.Info(String.Format("Finished paying {0}({1}) to addres {2}", (voter.AmountToBePaid / StaticProperties.ARK_DIVISOR), (long)voter.AmountToBePaid, voter.Address));
                }
            }

            return returnModel;
        }

        public static async Task<ArkAccount> GetAccount(string passPhrase)
        {
            var accCtnrl = new AccountController(passPhrase);
            return await accCtnrl.GetArkAccountAsync();
        }
    }
}
