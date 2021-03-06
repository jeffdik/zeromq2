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

#ifndef __ZMQ_XREP_HPP_INCLUDED__
#define __ZMQ_XREP_HPP_INCLUDED__

#include <map>

#include "socket_base.hpp"
#include "blob.hpp"
#include "fq.hpp"

namespace zmq
{

    class xrep_t : public socket_base_t
    {
    public:

        xrep_t (class app_thread_t *parent_);
        ~xrep_t ();

        //  Overloads of functions from socket_base_t.
        void xattach_pipes (class reader_t *inpipe_, class writer_t *outpipe_,
            const blob_t &peer_identity_);
        void xdetach_inpipe (class reader_t *pipe_);
        void xdetach_outpipe (class writer_t *pipe_);
        void xkill (class reader_t *pipe_);
        void xrevive (class reader_t *pipe_);
        int xsetsockopt (int option_, const void *optval_, size_t optvallen_);
        int xsend (zmq_msg_t *msg_, int flags_);
        int xflush ();
        int xrecv (zmq_msg_t *msg_, int flags_);
        bool xhas_in ();
        bool xhas_out ();

    private:

        //  Inbound messages are fair-queued.
        fq_t fq;

        //  Outbound pipes indexed by the peer names.
        typedef std::map <blob_t, class writer_t*> outpipes_t;
        outpipes_t outpipes;

        xrep_t (const xrep_t&);
        void operator = (const xrep_t&);
    };

}

#endif
