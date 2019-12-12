using System;
using System.Collections.Generic;
namespace CubusMVCTest.Models
{
    public class DashboardModel
    {
        public string id { get; set; }
        public string Error { get; set; }
        public AccountsData AccountsData { get; set; }
        public string Version {get;set;}
    }

    public class AccountsData
    {
        public string id { get; set; }
        public string status { get; set; }
        public string code { get; set; }
        public string reason { get; set; }
        public Accounts AccountResult { get; set; }
    }

    public class Accounts
    {
        public int userId { get; set; }
        public string firstName { get; set; }

        public string lastName { get; set; }

        public string emailAddr { get; set; }
        public string isReg { get; set; }
        public DateTime regDate { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public UserAccounts UserAccounts { get; set; }
    }

    public class UserAccounts
    {
        public AccountInfo[] AccountInfo { get; set; }
    }

    public class AccountInfo
    {
        public int userId { get; set; }
        public int acctId { get; set; }
        public int availBal { get; set; }
        public int currBal { get; set; }
    }
}