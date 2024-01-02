        using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRM_PCS_SERVICE.Module
{
    public class IvrRequest
    {
        public string conversationID { get; set; }

        public string time { get; set;}

        public string callId { get; set; }

        public string ani { get; set; }

        public string dnis { get; set; }

        public string callType { get; set; }

        public string routerCallKey { get; set;}

        public string routerCallKeyDay { get; set; }

        public string consent { get; set;}

        public string rating { get; set;}
    }
}
