using ObjectIPC;

class Program
{
    const int BUFFER_SIZE = 1024;  // 1 KB

    /// <summary>
    /// Main
    /// </summary>
    static void Main(string[] args)
    {
        {
            IpcMessage req = new IpcMessage(IpcCommand.IpcRequestListGpo, new IpcRequestListGpo());
            IpcMessage res = IpcModule.GetIpcResponse("PPLPM-RGT", req);
            if (res != null)
            {
                res.IpcCommand = res.IpcCommand;
            }
        }
        {
            IpcMessage req = new IpcMessage(IpcCommand.IpcRequestExit, null);
            IpcMessage res = IpcModule.GetIpcResponse("PPLPM-RGT", req);
            if (res != null)
            {
                res.IpcCommand = res.IpcCommand;
            }
        }
    }
}