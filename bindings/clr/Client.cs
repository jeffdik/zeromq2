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

            string query_string = "SELECT * FROM mytable";
            StringMessage query = new StringMessage(query_string);
            s.Send(query);

            StringMessage resultset = new StringMessage();
            s.Recv(resultset);

            Console.WriteLine("Received response: '{0}'", resultset.GetString());
        } catch (ZmqException e) {
            Console.WriteLine("An error occurred: {0}\n", e.Message);
            return 1;
        }
        return 0;
    }
}