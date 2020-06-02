using System;

namespace TWETTY_WEBSERVER
{
    public class MessageApiModel
    {
        public string SendBy_Email { get; set; }

        public string SendTo_Email { get; set; }

        public string Message { get; set; }
        
        public DateTimeOffset MessageSentTime { get; set; }
    }
}
