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
        Socket s = new Socket(ctx, SocketType.REP);
        s.Bind("tcp://192.168.0.11:5555");

        while (true) {
            Message query = new Message();
            s.Recv(query, 0);
            string query_string = Marshal.PtrToStringAnsi(query.Data);
            Console.WriteLine("Received query: '{0}'", query_string);
            query = null;

            Message resultset = new Message(resultset_string.Length+1);
            Encoding encoding = Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(resultset_string);
            Marshal.Copy(bytes, 0, resultset.Data, bytes.Length);
            Marshal.WriteByte(resultset.Data, bytes.Length, 0);
            s.Send(resultset, 0);
        }
    }
}