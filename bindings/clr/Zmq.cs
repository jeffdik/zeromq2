using System;
using System.Runtime.InteropServices;

namespace Zmq
{
    public class ZmqException : Exception
    {
        private int errno;

        public int Errno
        {
            get { return errno; }
        }

        public ZmqException(int errno) : base(zmq_strerror(errno))
        {
            this.errno = errno;
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        public static extern int zmq_get_errno();

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        static extern string zmq_strerror(int errnum);
    }

    public class Context
    {
        private IntPtr ptr;

        public IntPtr Ptr
        {
            get { return ptr; }
        }

        public Context(int app_threads, int io_threads, int flags)
        {
            ptr = zmq_init(app_threads, io_threads, flags);
            if (ptr == IntPtr.Zero)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        ~Context()
        {
            int rc = zmq_term(ptr);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr zmq_init(int app_threads, int io_threads, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_term(IntPtr context);
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

    public class Socket
    {
        private IntPtr ptr;
        private Context context;
        private SocketType type;

        public Socket(Context context, SocketType type)
        {
            this.context = context;
            this.type = type;

            ptr = zmq_socket(context.Ptr, type);
            if (ptr == IntPtr.Zero)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        ~Socket()
        {
            int rc = zmq_close(ptr);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public void Bind(string addr)
        {
            int rc = zmq_bind(ptr, addr);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public void Connect(string addr)
        {
            int rc = zmq_connect(ptr, addr);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public bool Recv(Message msg, int flags)
        {
            int rc = zmq_recv(ptr, msg.Ptr, flags);
            if (rc == 0)
                return true;
            if ((rc == -1) && ZmqException.zmq_get_errno() == 1)
                return false;
            throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public bool Send(Message msg, int flags)
        {
            int rc = zmq_send(ptr, msg.Ptr, flags);
            if (rc == 0)
                return true;
            if ((rc == -1) && (ZmqException.zmq_get_errno() == 1))
                return false;
            throw new ZmqException(ZmqException.zmq_get_errno());
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_close(IntPtr context);

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_bind(IntPtr socket, string addr);

        [DllImport("libzmq", CharSet = CharSet.Ansi,
                   CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_connect(IntPtr socket, string addr);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_recv(IntPtr socket, IntPtr msg, int flags);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_send(IntPtr socket, IntPtr msg, int flags);
        
        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr zmq_socket(IntPtr context, SocketType type);
    }

    public class Message
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
        //     unsigned char shared;                       // 2 bytes
        //     unsigned char vsm_size;                     // 2 bytes
        //     unsigned char vsm_data [ZMQ_MAX_VSM_SIZE];  // 2*30 bytes
        // } zmq_msg_t;
        private const int ZMQ_MSG_T_SIZE = 68;

        public Message()
        {
            ptr = Marshal.AllocHGlobal(ZMQ_MSG_T_SIZE);
            int rc = zmq_msg_init(ptr);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public Message(int size)
        {
            ptr = Marshal.AllocHGlobal(ZMQ_MSG_T_SIZE);
            int rc = zmq_msg_init_size(ptr, size);
            if (rc != 0)
                throw new ZmqException(ZmqException.zmq_get_errno());
        }

        public IntPtr Data
        {
            get { return zmq_msg_data(ptr); }
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_msg_init(IntPtr msg);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern int zmq_msg_init_size(IntPtr msg, int size);

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr zmq_msg_data(IntPtr msg);
    }
}