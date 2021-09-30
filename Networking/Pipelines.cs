using ProtoBuf;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.Networking
{
    class Pipelines
    {

        private static bool IsServer = false;
        private static byte MyServerID = 0;
        public static Action<int, byte[]> PacketIn;

        public static void Init(bool isServer, byte serverID = 0)
        {
            IsServer = isServer;
            MyServerID = serverID;
            new Task(() => { AwaitConnection(); }).Start();
        }

        [ProtoContract]
        private class BasePacket
        {
            public SendDir send;
            public byte[] Pack<T>(T obj) where T : IPacket
            {
                int Id = obj.GetID();

                byte[] data = Serialize(obj);
                byte[] newdata = new byte[data.Length + (sizeof(int) * 3)];
                Array.Copy(BitConverter.GetBytes((int)send), 0, newdata, 0, sizeof(int));
                Array.Copy(BitConverter.GetBytes(Id), 0, newdata, 4, sizeof(int));
                Array.Copy(BitConverter.GetBytes(data.Length), 0, newdata, 8, sizeof(int));

                Array.Copy(data, 0, newdata, 12, data.Length);

                return newdata;
            }
            public BasePacket(SendDir dir) { send = dir; }
        }

        public interface IPacket
        {
            int GetID();
        }

        public static bool SendData(IPacket data, int serverID = 0)
        {
            try
            {
                string sendTo = IsServer ? serverID.ToString() : "Server";
                using (NamedPipeClientStream client = new NamedPipeClientStream("localhost", $"StarcoreBot-{sendTo}", PipeDirection.Out))
                {
                    client.Connect(1000);
                    if (client.IsConnected)
                    {
                        BasePacket send = new BasePacket(SendDir.Server);
                        var d = send.Pack(data);
                        Utils.Log($"Sending '{d.Length}' bytes to {sendTo}");
                        client.Write(d, 0, d.Length);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Log(e.Message);
                Utils.Log(e.StackTrace);
            }
            return false;
        }

        private static void AwaitConnection()
        {
            //PipeSecurity ps = new PipeSecurity();
            //ps.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));

            while (true)
            {
                string sendTo = IsServer ? "Server" : MyServerID.ToString();
                using (NamedPipeServerStream server = new NamedPipeServerStream($"StarcoreBot-{sendTo}", PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 1024, 1024))
                {
                    Utils.Log("Waiting connection");
                    server.WaitForConnection();

                    try
                    {
                        SendDir sendDir = (SendDir)server.ReadInt();
                        int code = server.ReadInt();
                        int length = server.ReadInt();

                        Utils.Log($"Recieved messege from {sendDir} with code of {code} and length of {length}");

                        byte[] buffer = new byte[length];
                        var bytes = server.Read(buffer, 0, buffer.Length);

                        PacketIn?.Invoke(code, buffer);
                    } 
                    catch
                    {
                        Utils.Log("Failed to read incoming message!");
                    }
                }
            }
        }


        private static byte[] Serialize<T>(T obj)
        {
            byte[] result;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Serializer.Serialize(memoryStream, obj);
                result = memoryStream.ToArray();
            }
            return result;
        }

        public enum SendDir
        {
            Server = 1,
            Bot = 2,
        }

    }


    public static class Extenstions
    {
        public static int ReadInt(this NamedPipeServerStream stream)
        {
            byte[] intval = new byte[sizeof(int)];
            stream.Read(intval, 0, sizeof(int));
            return BitConverter.ToInt32(intval);
        }
        public static T DeSerialize<T>(this byte[] data)
        {
            T result;
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                result = Serializer.Deserialize<T>(memoryStream);
            }
            return result;
        }
    }
}
