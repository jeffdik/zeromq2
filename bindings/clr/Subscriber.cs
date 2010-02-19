using System;
using System.Runtime.InteropServices;
using System.Text;

using Zmq;

public class Go
{
    public static void Main()
    {
        Context ctx = new Context(1, 1, 0);
        Socket s = new Socket(ctx, SocketType.SUB);
        // Encoding encoding = Encoding.ASCII;
        IntPtr ptr = Marshal.AllocHGlobal(1);
        // Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr, 0, 0);

        s.SetSockOpt(SocketOption.SUBSCRIBE, ptr, 0);
        s.Connect("tcp://127.0.0.1:5555");

        while (true)
        {
            Message msg = new Message();
            s.Recv(msg, 0);
            long msg_id = Marshal.ReadInt64(msg.Data);
            if ((msg_id % 10000) == 0)
                Console.WriteLine(msg_id);
        }
    }
}