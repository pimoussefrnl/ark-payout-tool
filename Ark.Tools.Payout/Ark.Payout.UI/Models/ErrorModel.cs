using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ark.Payout.UI.Models
{
    public class ErrorIndexModel
    {
        public List<ErrorModel> ErrorClients { get; set; }

        public ErrorIndexModel()
        {
            ErrorClients = new List<ErrorModel>();
        }
    }
    public class ErrorModel
    {
        public string Address { get; set; }
        public long AmountToBePaid { get; set; }
        public string ErrorMessage { get; set; }

        public ErrorModel(string address, long amountToBePaid, string errorMessage)
        {
            Address = address;
            AmountToBePaid = amountToBePaid;
            ErrorMessage = errorMessage;
        }
    }
}
