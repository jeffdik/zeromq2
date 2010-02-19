using System;
using System.Runtime.InteropServices;
using System.Text;

using Zmq;

public class Go
{
    public static void Main()
    {
        Context ctx = new Context(1, 1, 0);
        Socket s = ctx.CreateSocket(SocketType.SUB);
        s.SetSockOpt(SocketOption.SUBSCRIBE, IntPtr.Zero, 0);
        s.Connect("tcp://127.0.0.1:5555");

        while (true)
        {
            using (Message msg = new Message()) {
                s.Recv(msg, 0);
                long msg_id = Marshal.ReadInt64(msg.Data);
                if ((msg_id % 10000) == 0)
                    Console.WriteLine(msg_id);
            }
        }
    }
}