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
            // if (ptr == IntPtr.Zero)
            //     throw new 
                
        }

        [DllImport("libzmq", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr zmq_init(int app_threads, int io_threads, int flags);
    }

}