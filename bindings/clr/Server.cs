using System;
using System.Runtime.InteropServices;
using System.Text;

using Zmq;

public class Go
{
    public static void Main()
    {
        string resultset_string = "OK";

        Context ctx = new Context(1, 1, 0);
        Socket s = ctx.CreateSocket(SocketType.REP);
        s.Bind("tcp://127.0.0.1:5555");

        while (true) {
            using (StringMessage query = new StringMessage()) {
                s.Recv(query, 0);
                Console.WriteLine("Received query: '{0}'", query.GetString());
            }

            using (StringMessage resultset = new StringMessage(resultset_string))
            {
                s.Send(resultset, 0);
            }
        }
    }
}