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
        s.Connect("tcp://127.0.0.1:5556");

        while (true)
        {
            Console.WriteLine(s.RecvString());
        }
    }
}