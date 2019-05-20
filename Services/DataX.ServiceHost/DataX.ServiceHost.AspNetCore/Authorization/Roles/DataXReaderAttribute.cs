using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization.Roles
{
    /// <inheritdoc />
    public class DataXReaderAttribute : DataXAuthorizeAttribute
    {
        public DataXReaderAttribute()
        {
            Policy = DataXAuthConstants.ReaderPolicyName;
        }
    }
}
