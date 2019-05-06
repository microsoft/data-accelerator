using System;
using System.Collections.Generic;
using System.Text;

namespace DataX.ServiceHost.AspNetCore.Authorization.Roles
{
    public class DataXWriterAttribute : DataXAuthorizeAttribute
    {
        public DataXWriterAttribute()
        {
            Policy = DataXAuthConstants.WriterPolicyName;
        }
    }
}
