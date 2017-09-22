using Ark.Payout.UI.Helpers;
using ArkNet;
using ArkNet.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Payout.UI.Models
{
    public class ArkClientIndexModel
    {
        public double ArkDelegateAccountBalance
        {
            get { return Int64.Parse(ArkDelegateAccount.Balance); }
        }
        public double ArkDelegateAccountBalanceUI
        {
            get { return ArkDelegateAccountBalance / StaticProperties.ARK_DIVISOR; }
        }
        public double FeesToPay
        {
            get { return ArkNetApi.Instance.NetworkSettings.Fee.Send * ArkClients.Count(); }
        }
        public double FeesToPayUI
        {
            get { return FeesToPay / StaticProperties.ARK_DIVISOR; }
        }
        public double TotalArkToPayUI
        {
            get { return (ArkClients.Sum(x => x.AmountToBePaid) + FeesToPay) / StaticProperties.ARK_DIVISOR; }
        }
        public int TotalClientsToPay
        {
            get { return ArkClients.Count(); }
        }

        public ArkAccount ArkDelegateAccount { get; set; }
        public List<ArkClientModel> ArkClients { get; set; }
    }
    public class ArkClientModel
    {
        public string Address { get; set; }
        public double AmountToBePaid { get; set; }

        public double AmountToBePaidUI
        {
            get { return AmountToBePaid / StaticProperties.ARK_DIVISOR; }
        }

        public double Balance { get; set; }

        public double BalanceUI
        {
            get { return Balance / StaticProperties.ARK_DIVISOR; }
        }

        public ArkClientModel(string address, double amountToBePaid, double balance)
        {
            this.Address = address;
            this.AmountToBePaid = amountToBePaid;
            this.Balance = balance;
        }
    }
}
