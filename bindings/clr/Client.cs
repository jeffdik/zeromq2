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
            Socket s = new Socket(ctx, SocketType.REQ);
            s.Connect("tcp://127.0.0.1:5555");

            string query_string = "SELECT * FROM mytable";
            Encoding encoding = Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(query_string);
            Message query = new Message(bytes.Length+1);
            Marshal.Copy(bytes, 0, query.Data, bytes.Length);
            Marshal.WriteByte(query.Data, bytes.Length, 0);
            s.Send(query, 0);

            Message resultset = new Message();
            s.Recv(resultset, 0);

            string resultset_string = Marshal.PtrToStringAnsi(resultset.Data);
            Console.WriteLine("Received response: '{0}'", resultset_string);
        } catch (ZmqException e) {
            Console.WriteLine("An error occurred: {0}\n", e.Message);
            return 1;
        }
        return 0;
    }
}