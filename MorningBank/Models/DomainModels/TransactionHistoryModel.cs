using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MorningBank.Models.DomainModels
{
    public partial class TransactionHistoryModel: EntityBase
    {
        public System.DateTime TransactionDate { get; set; }

        public long? CheckingAccountNumber { get; set; }

        public long? SavingAccountNumber { get; set; }

        public decimal Amount { get; set; }

        public decimal? TransactionFee { get; set; }

        public string TransactionTypeName { get; set; }
    }
}