using System;
using System.Runtime.InteropServices;

namespace Zmq
{
    public class ZmqException : Exception
    {
        private int errno;

        public ZmqException(int errno) : base(zmq_strerror(errno))
        {
            this.errno = errno;
        }

        public int Errno
        {
            get { return errno; }
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

}