using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace SyZero.Client
{
    public class ResponseTemplate<T> 
    {
        public HttpStatusCode HttpStatusCode { set; get; }
        
        public SyMessageBoxStatus Code { set; get; }

        public string Msg { set; get; }

        public T Body { set; get; }

        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public void EnsureSuccessStatusCode()
        {
            if (this.HttpStatusCode < HttpStatusCode.OK || this.HttpStatusCode > (HttpStatusCode)299) throw new Exception("httpError");
        }
    }
}
