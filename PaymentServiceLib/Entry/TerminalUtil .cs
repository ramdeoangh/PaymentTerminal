﻿using DependenyInjector;
using NLog;
using PaymentServiceLib.Enum;
using PaymentServiceLib.Model;
using PaymentServiceLib.Model.Request;
using PaymentServiceLib.Model.Response;
using PaymentServiceLib.PinPadPOS.Net;
using PaymentServiceLib.PinPadPOS.Slave;
using PaymentServiceLib.PinPadPOS.Utility;
using PaymentServiceLib.Util;
using PPaymentServiceLib.PinPadPOS.Net;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PaymentServiceLib.Entry
{
   

    #region SocketEventArgs

  

    /// <summary>EFT Client IP event object. Sent when an event occurs.</summary>
    /// <remarks>Note that only one response property is valid per event type.</remarks>
    public class SocketEventArgs : EventArgs
    {
        /// <summary>The error message describing the error that occurred.</summary>
        /// <value>Type: <see cref="System.String" /></value>
        /// <remarks>Valid for a OnSocketFail.</remarks>
        public string ErrorMessage { get; set; }

        /// <summary>The TCP/IP message that was sent or received.</summary>
        /// <value>Type: <see cref="System.String" /></value>
        /// <remarks>Valid for a OnTcpSend and OnTcpReceive.</remarks>
        public string TcpMessage { get; set; }

        /// <summary>The type of error that occurred.</summary>
        /// <value>Type: <see cref="ClientIPErrorType" /></value>
        /// <remarks>Valid for a OnSocketFail.</remarks>
        public ClientIPErrorType ErrorType { get; set; }
    }

    #endregion

    /// <summary>
    /// Encapsulates the PaymentTerminal TCP/IP interface using a request/event pattern
    /// <remarks>Where possible use <see cref="EFTClientIPAsync"/></remarks>
    /// </summary>
    public class TerminalUtil : ITerminalUtil
    {
        #region Data
        private static Logger logger = LogManager.GetLogger("PaymentServiceLibLog");
        SynchronizationContext syncContext;
        IMessageParser _parser;
        ITcpSocket socket;
        TerminalBaseRequest currentRequest;
        AutoResetEvent hideDialogEvent;
        bool gotResponse;
        string recvBuf;
        int recvTickCount;
        bool requestInProgess;

        #endregion

        #region Constructors

        /// <summary>Construct an TerminalClientIP object.</summary>
        public TerminalUtil()
        {
            Initialise();
        }

        public void Dispose()
        {
            socket?.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>Connect to the PaymentTerminal IP Client.</summary>
        /// <returns>TRUE if connected successfully.</returns>
        /// <example>This example code shows how to connect to the EFT Client IP interface using the <see cref="TerminalClientIP" /> class.
        /// <code>
        ///	eftClientIP.HostName = "127.0.0.1";
        ///	eftClientIP.HostPort = 6001;
        ///	
        ///	if( !eftClientIP.Connect() )
        ///	{
        ///	    MessageBox.Show( "Couldn't connect to the PaymentTerminal IP Client at " + eftClientIP.HostName + ":" + eftClientIP.HostPort.ToString(),
        ///	        "PaymentTerminal IP Client Test POS", MessageBoxButtons.OK, MessageBoxIcon.Error );
        ///	    return false;
        ///	}
        /// </code>
        /// </example>
        public bool Connect()
        {
            socket.HostName = HostName;
            socket.HostPort = HostPort;
            socket.UseKeepAlive = UseKeepAlive;
            socket.UseSSL = UseSSL;
            return socket.Start();
        }

        /// <summary>Disconnect from the PaymentTerminal client IP interface.</summary>
        public void Disconnect()
        {
            socket.Stop();
        }

        /// <summary>Sends a request to the EFT-Client</summary>
        /// <param name="request">The <see cref="TerminalBaseRequest"/> to send</param>
        /// <param name="member">Used for internal logging. Ignore</param>
        /// <returns>FALSE if an error occurs</returns>
        public bool DoRequest(TerminalBaseRequest request, [CallerMemberName] string member = "")
        {
            SetCurrentRequest(request);
            logger.Info($"Request via {member}");

            // Save the current synchronization context so we can use it to send events 
            syncContext = System.Threading.SynchronizationContext.Current;

            if (requestInProgess)
            {
                logger.Info("Ignored, request already in progress");               
                return false;
            }

            if (!IsConnected)
            {
                logger.Info($"Not connected in {member} request, trying to connect now...");
                if (!Connect())
                {
                    logger.Error("Connect failed");
                    return false;
                }
            }

            var r = SendIPClientRequest(request);
            requestInProgess = r;
            return r;
        }

        /// <summary>Hide the PaymentTerminal dialogs.</summary>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoHideDialogs()
        {
            hideDialogEvent.Reset();

            if (!DoRequest(new SetDialogRequest() { DialogType = DialogType.Hidden }))
                return false;

            var r = hideDialogEvent.WaitOne(2000);
            requestInProgess = false;
            return r;
        }

                
        /// <summary>Initiate a PaymentTerminal logon.</summary>
        /// <param name="request">An <see cref="EFTLogonRequest" /> object.</param>
        /// <returns></returns>
        //public bool DoLogon(EFTLogonRequest request)
        //{
        //    return DoRequest(request);
        //}

        /// <summary>Initiate a PaymentTerminal transaction.</summary>
        /// <param name="request">An <see cref="EFTTransactionRequest" /> object.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoTransaction(ECRTransactionRequest request)
        {
            return DoRequest(request);
        }

        /// <summary>Initiate a PaymentTerminal get last transaction.</summary>
        /// <param name="request">An <see cref="EFTGetLastTransactionRequest" /> object.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoGetLastTransaction(EFTGetLastTransactionRequest request)
        {
            return DoRequest(request);
        }

        ///// <summary>Initiate a PaymentTerminal duplicate receipt request.</summary>
        ///// <param name="request">An <see cref="EFTReprintReceiptRequest" /> object.</param>
        ///// <returns>FALSE if an error occured.</returns>
        //public bool DoDuplicateReceipt(EFTReprintReceiptRequest request)
        //{
        //    return DoRequest(request);
        //}

        /// <summary>Send a key to PaymentTerminal.</summary>
        /// <param name="request">An <see cref="EFTSendKeyRequest" />.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoSendKey(EFTSendKeyRequest request)
        {
            return DoRequest(request);
        }

        /// <summary>Send a key to PaymentTerminal.</summary>
        /// <param name="key">An <see cref="EFTPOSKey" />.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoSendKey(EFTPOSKey key)
        {
            return DoRequest(new EFTSendKeyRequest() { Key = key });
        }

  

        /// <summary>Send a request to PaymentTerminal to open the control panel.</summary>
        /// <param name="request">An <see cref="ControlPanelRequest" /> object.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoDisplayControlPanel(EFTControlPanelRequest request)
        {
            return DoRequest(request);
        }

        /// <summary>Send a request to PaymentTerminal to initiate a settlement.</summary>
        /// <param name="request">An <see cref="EFTSettlementRequest" /> object.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoSettlement(EFTSettlementRequest request)
        {
            return DoRequest(request);
        }

        /// <summary>Send a request to PaymentTerminal for a PIN pad status.</summary>
        /// <param name="request">An <see cref="EFTStatusRequest" /> object.</param>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoStatus(EFTStatusRequest request)
        {
            return DoRequest(request);
        }

     



       
        /// <summary>Send a request to PaymentTerminal to pass a slave cmd to the PIN pad.</summary>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoSlaveCommand(string command)
        {
            return DoRequest(new EFTSlaveRequest() { RawCommand = command });
        }

        /// <summary>Send a request to PaymentTerminal for a merchant config.</summary>
        /// <returns>FALSE if an error occured.</returns>
        public bool DoConfigMerchant(EFTConfigureMerchantRequest request)
        {
            return DoRequest(request);
        }

        ///// <summary>Send a cloud logon request to PaymentTerminal .</summary>
        ///// <returns>FALSE if an error occured.</returns>
        //public bool DoCloudLogon(EFTCloudLogonRequest request)
        //{
        //    return DoRequest(request);
        //}

        /// <summary>Clears the request in progress flag.</summary>
        /// <returns></returns>
        public void ClearRequestInProgress()
        {
            requestInProgess = false;
        }

        #endregion

        #region Methods


        void Initialise()
        {
            recvBuf = "";
            recvTickCount = 0;
            _parser = DependencyInjector.Get<IMessageParser, DefaultMessageParser>();
            socket = DependencyInjector.Get<ITcpSocket, TcpSocket>(HostName, HostPort);

            socket.OnTerminated += new TcpSocketEventHandler(_OnTerminated);
            socket.OnDataWaiting += new TcpSocketEventHandler(_OnDataWaiting);
            socket.OnError += new TcpSocketEventHandler(_OnError);
            socket.OnSend += new TcpSocketEventHandler(_OnSend);

            hideDialogEvent = new AutoResetEvent(false);
        }

        bool WaitForIPClientResponse(AutoResetEvent ResetEvent)
        {
            gotResponse = false;
            ResetEvent.WaitOne(500, false);
            requestInProgess = false;
            return gotResponse;
        }

        void ProcessEFTResponse(TerminalBaseResponse response)
        {
            try
            {
                switch (response)
                {
                    case null:
                        //Log(LogLevel.Error, tr => tr.Set($"Unable to handle null param {nameof(response)}"));
                        break;

                    case SetDialogResponse r:
                        if (currentRequest is SetDialogRequest)
                        {
                            gotResponse = true;
                            hideDialogEvent.Set();
                        }
                        break;

                    case EFTReceiptResponse r:
                        SendReceiptAcknowledgement();
                        //Log(LogLevel.Info, tr => tr.Set($"IsPrePrint={((EFTReceiptResponse)response).IsPrePrint}"));

                        if (r.IsPrePrint == false)
                        {
                            FireClientResponseEvent(nameof(OnReceipt), OnReceipt, new EFTEventArgs<EFTReceiptResponse>(r));
                        }
                        break;

                    case EFTDisplayResponse r:
                        //DialogUIHandler.HandleDisplayResponse(r);
                        FireClientResponseEvent(nameof(OnDisplay), OnDisplay, new EFTEventArgs<EFTDisplayResponse>(r));
                        break;

                    case EFTLogonResponse r:
                        FireClientResponseEvent(nameof(OnLogon), OnLogon, new EFTEventArgs<EFTLogonResponse>(r));
                        break;

                    case EFTCloudLogonResponse r:
                        FireClientResponseEvent(nameof(OnCloudLogon), OnCloudLogon, new EFTEventArgs<EFTCloudLogonResponse>(r));
                        break;

                    case EFTTransactionResponse r:
                        FireClientResponseEvent(nameof(OnTransaction), OnTransaction, new EFTEventArgs<EFTTransactionResponse>(r));
                        break;

                    case EFTGetLastTransactionResponse r:
                        FireClientResponseEvent(nameof(OnGetLastTransaction), OnGetLastTransaction, new EFTEventArgs<EFTGetLastTransactionResponse>(r));
                        break;

                    case EFTReprintReceiptResponse r:
                        FireClientResponseEvent(nameof(OnDuplicateReceipt), OnDuplicateReceipt, new EFTEventArgs<EFTReprintReceiptResponse>(r));
                        break;

                    case EFTControlPanelResponse r:
                        FireClientResponseEvent(nameof(OnDisplayControlPanel), OnDisplayControlPanel, new EFTEventArgs<EFTControlPanelResponse>(r));
                        break;

                    case EFTSettlementResponse r:
                        FireClientResponseEvent(nameof(OnSettlement), OnSettlement, new EFTEventArgs<EFTSettlementResponse>(r));
                        break;

                    case EFTStatusResponse r:
                        FireClientResponseEvent(nameof(OnStatus), OnStatus, new EFTEventArgs<EFTStatusResponse>(r));
                        break;

                    case EFTQueryCardResponse r:
                        FireClientResponseEvent(nameof(OnQueryCard), OnQueryCard, new EFTEventArgs<EFTQueryCardResponse>(r));
                        break;

                    case EFTChequeAuthResponse r:
                        FireClientResponseEvent(nameof(OnChequeAuth), OnChequeAuth, new EFTEventArgs<EFTChequeAuthResponse>(r));
                        break;

                    case EFTGetPasswordResponse r:
                        FireClientResponseEvent(nameof(OnGetPassword), OnGetPassword, new EFTEventArgs<EFTGetPasswordResponse>(r));
                        break;

                    case EFTSlaveResponse r:
                        FireClientResponseEvent(nameof(OnSlave), OnSlave, new EFTEventArgs<EFTSlaveResponse>(r));
                        break;

                    case EFTConfigureMerchantResponse r:
                        FireClientResponseEvent(nameof(OnConfigMerchant), OnConfigMerchant, new EFTEventArgs<EFTConfigureMerchantResponse>(r));
                        break;

                    case EFTClientListResponse r:
                        FireClientResponseEvent(nameof(OnClientList), OnClientList, new EFTEventArgs<EFTClientListResponse>(r));
                        break;

                    default:
                        //Log(LogLevel.Error, tr => tr.Set($"Unknown response type", response));
                        break;
                }
            }
            catch (Exception Ex)
            {
                //Log(LogLevel.Error, tr => tr.Set($"Unhandled error in {nameof(ProcessEFTResponse)}", Ex));
            }
        }

        void SendReceiptAcknowledgement()
        {
            socket.Send("#00073 ");
        }



        bool SendIPClientRequest(TerminalBaseRequest eftRequest)
        {
            // Store current request.
            this.currentRequest = eftRequest;

            // Build request
            var requestString = "";

            try
            {
                requestString = _parser.EFTRequestToString(eftRequest);
            }
            catch (Exception e)
            {
                //Log(LogLevel.Error, tr => tr.Set($"An error occured parsing the request", e));
                throw;
            }

            //Log(LogLevel.Debug, tr => tr.Set($"Tx {requestString}"));

            // Send the request string to the IP client.
            return socket.Send(requestString);
        }

        private void SetCurrentRequest(TerminalBaseRequest request)
        {
            // Always set _currentRequest to the last request we send
            currentRequest = request;

            if (request.GetIsStartOfTransactionRequest())
            {
                _currentStartTxnRequest = request;
            }
        }
        #endregion

        #region Parse response

        bool ReceiveTerminalResponse(byte[] data)
        {
            // Clear the receive buffer if 5 seconds has lapsed since the last message
            var tc = System.Environment.TickCount;
            if (tc - recvTickCount > 5000)
            {
                //Log(LogLevel.Debug, tr => tr.Set($"Data is being cleared from the buffer due to a timeout. Content {recvBuf.ToString()}"));
                recvBuf = "";
            }
            recvTickCount = System.Environment.TickCount;

            // Append receive data to our buffer
            recvBuf += System.Text.Encoding.ASCII.GetString(data, 0, data.Length);

            // Keep parsing until no more characters
            try
            {
                int index = 0;
                while (index < recvBuf.Length)
                {
                    // Look for start character
                    if (recvBuf[index] == (byte)'#')
                    {
                        // Check that we have enough bytes to validate length, if not wait for more
                        if (recvBuf.Length < index + 5)
                        {
                            recvBuf = recvBuf.Substring(index);
                            break;
                        }

                        // We have enough bytes to check for length
                        index++;

                        // Try to get the length of the new message. If it's not a valid length 
                        // we might have some corrupt data, keep checking for a valid message
                        if (!int.TryParse(recvBuf.Substring(index, 4), out int length) || length <= 5)
                        {
                            continue;
                        }

                        // We have a valid length
                        index += 4;

                        // If our buffer doesn't contain enough data, wait for more 
                        if (recvBuf.Length < index + length - 5)
                        {
                            recvBuf = recvBuf.Substring(index - 5);
                            continue;
                        }

                        // We have a valid response
                        var response = recvBuf.Substring(index, length - 5);
                        FireOnTcpReceive(response);

                        // Process the response
                        TerminalBaseResponse eftResponse = null;
                        try
                        {
                            eftResponse = _parser.StringToEFTResponse(response);
                            ProcessEFTResponse(eftResponse);
                            if (eftResponse.GetType() == _currentStartTxnRequest?.GetPairedResponseType())
                            {
                                dialogUIHandler.HandleCloseDisplay();
                            }
                        }
                        catch (ArgumentException argumentException)
                        {
                            logger.Error("Error parsing response string", argumentException);
                        }


                        index += length - 5;
                    }

                    // Clear our buffer if we are all done
                    if (index == recvBuf.Length)
                    {
                        recvBuf = "";
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail gracefully.
                FireOnSocketFailEvent(ClientIPErrorType.Client_ParseError, ex.Message);
                //Log(LogLevel.Error, tr => tr.Set($"Exception (ReceiveTerminalResponse): {ex.Message}", ex));
                return false;
            }

            return true;
        }

        #endregion

        #region Event Handlers

        void _OnError(object sender, TcpSocketEventArgs e)
        {
            ClientIPErrorType errorType = ClientIPErrorType.Socket_GeneralError;

            //Log(LogLevel.Error, tr => tr.Set($"OnError: {e.Error}"));
            switch (e.ExceptionType)
            {
                case TcpSocketExceptionType.ConnectException:
                    errorType = ClientIPErrorType.Socket_ConnectError;
                    break;
                case TcpSocketExceptionType.GeneralException:
                    errorType = ClientIPErrorType.Socket_GeneralError;
                    break;
                case TcpSocketExceptionType.ReceiveException:
                    errorType = ClientIPErrorType.Socket_ReceiveError;
                    break;
                case TcpSocketExceptionType.SendException:
                    errorType = ClientIPErrorType.Socket_SendError;
                    break;
            }

            FireOnSocketFailEvent(errorType, e.Error);
        }
        void _OnDataWaiting(object sender, TcpSocketEventArgs e)
        {
            //Log(LogLevel.Debug, tr => tr.Set($"Rx>>{System.Text.ASCIIEncoding.ASCII.GetString(e.Bytes)}<<"));
            ReceiveTerminalResponse(e.Bytes);
        }
        void _OnTerminated(object sender, TcpSocketEventArgs e)
        {
            FireOnTerminatedEvent(e.Error);
        }
        void _OnSend(object sender, TcpSocketEventArgs e)
        {
            FireOnTcpSend(e.Message);
        }

        #endregion

        #region Event Firers

        void FireClientResponseEvent<TEFTResponse>(string name, EventHandler<EFTEventArgs<TEFTResponse>> eventHandler, EFTEventArgs<TEFTResponse> args) where TEFTResponse : TerminalBaseResponse
        {
            //Log(LogLevel.Info, tr => tr.Set($"Handle {name}"));
            requestInProgess = false;

            var tmpEventHandler = eventHandler;
            if (tmpEventHandler != null)
            {
                if (UseSynchronizationContextForEvents && syncContext != null && syncContext != SynchronizationContext.Current)
                {
                    syncContext.Post(o => tmpEventHandler.Invoke(this, args), null);
                }
                else
                {
                    tmpEventHandler.Invoke(this, args);
                }
            }
            else
            {
                throw (new Exception($"There is no event handler defined for {name}"));
            }
        }

        void FireOnTcpSend(string message)
        {
            OnTcpSend?.Invoke(this, new SocketEventArgs() { TcpMessage = message });
        }
        void FireOnTcpReceive(string message)
        {
            OnTcpReceive?.Invoke(this, new SocketEventArgs() { TcpMessage = message });
        }
        void FireOnTerminatedEvent(string message)
        {
            ////Log(LogLevel.Info, tr => tr.Set($"OnTerminated: {message}"));
            OnTerminated?.Invoke(this, new SocketEventArgs() { ErrorMessage = message, ErrorType = ClientIPErrorType.Socket_GeneralError });
        }
        void FireOnSocketFailEvent(ClientIPErrorType errorType, string message)
        {
            ////Log(LogLevel.Error, tr => tr.Set($"OnSocketFail: {message}"));
            OnSocketFail?.Invoke(this, new SocketEventArgs() { ErrorMessage = message, ErrorType = ClientIPErrorType.Socket_GeneralError });
        }

        /// <summary>
        /// Returns the connected state as of the last read or write operation. This does not necessarily represent 
        /// the current state of the connection. 
        /// To check the current socket state call <see cref="CheckConnectState()"/>
        /// </summary>
        public bool CheckConnectState()
        {
            if (socket == null)
                return false;
            return socket.CheckConnectState();
        }

      

        #endregion

        #region Properties

        /// <summary>The IP host name of the PaymentTerminal IP Client.</summary>
        /// <value>Type: <see cref="System.String" /><para>The IP address or host name of the EFT Client IP interface.</para></value>
        /// <remarks>The setting of this property is required.<para>See <see cref="TerminalClientIP.Connect"></see> example.</para></remarks>
        public string HostName { get; set; } = "127.0.0.1";

        /// <summary>The IP port of the PaymentTerminal IP Client.</summary>
        /// <value>Type: <see cref="System.Int32" /><para>The listening port of the EFT Client IP interface.</para></value>
        /// <remarks>The setting of this property is required.<para>See <see cref="TerminalClientIP.Connect"></see> example.</para></remarks>
        public int HostPort { get; set; } = 2011;

        /// <summary>Indicates whether to use SSL encryption.</summary>
        /// <value>Type: <see cref="System.Boolean" /><para>Defaults to FALSE.</para></value>
        public bool UseSSL { get; set; } = false;

        /// <summary>Indicates whether to allow TCP keep-alives.</summary>
        /// <value>Type: <see cref="System.Boolean" /><para>Defaults to FALSE.</para></value>
        public bool UseKeepAlive { get; set; } = false;

        /// <summary>Indicates whether there is a request currently in progress.</summary>
        public bool IsRequestInProgress { get { return requestInProgess; } }

        /// <summary>Indicates whether EFT Client is currently connected.</summary>
        public bool IsConnected { get { return socket.IsConnected; } }

        /// <summary> When TRUE, the SynchronizationContext will be captured from requests and used to call events</summary>
        public bool UseSynchronizationContextForEvents { get; set; } = true;
 

        IDialogUIHandler dialogUIHandler = null;
        private TerminalBaseRequest _currentStartTxnRequest;

        public IDialogUIHandler DialogUIHandler
        {
            get
            {
                return dialogUIHandler;
            }
            set
            {
                dialogUIHandler = value;
                if (dialogUIHandler.TerminalClientIP == null)
                {
                    dialogUIHandler.TerminalClientIP = this;
                }
            }
        }

        public IMessageParser Parser
        {
            get
            {
                return _parser;
            }
            set
            {
                _parser = value;
            }
        }


        #endregion

        #region Events

        /// <summary>Fired when a client socket is terminated.</summary>
        public event EventHandler<SocketEventArgs> OnTerminated;
        /// <summary>Fired when a socket error occurs.</summary>
        public event EventHandler<SocketEventArgs> OnSocketFail;
        /// <summary>Fired when a get config merchant result is received.</summary>
        public event EventHandler<SocketEventArgs> OnTcpSend;
        /// <summary>Fired when a get config merchant result is received.</summary>
        public event EventHandler<SocketEventArgs> OnTcpReceive;
        /// <summary>Fired when a logging event occurs.</summary>
        public event EventHandler<LogEventArgs> OnLog;

        /// <summary>Fired when a display is received.</summary>
        public event EventHandler<EFTEventArgs<EFTDisplayResponse>> OnDisplay;
        /// <summary>Fired when a receipt is received.</summary>
        public event EventHandler<EFTEventArgs<EFTReceiptResponse>> OnReceipt;
        /// <summary>Fired when a logon result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTLogonResponse>> OnLogon;
        /// <summary>Fired when a cloud logon result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTCloudLogonResponse>> OnCloudLogon;
        /// <summary>Fired when a transaction result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTTransactionResponse>> OnTransaction;
        /// <summary>Fired when a get last transaction result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTGetLastTransactionResponse>> OnGetLastTransaction;
        /// <summary>Fired when a duplicate receipt result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTReprintReceiptResponse>> OnDuplicateReceipt;
        /// <summary>Fired when a display control panel result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTControlPanelResponse>> OnDisplayControlPanel;
        /// <summary>Fired when a settlement result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTSettlementResponse>> OnSettlement;
        /// <summary>Fired when a status result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTStatusResponse>> OnStatus;
        /// <summary>Fired when a cheque authorization result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTChequeAuthResponse>> OnChequeAuth;
        /// <summary>Fired when a query card result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTQueryCardResponse>> OnQueryCard;
        /// <summary>Fired when a get password result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTGetPasswordResponse>> OnGetPassword;
        /// <summary>Fired when a get slave result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTSlaveResponse>> OnSlave;
        /// <summary>Fired when a get config merchant result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTConfigureMerchantResponse>> OnConfigMerchant;
        /// <summary>Fired whan a get client list result is received.</summary>
        public event EventHandler<EFTEventArgs<EFTClientListResponse>> OnClientList;
        #endregion
    }
}
