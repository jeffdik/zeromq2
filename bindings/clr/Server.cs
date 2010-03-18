/*
    Copyright (c) 2010 Jeffrey Dik <s450r1@gmail.com>

    This file is part of clrzmq.

    clrzmq is free software; you can redistribute it and/or modify it under
    the terms of the Lesser GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    clrzmq is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    Lesser GNU General Public License for more details.

    You should have received a copy of the Lesser GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
            Console.WriteLine("Received query: '{0}'", s.RecvString());
            s.SendString("OK");
        }
    }
}
