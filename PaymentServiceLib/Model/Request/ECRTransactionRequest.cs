﻿using PaymentServiceLib.Enum;

namespace PaymentServiceLib.Model.Request
{
    public class ECRTransactionRequest : TerminalBaseRequest
    {
        /// <summary>Constructs a default EFTTransactionRequest object.</summary>
		public ECRTransactionRequest() : base(true, typeof(ECRTransactionResponse))
        {
        }

        /// <summary>The type of transaction to perform.</summary>
        /// <value>Type: <see cref="TransactionType"/><para>The default is <see cref="TransactionType.PurchaseCash"></see></para></value>
        public TransactionType TxnType { get; set; } = TransactionType.Purchase;

        /// <summary>The purchase amount for the transaction.</summary>
        /// <value>Type: <see cref="System.Decimal"/><para>The default is 0.</para></value>
        /// <remarks>This property is mandatory for all but <see cref="TransactionType.CashOut"></see> transaction types.</remarks>
        public decimal AmtPurchase { get; set; } = 0;


        /// <summary>Tender Type</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public TenderType tenderType { get; set; } = TenderType.Credit;

        ///// <summary>The cash amount for the transaction.</summary>
        ///// <value>Type: <see cref="System.Decimal"/><para>The default is 0.</para></value>
        ///// <remarks>This property is mandatory for a <see cref="TransactionType.CashOut"></see> transaction type.</remarks>
        //public decimal AmtCash { get; set; } = 0;


        /// <summary>Two digit merchant code/ Clerk ID</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public string Merchant { get; set; }


        /// <summary>AlphaNumeric Invoice Number</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public string Invoice { get; set; }

        /// <summary>AlphaNumeric CustomerReference Number</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public string CustomerReference { get; set; }

        /// <summary>AlphaNumeric Optional authorization code</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public string AuthCode { get; set; }

        /// <summary>Optional Reference # generated by terminal</summary>
        /// <value>Type: <see cref="string"/><para></para></value>
        public string Reference { get; set; }

        /// <summary>Optional last 4 digits of account #</summary>      
        /// <value>Type: <see cref="System.String"/></value>
        /// <remarks>Use this property in conjunction with <see cref="PanSource"></see>.</remarks>
        public string Pan { get; set; } = "";

        /// <summary>Indicates whether to trigger receipt events.</summary>
        /// <value>Type: <see cref="ReceiptPrintModeType"/><para>The default is POSPrinter.</para></value>
        public ReceiptPrintModeType ReceiptAutoPrint { get; set; } = ReceiptPrintModeType.PinpadPrinter;

    }
}