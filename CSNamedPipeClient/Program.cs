using ObjectIPC;

class Program
{
    /// <summary>
    /// Main
    /// </summary>
    static void Main(string[] args)
    {
        {
            IpcMessage req = new IpcMessage(IpcCommand.IpcRequestListGpo, new IpcRequestListGpo());
            IpcMessage res = IpcModule.GetIpcResponse("OBJECT-IPC", req);
            if (res != null)
            {
                res.IpcCommand = res.IpcCommand;
            }
        }
        {
            IpcMessage req = new IpcMessage(IpcCommand.IpcRequestExit, null);
            IpcMessage res = IpcModule.GetIpcResponse("OBJECT-IPC", req);
            if (res != null)
            {
                res.IpcCommand = res.IpcCommand;
            }
        }
    }
}
