using ObjectIPC;
using System.Threading;


class Program
{
    static void Main(string[] args)
    {
        IpcModule ipc = new IpcModule();
        ipc.StartIpcServer("OBJECT-IPC");
        //Thread.Sleep(10000);
        //ipc.CloseIpcServer();
    }
}
