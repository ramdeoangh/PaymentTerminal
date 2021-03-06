﻿using System;

namespace PaymentServiceLib.PinPadPOS.Net
{
    public class ConnectionException : Exception
    {
        public ConnectionException()
        {
        }

        public ConnectionException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public ConnectionException(string message)
            : base(message)
        {
        }
    }
}
