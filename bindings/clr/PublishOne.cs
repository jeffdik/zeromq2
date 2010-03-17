using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using Zmq;

public class Go
{
    public static int Main()
    {
        try {
            Context ctx = new Context(1, 1, 0);
            Socket s = ctx.CreateSocket(SocketType.P2P);
            s.Connect("tcp://127.0.0.1:5555");

            Encoding encoding = Encoding.GetEncoding("ASCII");
            s.SendString(DateTime.Now.ToString(), encoding);
            for (long msg_id = 1; msg_id < 10; msg_id++)
            {
                s.SendString(msg_id.ToString(), encoding);
                // Thread.Sleep(100);
            }
        } catch (ZmqException e) {
            Console.WriteLine("An error occurred: {0}\n", e.Message);
            return 1;
        }
        return 0;
    }
}