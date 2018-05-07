using MorningBank.Models.DomainModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MorningBank.BusinessLayer
{
    interface IBusinessBanking
    {
        decimal GetCheckingBalance(long checkingAccountNum);
        decimal GetSavingBalance(long savingAccountNum);
        long GetCheckingAccountNumForUser(string username);
        long GetSavingAccountNumForUser(string username);
        bool TransferCheckingToSaving(long checkingAccountNum, long savingAccountNum, decimal amount);
        bool TransferSavingToChecking(long checkingAccountNum, long savingAccountNum, decimal amount);
        List<TransactionHistoryModel> GetTransactionHistory(long checkingAccountNum);
    }
}
