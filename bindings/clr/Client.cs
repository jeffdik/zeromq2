using System;
using System.Runtime.InteropServices;
using System.Text;

using Zmq;

public class Go
{
    public static int Main()
    {
        try {
            Context ctx = new Context(1, 1, 0);
            Socket s = ctx.CreateSocket(SocketType.REQ);
            s.Connect("tcp://127.0.0.1:5555");

            s.SendString("SELECT * FROM mytable", Encoding.GetEncoding("Unicode"));

            Console.WriteLine("Received response: '{0}'", s.RecvString());
        } catch (ZmqException e) {
            Console.WriteLine("An error occurred: {0}\n", e.Message);
            return 1;
        }
        return 0;
    }
}