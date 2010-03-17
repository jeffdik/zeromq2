using System;
using System.Text;

using Zmq;

public class Go
{
    public static void Main()
    {
        Context ctx = new Context(1, 1, 0);
        Socket s = ctx.CreateSocket(SocketType.PUB);
        s.Bind("tcp://127.0.0.1:5555");

        Encoding encoding = Encoding.GetEncoding("ASCII");
        for (long msg_id = 1; msg_id < 50000; msg_id++)
        {
            s.SendString(msg_id.ToString(), encoding);
        }
    }
}