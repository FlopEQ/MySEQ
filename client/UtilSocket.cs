using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Structures
{
    //========================================================================
    /// <summary> This class abstracts a socket </summary>
    public class CSocketClient : IDisposable
    {
        private const int cbufferSize = 1048576;
        private readonly object connectionLock = new object();
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
        private bool closeNotified;
        private bool disposed;

        // Delegate Method Types
        /// <summary> DelType: Called when a message is received </summary>
        public delegate void MESSAGE_HANDLER(CSocketClient pSocket, int iNumberOfBytes);

        /// <summary> DelType: Called when a connection is closed </summary>
        public delegate void CLOSE_HANDLER(CSocketClient pSocket);

        /// <summary> RefType: A network stream object </summary>
        private NetworkStream GetNetworkStream { get; set; }

        /// <summary> RefType: A TcpClient object for socket connection </summary>
        private TcpClient GetTcpClient { get; set; }

        /// <summary> DelType: A reference to a user supplied function to be called when a socket message arrives </summary>
        private MESSAGE_HANDLER GetMessageHandler { get; }

        /// <summary> DelType: A reference to a user supplied function to be called when a socket connection is closed </summary>
        private CLOSE_HANDLER GetCloseHandler { get; }

        /// <summary> SimType: A raw buffer to capture data comming off the socket </summary>
        public byte[] GetRawBuffer { get; set; }

        /// <summary> SimType: Size of the raw buffer for received socket data </summary>
        public int GetSizeOfRawBuffer { get; set; }

        // Constructor, Finalize, Dispose
        //********************************************************************
        /// <summary> Constructor for client support </summary>
        /// <param name="iSizeOfRawBuffer"> SimType: The size of the raw buffer </param>
        /// <param name="pfnMessageHandler"> DelType: Reference to the user defined message handler method </param>
        /// <param name="pfnCloseHandler"> DelType: Reference to the user defined close handler method </param>
        public CSocketClient(int iSizeOfRawBuffer,
            MESSAGE_HANDLER pfnMessageHandler, CLOSE_HANDLER pfnCloseHandler)
        {
            // Create the raw buffer
            GetSizeOfRawBuffer = iSizeOfRawBuffer;
            GetRawBuffer = new byte[iSizeOfRawBuffer];

            // Set the handler methods
            GetMessageHandler = pfnMessageHandler;
            GetCloseHandler = pfnCloseHandler;
        }

        // Private Methods
        //********************************************************************
        /// <summary> Reads messages until the connection closes. </summary>
        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (GetNetworkStream?.CanRead == true)
                {
                    var bytesReceived = await GetNetworkStream.ReadAsync(GetRawBuffer, 0, GetSizeOfRawBuffer).ConfigureAwait(false);

                    if (bytesReceived == 0)
                    {
                        LogLib.WriteLine("CSocketClient.ReceiveLoopAsync(): Shutting down", LogLevel.Error);
                        break;
                    }

                    try { GetMessageHandler(this, bytesReceived); }
                    catch (Exception ex) { LogLib.WriteLine("Error GetMessageHandler - CSocketClient.ReceiveLoopAsync(): ", ex); }
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected during disconnect.
            }
            catch (Exception ex)
            {
                LogLib.WriteLine("Error CSocketClient.ReceiveLoopAsync(): ", ex);
            }
            finally
            {
                NotifyClosed();
                Disconnect();
            }
        }

        private void NotifyClosed()
        {
            lock (connectionLock)
            {
                if (closeNotified)
                {
                    return;
                }

                closeNotified = true;
            }

            try { GetCloseHandler(this); }
            catch (Exception ex) { LogLib.WriteLine("Error CSocketClient.NotifyClosed(): ", ex); }
        }

        // Public Methods
        //********************************************************************
        /// <summary> Function used to connect to a server </summary>
        /// <param name="strIpAddress"> RefType: The address to connect to </param>
        /// <param name="iPort"> SimType: The Port to connect to </param>
        public void Connect(string strIpAddress, int iPort)
        {
            lock (connectionLock)
            {
                if (GetTcpClient?.Connected == true && GetNetworkStream != null)
                {
                    return;
                }

                Disconnect();

                // Attempt to establish a connection
                GetTcpClient = new TcpClient
                {
                    ReceiveBufferSize = cbufferSize,
                    SendBufferSize = cbufferSize,
                    NoDelay = true
                };

                GetTcpClient.Connect(strIpAddress, iPort);
                GetNetworkStream = GetTcpClient.GetStream();
                closeNotified = false;
            }

            Receive();
        }

        //********************************************************************
        /// <summary> Function used to disconnect from the server </summary>
        public void Disconnect()
        {
            lock (connectionLock)
            {
                // Close down the connection
                GetNetworkStream?.Close();
                GetTcpClient?.Close();
                GetNetworkStream = null;
                GetTcpClient = null;
            }
        }

        /// <summary> Function to send a raw buffer to the server </summary>
        /// <param name="pRawBuffer"> RefType: A Raw buffer of bytes to send </param>
        public void Send(byte[] pRawBuffer)
        {
            _ = SendAsync(pRawBuffer);
        }

        private async Task SendAsync(byte[] pRawBuffer)
        {
            await sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (GetNetworkStream?.CanWrite == true)
                {
                    await GetNetworkStream.WriteAsync(pRawBuffer, 0, pRawBuffer.Length).ConfigureAwait(false);
                    LogLib.WriteLine("CSocketClient.SendAsync(): GetNetworkStream.WriteAsync()", LogLevel.Debug);
                }
                else
                {
                    LogLib.WriteLine("Error CSocketClient.Send(Byte[]): Socket Closed");
                }
            }
            catch (Exception ex) { LogLib.WriteLine("Error CSocketClient.SendAsync(): ", ex); }
            finally { sendLock.Release(); }
        }

        //********************************************************************
        /// <summary> Wait for a message to arrive </summary>
        public void Receive()
        {
            if (GetNetworkStream?.CanRead == true)
            {
                _ = ReceiveLoopAsync();
            }
            else
            {
                LogLib.WriteLine("Error CSocketClient.Receive(): Socket Closed");
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Disconnect();
            sendLock.Dispose();
        }
    }
}
