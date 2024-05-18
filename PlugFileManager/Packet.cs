
using Quasar.Common.DNS;
using Quasar.Common.Helpers;
using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using Plugin.Networking;
using Plugin.Messages;

namespace Plugin
{
    /// <summary>
    /// The client application which handles basic bootstrapping of the message processors and background tasks.
    /// </summary>
    public class QuasarApplication
    {

        /// <summary>
        /// The client used for the connection to the server.
        /// </summary>
        private QuasarClient _connectClient;

        /// <summary>
        /// List of <see cref="IMessageProcessor"/> to keep track of all used message processors.
        /// </summary>
        private readonly List<IMessageProcessor> _messageProcessors;





        /// <summary>
        /// Initializes a new instance of the <see cref="QuasarApplication"/> class.
        /// </summary>
        public QuasarApplication()
        {
            _messageProcessors = new List<IMessageProcessor>();

        }





        /// <summary>
        /// Begins running the application.
        /// </summary>
        public void Run()
        {
            var hosts = new HostsManager(new HostsConverter().RawHostsToList(Plugin.HOSTS));
            _connectClient = new QuasarClient(hosts, Plugin.CERTIFICATE);
            _connectClient.ClientState += ConnectClientOnClientState;
            InitializeMessageProcessors(_connectClient);



            new Thread(() =>
            {
                // Start connection loop on new thread and dispose application once client exits.
                // This is required to keep the UI thread responsive and run the message loop.
                _connectClient.ConnectLoop();
            }).Start();
            
        }

        private void ConnectClientOnClientState(Networking.Client s, bool connected)
        {

        }

        /// <summary>
        /// Adds all message processors to <see cref="_messageProcessors"/> and registers them in the <see cref="MessageHandler"/>.
        /// </summary>
        /// <param name="client">The client which handles the connection.</param>
        /// <remarks>Always initialize from UI thread.</remarks>
        private void InitializeMessageProcessors(QuasarClient client)
        {
            _messageProcessors.Add(new FileManagerHandler(client));



            foreach (var msgProc in _messageProcessors)
            {
                MessageHandler.Register(msgProc);

            }
        }

        /// <summary>
        /// Disposes all message processors of <see cref="_messageProcessors"/> and unregisters them from the <see cref="MessageHandler"/>.
        /// </summary>
        private void CleanupMessageProcessors()
        {
            foreach (var msgProc in _messageProcessors)
            {
                MessageHandler.Unregister(msgProc);

                if (msgProc is IDisposable disposableMsgProc)
                    disposableMsgProc.Dispose();
            }
        }

       
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupMessageProcessors();

                _connectClient?.Dispose();
               
            }

        }
    }
}
