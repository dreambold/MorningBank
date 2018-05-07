using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using MorningBank.Cache;
using MorningBank.Models.DomainModels;

namespace MorningBank.DataLayer
{
    public class Repository : IRepositoryAuthentication, IRepositoryBanking
    {
        // Repository needs to communicate with DataAcess sublayer
        // We should use loose coupling so that we can can
        // use DependencyInjection to test each sublayer
        IDataAccess _idac = null;
        public Repository(IDataAccess idac)
        {
            _idac = idac;
        }
        public Repository() : this(new SQLDataAccess())
        { }
        public bool CheckIfValidUser(string username, string password)
        {
            bool ret = false;
            try
            {
                string sql = "select Username from Users where " +
                "Username=@Username and Password=@Password";
                List<DbParameter> PList = new List<DbParameter>();
                DbParameter p1 = new SqlParameter("@Username", SqlDbType.VarChar, 50);
                p1.Value = username;
                PList.Add(p1);
                DbParameter p2 = new SqlParameter("@Password", SqlDbType.VarChar, 50);
                p2.Value = password;
                PList.Add(p2);
                object obj = _idac.GetSingleAnswer(sql, PList);
                if (obj != null)
                    ret = true;
            }
            catch (Exception)
            {
                throw;
            }
            return ret;
        }
        public string GetRolesForUser(string username)
        {
            string ret = "";
            try
            {
                string sql = "select r.RoleName from Roles r inner join UserRoles ur on "
               + "r.RoleId=ur.RoleId inner join Users u on ur.Username=u.Username where " +
               "u.Username=@Username";
                List<DbParameter> PList = new List<DbParameter>();
                DbParameter p1 = new SqlParameter("@Username", SqlDbType.VarChar, 50);
                p1.Value = username;
                PList.Add(p1);
                DataTable dt = _idac.GetManyRowsCols(sql, PList);
                // convert the roles to a pipe delimited string
                string roles = "";
                foreach (DataRow dr in dt.Rows)
                {
                    roles += dr["RoleName"] + "|";
                }
                if (roles.Length > 0)
                    roles = roles.Substring(0, roles.Length - 1); // remove last |
                ret = roles;
            }
            catch (Exception)
            {
                throw;
            }
            return ret;
        }
        public bool ChangePassword(string username, string oldPassword, string
       newPassword)
        {
            throw new NotImplementedException();
        }
        public decimal GetCheckingBalance(long checkingAccountNum)
        {
            decimal balance = 0;
            try
            {
                string sql = "select balance from CheckingAccounts where CheckingAccountNumber = @CheckingAccountNumber";
                List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@CheckingAccountNumber", SqlDbType.BigInt);
                p1.Value = checkingAccountNum;
                ParamList.Add(p1);
                object obj = _idac.GetSingleAnswer(sql, ParamList);
                if (obj != null)
                    balance = decimal.Parse(obj.ToString());
            }
            catch (Exception)
            {
                throw;
            }
            return balance; ;
        }
        public decimal GetSavingBalance(long savingAccountNum)
        {
            decimal balance = 0;
            try
            {
                string sql = "select balance from SavingAccounts where SavingAccountNumber = @SavingAccountNumber";
            List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@SavingAccountNumber", SqlDbType.BigInt);
                p1.Value = savingAccountNum;
                ParamList.Add(p1);
                object obj = _idac.GetSingleAnswer(sql, ParamList);
                if (obj != null)
                    balance = decimal.Parse(obj.ToString());
            }
            catch (Exception)
            {
                throw;
            }
            return balance; ;
        }
        public long GetCheckingAccountNumForUser(string username)
        {
            long checkingAccountNum = 0;
            try
            {
                string sql = "select CheckingAccountNumber from CheckingAccounts where " 
                    + "Username=@Username";
                List<DbParameter> PList = new List<DbParameter>();
                DbParameter p1 = new SqlParameter("@Username", SqlDbType.VarChar, 50);
                p1.Value = username;
                PList.Add(p1);
                object obj = _idac.GetSingleAnswer(sql, PList);
                if (obj != null)
                    checkingAccountNum = long.Parse(obj.ToString());
            }
            catch (Exception)
            {
                throw;
            }
            return checkingAccountNum;
        }
        public long GetSavingAccountNumForUser(string username)
        {
            long savingAccountNum = 0;
            try
            {
                string sql = "select SavingAccountNumber from SavingAccounts where " +
                    "Username=@Username";
                List<DbParameter> PList = new List<DbParameter>();
                DbParameter p1 = new SqlParameter("@Username", SqlDbType.VarChar, 50);
                p1.Value = username;
                PList.Add(p1);
                object obj = _idac.GetSingleAnswer(sql, PList);
                if (obj != null)
                    savingAccountNum = long.Parse(obj.ToString());
            }
            catch (Exception)
            {
                throw;
            }
            return savingAccountNum;
        }
        public bool TransferCheckingToSaving(long checkingAccountNum, long savingAccountNum, decimal amount, decimal transactionFee)
        {
            // transfer checking to saving has to be done as a transaction
            // transactions are assocated with a connection
            bool ret = false;
            string CONNSTR = ConfigurationManager.ConnectionStrings["MYBANK"].ConnectionString;
            SqlConnection conn = new SqlConnection(CONNSTR);
            SqlTransaction sqtr = null;
            try
            {
                conn.Open();
                sqtr = conn.BeginTransaction();
                int rows = UpdateCheckingBalanceTR(checkingAccountNum, -1 * amount, conn, sqtr, true);
                if (rows == 0)
                    throw new Exception("Problem in transferring from Checking Account..");


                object obj = GetCheckingBalanceTR(checkingAccountNum, conn, sqtr, true);
                if (obj != null)
                {
                    if (decimal.Parse(obj.ToString()) < 0) // exception causes transaction to be rolled back
                        throw new Exception("Insufficient funds in Checking Account - rolling back transaction");
                }
                rows = UpdateSavingBalanceTR(savingAccountNum, amount, conn, sqtr, true);
                if (rows == 0)
                    throw new Exception("Problem in transferring to Saving Account..");
                rows = AddToTransactionHistoryTR(checkingAccountNum, savingAccountNum, amount, 100, transactionFee, conn, sqtr, true);
                if (rows == 0)
                    throw new Exception("Problem in transferring to Saving Account..");
                else
                {
                    sqtr.Commit();
                    ret = true;
                    // clear the cache
                    CacheAbstraction cabs = new CacheAbstraction();
                    cabs.Remove("TRHISTORY");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                conn.Close();
            }
            return ret;
        }
        public bool TransferSavingToChecking(long checkingAccountNum, long
       savingAccountNum, decimal amount, decimal transactionFee)
        {
            throw new NotImplementedException();
        }
        private int UpdateCheckingBalanceTR(long checkingAccountNum, decimal amount,
       DbConnection conn, DbTransaction sqtr, bool doTransaction)
        {
            int rows = 0;
            try
            {
                string sql1 = "Update CheckingAccounts set Balance=Balance+@Amount where CheckingAccountNumber = @CheckingAccountNumber";
            List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@CheckingAccountNumber", SqlDbType.BigInt);
                p1.Value = checkingAccountNum;
                ParamList.Add(p1);
                SqlParameter p2 = new SqlParameter("@Amount", SqlDbType.Decimal);
                p2.Value = amount;
                ParamList.Add(p2);
                rows = _idac.InsertUpdateDelete(sql1, ParamList, conn, sqtr,
               doTransaction); // part of transaction
            }
            catch (Exception)
            {
                throw;
            }
            return rows;
        }
        private int UpdateSavingBalanceTR(long savingAccountNum, decimal amount,
       DbConnection conn, DbTransaction sqtr, bool doTransaction)
        {
            int rows = 0;
            try
            {
                string sql1 = "Update SavingAccounts set Balance=Balance+@Amount where SavingAccountNumber = @SavingAccountNumber";
            List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@SavingAccountNumber", SqlDbType.BigInt);
                p1.Value = savingAccountNum;
                ParamList.Add(p1);
                SqlParameter p2 = new SqlParameter("@Amount", SqlDbType.Decimal);
                p2.Value = amount;
                ParamList.Add(p2);
                rows = _idac.InsertUpdateDelete(sql1, ParamList, conn, sqtr,
               doTransaction); // part of transaction
            }
            catch (Exception)
            {
                throw;
            }
            return rows;
        }
        private object GetCheckingBalanceTR(long checkingAccountNum, DbConnection conn,
       DbTransaction sqtr, bool doTransaction)
        {
            object objBal = null;
            try
            {
                string sql2 = "select Balance from CheckingAccounts where CheckingAccountNumber = @CheckingAccountNumber";
            List<DbParameter> ParamList2 = new List<DbParameter>();
                SqlParameter pa = new SqlParameter("@CheckingAccountNumber", SqlDbType.BigInt);
                pa.Value = checkingAccountNum;
                ParamList2.Add(pa);
                objBal = _idac.GetSingleAnswer(sql2, ParamList2, conn, sqtr,
               doTransaction);
            }
            catch (Exception)
            {
                throw;
            }
            return objBal;
        }
        private int AddToTransactionHistoryTR(long checkingAccountNum, long
       savingAccountNum,
        decimal amount, int transTypeId, decimal transFee,
        DbConnection conn, DbTransaction sqtr, bool doTransaction)
        {
            int rows = 0;
            try
            {
                string sql1 = "insert into TransactionHistories(CheckingAccountNumber, SavingAccountNumber, " 
                    + "Amount,TransactionFee,TransactionTypeId) values (@CheckingAccountNumber, @SavingAccountNumber, " 
                    +  "@Amount,@TransactionFee,@TransactionTypeId)";
                List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@CheckingAccountNumber", SqlDbType.BigInt);
                p1.Value = checkingAccountNum;
                ParamList.Add(p1);
                SqlParameter p2 = new SqlParameter("@SavingAccountNumber", SqlDbType.BigInt);
                p2.Value = savingAccountNum;
                ParamList.Add(p2);
                SqlParameter p3 = new SqlParameter("@Amount", SqlDbType.Decimal);
                p3.Value = amount;
                ParamList.Add(p3);
                SqlParameter p4 = new SqlParameter("@TransactionFee", SqlDbType.Decimal);
                p4.Value = transFee;
                ParamList.Add(p4);
                SqlParameter p5 = new SqlParameter("@TransactionTypeId", SqlDbType.Int);
                p5.Value = transTypeId;
                ParamList.Add(p5);
                rows = _idac.InsertUpdateDelete(sql1, ParamList, conn, sqtr,
               doTransaction); // part of transaction
            }
            catch (Exception)
            {
                throw;
            }
            return rows;
        }

        public List<TransactionHistoryModel> GetTransactionHistory(long checkingAccountNum)
        {
            List<TransactionHistoryModel> THList = null;
            try
            {
                CacheAbstraction cabs = new CacheAbstraction();
                THList = cabs.Retrieve<List<TransactionHistoryModel>>("TRHISTORY" + ":" + checkingAccountNum);
                if (THList != null)
                    return THList;
                string sql = "select th.*, trt.TransactionTypeName from TransactionHistories th " +
            "inner join TransactionTypes trt on th.TransactionTypeId = trt.TransactionTypeId " +
            "where CheckingAccountNumber=@CheckingAccountNumber";
                List<DbParameter> ParamList = new List<DbParameter>();
                SqlParameter p1 = new SqlParameter("@CheckingAccountNumber", SqlDbType.BigInt);
                p1.Value = checkingAccountNum;
                ParamList.Add(p1);
                DataTable dt = _idac.GetManyRowsCols(sql, ParamList);
                //string amt = dt.Rows[0]["Amount"].ToString();
                THList = DBList.ToList<TransactionHistoryModel>(dt);
                cabs.Insert("TRHISTORY" + ":" + checkingAccountNum, THList);
            }
            catch (Exception)
            {
                throw;
            }
            return THList;
        }
    }
}