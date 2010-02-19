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
            using (Message query = new Message()) {
                s.Recv(query, 0);
                string query_string = Marshal.PtrToStringAnsi(query.Data, query.Size);
                Console.WriteLine("Received query: '{0}'", query_string);
            }

            using (Message resultset = new Message(resultset_string.Length))
            {
                Encoding encoding = Encoding.ASCII;
                byte[] bytes = encoding.GetBytes(resultset_string);
                Marshal.Copy(bytes, 0, resultset.Data, bytes.Length);
                s.Send(resultset, 0);
            }
        }
    }
}