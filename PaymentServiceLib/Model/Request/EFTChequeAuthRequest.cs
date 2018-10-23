﻿using PaymentServiceLib.Enum;
using PaymentServiceLib.Model.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentServiceLib.Model.Request
{
    /// <summary>A PaymentTerminal cheque authorization request object.</summary>
    public class EFTChequeAuthRequest : TerminalBaseRequest
    {
        /// <summary>Constructs a default ChequeAuthRequest object.</summary>
        public EFTChequeAuthRequest() : base(true, typeof(EFTChequeAuthResponse))
        {
        }

        /// <summary>Two digit merchant code</summary>
        /// <value>Type: <see cref="string"/><para>The default is "00"</para></value>
        public string Merchant { get; set; } = "00";

        /// <summary>The branch code of the cheque.</summary>
        /// <value>Type: <see cref="System.String"/><para>A 6-digit branch code (BSB). This is a required property for a cheque authorization request.</para></value>
        public string BranchCode { get; set; } = "";

        /// <summary>The account number of the cheque.</summary>
        /// <value>Type: <see cref="System.String"/><para>A 14-digit account number. This is a required property for a cheque authorization request.</para></value>
        public string AccountNumber { get; set; } = "";

        /// <summary>The serial number on the cheque.</summary>
        /// <value>Type: <see cref="System.String"/><para>A 14-digit serial number. This is a required property for a cheque authorization request.</para></value>
        public string SerialNumber { get; set; } = "";

        /// <summary>The cheque amount to authorize.</summary>
        /// <value>Type: <see cref="System.Decimal"/><para>This is a required property for a cheque authorization request.</para></value>
        public decimal Amount { get; set; } = 0m;

        /// <summary>Indicates if the type of cheque authorization to perform.</summary>
        /// <value>Type: <see cref="ChequeType"/><para>The default is <see cref="ChequeType.PersonalGuarantee"/>.</para></value>
        public ChequeType ChequeType { get; set; } = ChequeType.BusinessGuarantee;

        /// <summary>The reference number attached to the authorization.</summary>
        /// <value>Type: <see cref="System.String"/><para>Maximum 12 digit reference number.</para></value>
        public string ReferenceNumber { get; set; } = "";

        /// <summary>Indicates where the request is to be sent to. Should normally be PaymentTerminal.</summary>
        /// <value>Type: <see cref="TerminalApplication"/><para>The default is <see cref="TerminalApplication.EFTPOS"/>.</para></value>
        public TerminalApplication Application { get; set; } = TerminalApplication.EFTPOS;
    }
}
