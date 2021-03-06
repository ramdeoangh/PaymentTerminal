﻿using PaymentServiceLib.Enum;
using PaymentServiceLib.Model.Request;
using PaymentServiceLib.PinPadPOS.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentServiceLib.Model.Response
{

    /// <summary>A PaymentTerminal terminal query card response object.</summary>
    public class EFTQueryCardResponse : TerminalBaseResponse
    {
        /// <summary>Constructs a default terminal logon response object.</summary>
        public EFTQueryCardResponse()
            : base(typeof(EFTQueryCardRequest))
        {
        }

        /// <summary>Two digit merchant code</summary>
        /// <value>Type: <see cref="string"/><para>The default is "00"</para></value>
        public string Merchant { get; set; } = "00";

        /// <summary>Flags indicating tracks present in the response.</summary>
        /// <value>Type: <see cref="TrackFlags" /></value>
        public TrackFlags TrackFlags { get; set; }

        /// <summary>Data encoded on Track1 of the card.</summary>
        /// <value>Type: <see cref="System.String" /></value>
        public string Track1 { get; set; } = "";

        /// <summary>Data encoded on Track2 of the card.</summary>
        /// <value>Type: <see cref="System.String" /></value>
        public string Track2 { get; set; } = "";

        /// <summary>Data encoded on Track3 of the card.</summary>
        /// <value>Type: <see cref="System.String" /></value>
        public string Track3 { get; set; } = "";

        /// <summary>BIN number of the card swiped.</summary>
        /// <value>Type: <see cref="System.Int32" /></value>
        /// <remarks><list type="table">
        /// <listheader><term>Card BIN</term><description>Card Type</description></listheader>
        ///	<item><term>0</term><description>Unknown</description></item>
        ///	<item><term>1</term><description>Debit</description></item>
        ///	<item><term>2</term><description>Bankcard</description></item>
        ///	<item><term>3</term><description>Mastercard</description></item>
        ///	<item><term>4</term><description>Visa</description></item>
        ///	<item><term>5</term><description>American Express</description></item>
        ///	<item><term>6</term><description>Diner Club</description></item>
        ///	<item><term>7</term><description>JCB</description></item>
        ///	<item><term>8</term><description>Label Card</description></item>
        ///	<item><term>9</term><description>JCB</description></item>
        ///	<item><term>11</term><description>JCB</description></item>
        ///	<item><term>12</term><description>Other</description></item></list>
        ///	</remarks>
        public int CardName { get; set; } = 0;

        /// <summary>BIN number of the card swiped.</summary>
        /// <value>Type: <see cref="System.Int32" /></value>
        /// <remarks><list type="table">
        /// <listheader><term>Card BIN</term><description>Card Type</description></listheader>
        ///	<item><term>0</term><description>Unknown</description></item>
        ///	<item><term>1</term><description>Debit</description></item>
        ///	<item><term>2</term><description>Bankcard</description></item>
        ///	<item><term>3</term><description>Mastercard</description></item>
        ///	<item><term>4</term><description>Visa</description></item>
        ///	<item><term>5</term><description>American Express</description></item>
        ///	<item><term>6</term><description>Diner Club</description></item>
        ///	<item><term>7</term><description>JCB</description></item>
        ///	<item><term>8</term><description>Label Card</description></item>
        ///	<item><term>9</term><description>JCB</description></item>
        ///	<item><term>11</term><description>JCB</description></item>
        ///	<item><term>12</term><description>Other</description></item></list>
        ///	</remarks>
        [System.Obsolete("Please use CardName instead of CardBIN")]
        public int CardBin { get { return CardName; } set { CardName = value; } }

        /// <summary>Account type selected.</summary>
        /// <value>Type: <see cref="AccountType" /></value>
        public AccountType AccountType { get; set; }

        /// <summary>Indicates if the request was successful.</summary>
        /// <value>Type: <see cref="System.Boolean"/></value>
        public bool Success { get; set; } = false;

        /// <summary>The response code of the request.</summary>
        /// <value>Type: <see cref="System.String"/><para>A 2 character response code. "00" indicates a successful response.</para></value>
        public string ResponseCode { get; set; } = "";

        /// <summary>The response text for the response code.</summary>
        /// <value>Type: <see cref="System.String"/></value>
        public string ResponseText { get; set; } = "";

        /// <summary>Additional information sent with the response.</summary>
        /// <value>Type: <see cref="PadField"/></value>
        public PadField PurchaseAnalysisData { get; set; } = new PadField();
    }
}
