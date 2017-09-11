
namespace ObjectIPC
{
    class IpcRequestListGpo
    {
        public string Filter { get; set; }

        public IpcRequestListGpo(string filter = "*")
        {
            Filter = filter;
        }
    }
}
