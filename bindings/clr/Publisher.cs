using System;
using System.Runtime.InteropServices;

using Zmq;

public class Go
{
    public static void Main()
    {
        Context ctx = new Context(1, 1, 0);
        Socket s = new Socket(ctx, SocketType.PUB);
        s.Bind("tcp://127.0.0.1:5555");

        for (long msg_id = 1; ; msg_id++)
        {
            Message msg = new Message(8);
            Marshal.WriteInt64(msg.Data, msg_id);
            s.Send(msg, 0);
            long tmp = Marshal.ReadInt64(msg.Data);
            if ((tmp % 10000) == 0)
                Console.WriteLine(tmp);
            msg.Close();
        }
    }
}