using System;
using System.Collections.Generic;
using System.Text;

namespace SPListWithSQLCLR
{
    public class ReturnResult
    {
        string returnMessage;
        bool isSucceed;

        public string ReturnMessage
        {
            get
            {
                return returnMessage;
            }
            set
            {
                returnMessage = value;
            }
        }

        public bool IsSucceed
        {
            get
            {
                return isSucceed;
            }
            set
            {
                isSucceed = value;
            }
        }
    }
}
