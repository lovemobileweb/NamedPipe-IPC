using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ObjectIPC
{
    public class IpcMessage
    {
        public IpcCommand IpcCommand { get; set; }
        public object IpcData { get; set; }

        public IpcMessage(IpcCommand command, object data)
        {
            IpcCommand = command;
            IpcData = data;
        }

        public static IpcMessage FromMessage(string message)
        {
            IpcMessage msg = JsonConvert.DeserializeObject<IpcMessage>(message);
            if (msg != null && msg.IpcData is JObject)
            {
                string str = (msg.IpcData as JObject).ToString();
                if (msg.IpcCommand == IpcCommand.IpcRequestListGpo)
                    msg.IpcData = JsonConvert.DeserializeObject<IpcRequestListGpo>(str);
                else if (msg.IpcCommand == IpcCommand.IpcResponseListGpo)
                    msg.IpcData = JsonConvert.DeserializeObject<IpcResponseListGpo>(str);
            }
            return msg;
        }

        public T IpcDataAs<T>() where T : class
        {
            if (IpcData is T)
                return (T)IpcData;
            return null;
        }

        public string ToMessage()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public enum IpcCommand
    {
        IpcRequestExit,
        IpcResponseExit,
        IpcRequestListGpo,
        IpcResponseListGpo,
        IpcRequestPplpmCollections,
        IpcResponsePplpmCollections,
        IpcResponseUpdatePplpmRules,
        IpcRequestLog,
        IpcResponseLog
    }
}
