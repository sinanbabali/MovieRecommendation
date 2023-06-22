using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Helpers
{
    public class EmailService
    {
        public class EmailData
        {
            public SenderInfo Sender { get; set; }
            public RecipientInfo[] To { get; set; }
            public string Subject { get; set; }
            public string HtmlContent { get; set; }
        }

        public class SenderInfo
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public class RecipientInfo
        {
            public string Email { get; set; }
        }
    }
}
