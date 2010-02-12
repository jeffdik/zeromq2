using System;

using Zmq;

public class Go
{
    public static void Main()
    {
        try {
        Context context = new Context(1, -1, 0);
        } catch (ZmqException e) {
            Console.WriteLine("Errno: {0}", e.Errno);
            throw;
        }
    }
}