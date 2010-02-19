using System;
using System.Runtime.InteropServices;
using System.Text;

using Zmq;

public class Go
{
    public static void Main()
    {
        Context ctx = new Context(1, 1, 0);
        Socket s = ctx.CreateSocket(SocketType.REP);
        s.Bind("tcp://127.0.0.1:5555");

        while (true) {
            Console.WriteLine("Received query: '{0}'", s.RecvString(Encoding.GetEncoding("Unicode")));
            s.SendString("OK");
        }
    }
}