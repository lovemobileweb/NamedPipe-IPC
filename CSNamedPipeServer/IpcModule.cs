using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace ObjectIPC
{
    public class IpcModule
    {
        const int BUFFER_SIZE = 4096;  // 4 KB
        Thread thread;

        /// <summary>
        /// Named pipe client through BCL System.IO.Pipes
        /// </summary>
        public static IpcMessage GetIpcResponse(string strPipeName, IpcMessage req)
        {
            /////////////////////////////////////////////////////////////////////
            // Try to open a named pipe.
            // 
            IpcMessage res = null;
            // Prepare the pipe name
            string strServerName = ".";

            NamedPipeClientStream pipeClient = null;

            try
            {
                pipeClient = new NamedPipeClientStream(
                    strServerName,              // The server name
                    strPipeName,                // The unique pipe name
                    PipeDirection.InOut,        // The pipe is bi-directional   
                    PipeOptions.None,           // No additional parameters

                    //The server process cannot obtain identification information about 
                    //the client, and it cannot impersonate the client.
                    TokenImpersonationLevel.Anonymous);

                pipeClient.Connect(60000); // set TimeOut for connection
                pipeClient.ReadMode = PipeTransmissionMode.Message;

                Console.WriteLine(@"The named pipe, \\{0}\{1}, is connected.",
                    strServerName, strPipeName);


                /////////////////////////////////////////////////////////////////
                // Send a message to the pipe server and receive its response.
                //                

                // A byte buffer of BUFFER_SIZE bytes. The buffer should be big 
                // enough for ONE request to the client

                byte[] bRequest;                        // Client -> Server
                int cbRequestBytes;
                byte[] bReply = new byte[BUFFER_SIZE];  // Server -> Client
                int cbBytesRead, cbReplyBytes;

                // Send one message to the pipe.
                string strMessage = req.ToMessage();
                bRequest = Encoding.Unicode.GetBytes(strMessage);
                cbRequestBytes = bRequest.Length;
                if (pipeClient.CanWrite)
                {
                    pipeClient.Write(bRequest, 0, cbRequestBytes);
                }
                pipeClient.Flush();

                Console.WriteLine("Sends {0} bytes; Message: \"{1}\"",
                    cbRequestBytes, strMessage.TrimEnd('\0'));

                // Receive one message from the pipe.

                cbReplyBytes = BUFFER_SIZE;
                strMessage = string.Empty;
                do
                {
                    if (pipeClient.CanRead)
                    {
                        cbBytesRead = pipeClient.Read(bReply, 0, cbReplyBytes);

                        // Unicode-encode the byte array and trim all the '\0' chars 
                        // at the end.
                        string strPacket = Encoding.Unicode.GetString(bReply, 0, cbBytesRead).TrimEnd('\0');
                        strMessage += strPacket;
                        Console.WriteLine("Receives {0} bytes; Message: \"{1}\"",
                            cbBytesRead, strPacket);
                    }
                }
                while (!pipeClient.IsMessageComplete);
                res = IpcMessage.FromMessage(strMessage);
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine("Unable to open named pipe {0}\\{1}",
                   strServerName, strPipeName);
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("The client throws the error: {0}", ex.Message);
            }
            finally
            {
                /////////////////////////////////////////////////////////////////
                // Close the pipe.
                // 

                if (pipeClient != null)
                    pipeClient.Close();
            }

            return res;
        }

        /// <summary>
        /// Named pipe server through BCL System.IO.Pipes
        /// </summary>
        private static void IpcServerThread(object param)
        {
            NamedPipeServerStream pipeServer = null;
            string strPipeName = param as string;
            if (string.IsNullOrEmpty(strPipeName))
                return;
            try
            {
                /////////////////////////////////////////////////////////////////
                // Create a named pipe.
                // 

                // Prepare the security attributes
                // Granting everyone the full control of the pipe is just for 
                // demo purpose, though it creates a security hole.
                PipeSecurity pipeSa = new PipeSecurity();
                pipeSa.SetAccessRule(new PipeAccessRule("Everyone",
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));

                // Create the named pipe
                pipeServer = new NamedPipeServerStream(
                    strPipeName,                    // The unique pipe name.
                    PipeDirection.InOut,            // The pipe is bi-directional
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,   // Message type pipe 
                    PipeOptions.None,               // No additional parameters
                    BUFFER_SIZE,                    // Input buffer size
                    BUFFER_SIZE,                    // Output buffer size
                    pipeSa,                         // Pipe security attributes
                    HandleInheritability.None       // Not inheritable
                    );

                Console.WriteLine("The named pipe, {0}, is created", strPipeName);


                while (true)
                {
                    /////////////////////////////////////////////////////////////////
                    // Wait for the client to connect.
                    // 

                    Console.WriteLine("Waiting for the client's connection...");
                    pipeServer.WaitForConnection();

                    /////////////////////////////////////////////////////////////////
                    // Read client requests from the pipe and write the response.
                    // 

                    // A byte buffer of BUFFER_SIZE bytes. The buffer should be big 
                    // enough for ONE request from a client.

                    string strMessage = string.Empty;
                    byte[] bRequest = new byte[BUFFER_SIZE];// Client -> Server
                    int cbBytesRead, cbRequestBytes;
                    byte[] bReply;                          // Server -> Client
                    int cbBytesWritten, cbReplyBytes;

                    do
                    {
                        // Receive one message from the pipe.

                        cbRequestBytes = BUFFER_SIZE;
                        cbBytesRead = pipeServer.Read(bRequest, 0, cbRequestBytes);

                        // Unicode-encode the byte array and trim all the '\0' chars 
                        // at the end.
                        string strPacket = Encoding.Unicode.GetString(bRequest, 0, cbBytesRead).TrimEnd('\0');
                        strMessage += strPacket;
                        Console.WriteLine("Receives {0} bytes; Message: \"{1}\"",
                            cbBytesRead, strPacket);
                    }
                    while (!pipeServer.IsMessageComplete);

                    // Prepare the response.
                    IpcMessage req = IpcMessage.FromMessage(strMessage);
                    strMessage = string.Empty;

                    if (req != null)
                    {
                        if (req.IpcCommand == IpcCommand.IpcRequestExit)
                        {
                            IpcMessage res = new IpcMessage(IpcCommand.IpcResponseExit, null);
                            strMessage = res.ToMessage();
                        }
                        else if (req.IpcCommand == IpcCommand.IpcRequestListGpo)
                        {
                            var data = new IpcResponseListGpo();
                            data.Set();
                            IpcMessage res = new IpcMessage(IpcCommand.IpcResponseListGpo, data);
                            strMessage = res.ToMessage();
                        }

                        bReply = Encoding.Unicode.GetBytes(strMessage);
                        cbReplyBytes = bReply.Length;

                        // Write the response to the pipe.

                        pipeServer.Write(bReply, 0, cbReplyBytes);
                        // If no IO exception is thrown from Write, number of bytes 
                        // written (cbBytesWritten) != -1.
                        cbBytesWritten = cbReplyBytes;

                        Console.WriteLine("Replies {0} bytes; Message: \"{1}\"",
                            cbBytesWritten, strMessage.TrimEnd('\0'));

                        /////////////////////////////////////////////////////////////////
                        // Flush the pipe to allow the client to read the pipe's contents 
                        // before disconnecting. Then disconnect the pipe.
                        // 

                        pipeServer.Flush();
                        pipeServer.Disconnect();

                        if (req.IpcCommand == IpcCommand.IpcRequestExit)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("The server throws the error: {0}", ex.Message);
            }
            finally
            {
                if (pipeServer != null)
                {
                    // Close the stream.
                    pipeServer.Close();
                }
            }
        }

        /// <summary>
        /// Named pipe server through BCL System.IO.Pipes
        /// </summary>
        public void StartIpcServer(string strPipeName)
        {
            thread = new Thread(IpcServerThread);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(strPipeName);
        }

        public void CloseIpcServer()
        {
            thread.Abort();
        }
    }
}
