/*
    Copyright (c) 2007-2010 iMatix Corporation

    This file is part of 0MQ.

    0MQ is free software; you can redistribute it and/or modify it under
    the terms of the Lesser GNU General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    0MQ is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    Lesser GNU General Public License for more details.

    You should have received a copy of the Lesser GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#ifndef __ZMQ_I_SIGNALER_HPP_INCLUDED__
#define __ZMQ_I_SIGNALER_HPP_INCLUDED__

#include "stdint.hpp"
#include "fd.hpp"

namespace zmq
{
    //  Virtual interface used to send signals. Individual implementations
    //  may restrict the number of possible signal types to send.

    struct i_signaler
    {
        virtual ~i_signaler () {};

        //  Send a signal with a specific ID.
        virtual void signal (int signal_) = 0;

        //  Wait for signal. Returns a set of signals in form of a bitmap.
        //  Signal with index 0 corresponds to value 1, index 1 to value 2,
        //  index 2 to value 3 etc.
        virtual uint64_t poll () = 0;

        //  Same as poll, however, if there is no signal available,
        //  function returns zero immediately instead of waiting for a signal.
        virtual uint64_t check () = 0;

        //  Returns file descriptor that allows waiting for signals. Specific
        //  signalers may not support this functionality. If so, the function
        //  returns retired_fd.
        virtual fd_t get_fd () = 0;
    };

}

#endif
