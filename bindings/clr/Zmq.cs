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

namespace Zmq
{
    internal class C
    {
        // Context
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_init(int app_threads, int io_threads, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_term(IntPtr context);

        // Socket
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_close(IntPtr socket);

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_bind(IntPtr socket, string addr);

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_connect(IntPtr socket, string addr);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_flush(IntPtr socket);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_recv(IntPtr socket, IntPtr msg, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_send(IntPtr socket, IntPtr msg, int flags);
        
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_setsockopt(IntPtr socket, SocketOption option, IntPtr optval, int optvallen); 

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_socket(IntPtr context, SocketType type);

        // Message
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_close(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr zmq_msg_data(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_init(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_init_size(IntPtr msg, int size);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_msg_size(IntPtr msg);

        // Errors
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_errno();

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        public static extern string zmq_strerror(int errnum);

    }

    public enum SocketOption
    {
        HWM=1,
        LWM,
        SWAP,
        AFFINITY,
        IDENTITY,
        SUBSCRIBE,
        UNSUBSCRIBE,
        RATE,
        RECOVERY_IVL,
        MCAST_LOOP,
        SNDBUF,
        RCVBUF
    }

    public enum SocketType
    {
        P2P,
        PUB,
        SUB,
        REQ,
        REP,
        XREQ,
        XREP,
        UPSTREAM,
        DOWNSTREAM
    }

    public class ZmqException : Exception
    {
        private int errno;

        public int Errno
        {
            get { return errno; }
        }

        public ZmqException(int errno) : base(C.zmq_strerror(errno))
        {
            this.errno = errno;
        }

        public static void ThrowIfNotZero(int rc)
        {
            if (rc != 0)
                Throw();
        }

        public static void ThrowIfNotZero(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                Throw();
        }

        private static void Throw()
        {
                throw new ZmqException(C.zmq_errno());
        }
    }

    public class Context : IDisposable
    {
        private IntPtr ptr;

        public Context(int app_threads, int io_threads, int flags)
        {
            ptr = C.zmq_init(app_threads, io_threads, flags);
            ZmqException.ThrowIfNotZero(ptr);
        }

        ~Context()
        {
            Dispose(false);
        }

        public Socket CreateSocket(SocketType type)
        {
            IntPtr socket_ptr = C.zmq_socket(ptr, type);
            ZmqException.ThrowIfNotZero(ptr);

            return new Socket(socket_ptr, type);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // if (disposing)
            // {
            //     // Clean up all managed resources
            // }
            
            if (ptr != IntPtr.Zero) {
                int rc = C.zmq_term(ptr);
                ptr = IntPtr.Zero;
                ZmqException.ThrowIfNotZero(rc);
            }
        }
    }

    public class Socket : IDisposable
    {
        private IntPtr ptr;
        private SocketType type;

        // got EAGAIN by compiling and running the following C code with VC++ 2008 Express
        // #include <errno.h>
        // #include <stdio.h>
        //
        // int main()
        // {
        // 	printf("%i\n", EAGAIN);
        // }
        //
        // E:\jad\repos\external\zeromq2\bindings\clr>..\..\builds\msvc\Release\whats_eagain.exe
        // 11
        private const int EAGAIN=11;

        // Don't call this, call Context.CreateSocket
        public Socket(IntPtr ptr, SocketType type)
        {
            this.ptr = ptr;
            this.type = type;
        }

        ~Socket()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // if (disposing)
            // {
            //     // Clean up all managed resources
            // }

            if (ptr != IntPtr.Zero) {
                int rc = C.zmq_close(ptr);
                ptr = IntPtr.Zero;
                ZmqException.ThrowIfNotZero(rc);
            }
        }

        public void Bind(string addr)
        {
            int rc = C.zmq_bind(ptr, addr);
            ZmqException.ThrowIfNotZero(rc);
        }

        public void Connect(string addr)
        {
            int rc = C.zmq_connect(ptr, addr);
            ZmqException.ThrowIfNotZero(rc);
        }

        public void Flush()
        {
            int rc = C.zmq_flush(ptr);
            ZmqException.ThrowIfNotZero(rc);
        }

        public bool Recv(Message msg)
        {
            return Recv(msg, 0);
        }

        public bool Recv(Message msg, int flags)
        {
            int rc = C.zmq_recv(ptr, msg.Ptr, flags);
            return CheckRecvSendReturnCode(rc);
        }

        public string RecvString(StringMessage msg)
        {
            using (msg) {
                int rc = C.zmq_recv(ptr, msg.Ptr, 0);
                ZmqException.ThrowIfNotZero(rc);
                return msg.GetString();
            }
        }

        public string RecvString()
        {
            return RecvString(new StringMessage());
        }

        public string RecvString(Encoding encoding)
        {
            return RecvString(new StringMessage(encoding));
        }

        public bool Send(Message msg)
        {
            return Send(msg, 0);
        }

        public bool Send(Message msg, int flags)
        {
            int rc = C.zmq_send(ptr, msg.Ptr, flags);
            return CheckRecvSendReturnCode(rc);
        }

        public bool SendString(StringMessage msg)
        {
            using (msg) {
                return Send(msg);
            }
        }

        public bool SendString(string s)
        {
            return SendString(new StringMessage(s));
        }

        public bool SendString(string s, Encoding encoding)
        {
            return SendString(new StringMessage(s, encoding));
        }


        public int SetSockOpt(SocketOption option, IntPtr optval, int optvallen)
        {
            int rc = C.zmq_setsockopt (ptr, option, optval, optvallen);
            ZmqException.ThrowIfNotZero(rc);            
            return rc;
        }

        private bool CheckRecvSendReturnCode(int rc)
        {
            if (rc == 0)
                return true;
            if ((rc == -1) && (C.zmq_errno() == EAGAIN))
                return false;
            throw new ZmqException(C.zmq_errno());
        }
    }

    public class Message : IDisposable
    {
        private IntPtr ptr;

        public IntPtr Ptr
        {
            get { return ptr; }
        }

        // see bindings/c/zmq.h for calculating how big zmq_msg_t should be...

        // #define ZMQ_MAX_VSM_SIZE 30

        // typedef struct
        // {
        //     void *content;                              // 4 bytes
        //     unsigned char shared;                       // 1 bytes
        //     unsigned char vsm_size;                     // 1 bytes
        //     unsigned char vsm_data [ZMQ_MAX_VSM_SIZE];  // 1*30 bytes
        // } zmq_msg_t;

        // better calculated by compiling and running the following C code
        // #include <stdio.h>
        // #include <zmq.h>
        //
        // void main()
        // {
        // 	printf("%i\n", sizeof(zmq_msg_t));
        // }
        //
        // E:\jad\repos\external\zeromq2\bindings\clr>zmq_msg_t_size.exe
        // 36
        private const int ZMQ_MSG_T_SIZE = 36;

        public Message()
        {
            ptr = Marshal.AllocHGlobal(ZMQ_MSG_T_SIZE);
            int rc = C.zmq_msg_init(ptr);
            ZmqException.ThrowIfNotZero(rc);
        }

        public Message(int size)
        {
            ptr = Marshal.AllocHGlobal(ZMQ_MSG_T_SIZE);
            int rc = C.zmq_msg_init_size(ptr, size);
            ZmqException.ThrowIfNotZero(rc);
        }

        public Message(byte[] bytes) : this(bytes.Length)
        {
            Marshal.Copy(bytes, 0, Data, bytes.Length);
        }

        ~Message()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // if (disposing)
            // {
            //     // Clean up all managed resources
            // }

            if (ptr != IntPtr.Zero) {
                int rc = C.zmq_msg_close(ptr);
                Marshal.FreeHGlobal(ptr);
                ptr = IntPtr.Zero;
                ZmqException.ThrowIfNotZero(rc);
            }
        }

        public IntPtr Data
        {
            get { return C.zmq_msg_data(ptr); }
        }

        public int Size
        {
            get { return C.zmq_msg_size(ptr); }
        }

    }

    public class StringMessage : Message
    {
        private static Encoding default_encoding = Encoding.Default;
        public static Encoding DefaultEncoding
        {
            get { return default_encoding; }
            set { default_encoding = value; }
        }

        private Encoding encoding;
        public Encoding Encoding
        {
            get { return encoding; }
        }

        public StringMessage() : this(default_encoding)
        {
        }

        public StringMessage(Encoding encoding) : base()
        {
            this.encoding = encoding;
        }


        public StringMessage(string s) : this(s, default_encoding)
        {
        }

        public StringMessage(string s, Encoding encoding) : base(encoding.GetBytes(s))
        {
            this.encoding = encoding;
        }

        public string GetString()
        {
            return GetString(encoding);
        }

        public string GetString(Encoding encoding)
        {
            byte[] bytes = new byte[Size];
            Marshal.Copy(Data, bytes, 0, bytes.Length);
            return encoding.GetString(bytes);
        }
    }
}
