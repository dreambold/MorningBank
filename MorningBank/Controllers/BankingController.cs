using MorningBank.BusinessLayer;
using MorningBank.Models.DomainModels;
using MorningBank.Models.ViewModels;
using MorningBank.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MorningBank.Controllers
{
    [Authorize]
    public class BankingController : Controller
    {
        public ActionResult TransferCheckingToSaving()
        {
            TransferCToSModel tcs = new TransferCToSModel();
            UserInfo ui = CookieFacade.USERINFO;
            IBusinessBanking ibank = GenericFactory<Business, IBusinessBanking>.GetInstance();
            tcs.CheckingBalance = ibank.GetCheckingBalance(ui.CheckingAcccountNumber);
            tcs.SavingBalance = ibank.GetSavingBalance(ui.SavingAccountNumber);
            tcs.Amount = 5;
            ViewBag.Message = "";
            return View(tcs);
        }
        [HttpPost]
        public ActionResult TransferCheckingToSaving(TransferCToSModel tcs)
        {
            IBusinessBanking ibank = GenericFactory<Business, IBusinessBanking>.GetInstance();
            UserInfo ui = CookieFacade.USERINFO;
            try
            {
                if (ModelState.IsValid)
                {
                    bool ret = ibank.TransferCheckingToSaving(ui.CheckingAcccountNumber, ui.SavingAccountNumber, tcs.Amount);
                    if (ret == true)
                    {
                        ViewBag.Success = true;
                        ViewBag.Message = "Transfer successful..";
                        ModelState.Clear(); // otherwise, textbox will display the old amount
                        tcs.Amount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Success = false;
                ViewBag.Message = ex.Message;
            }
            tcs.CheckingBalance = ibank.GetCheckingBalance(ui.CheckingAcccountNumber);
            tcs.SavingBalance = ibank.GetSavingBalance(ui.SavingAccountNumber);
            return View(tcs);
        }
        public ActionResult TransferHistory()
        {
            IBusinessBanking ibank = GenericFactory<Business, IBusinessBanking>.GetInstance();
            UserInfo ui = CookieFacade.USERINFO;
            List<Models.DomainModels.TransactionHistoryModel> THList = ibank.GetTransactionHistory(ui.CheckingAcccountNumber);
            Models.ViewModels.TransactionHistoryModel thm = new Models.ViewModels.TransactionHistoryModel();

            return View(THList);
        }
    }
}