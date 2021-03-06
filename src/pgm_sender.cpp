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

#include "platform.hpp"

#if defined ZMQ_HAVE_OPENPGM

#ifdef ZMQ_HAVE_WINDOWS
#include "windows.hpp"
#endif

#include <stdlib.h>

#include "io_thread.hpp"
#include "pgm_sender.hpp"
#include "err.hpp"
#include "wire.hpp"
#include "stdint.hpp"

zmq::pgm_sender_t::pgm_sender_t (io_thread_t *parent_, 
      const options_t &options_) :
    io_object_t (parent_),
    encoder (0),
    pgm_socket (false, options_),
    options (options_),
    out_buffer (NULL),
    out_buffer_size (0),
    write_size (0)
{
}

int zmq::pgm_sender_t::init (bool udp_encapsulation_, const char *network_)
{
    int rc = pgm_socket.init (udp_encapsulation_, network_);
    if (rc != 0)
        return rc;

    out_buffer_size = pgm_socket.get_max_tsdu_size ();
    out_buffer = (unsigned char*) malloc (out_buffer_size);
    zmq_assert (out_buffer);

    return rc;
}

void zmq::pgm_sender_t::plug (i_inout *inout_)
{
    //  Alocate 2 fds for PGM socket.
    int downlink_socket_fd = 0;
    int uplink_socket_fd = 0;
    int rdata_notify_fd = 0;
    int pending_notify_fd = 0;

    encoder.set_inout (inout_);

    //  Fill fds from PGM transport and add them to the poller.
    pgm_socket.get_sender_fds (&downlink_socket_fd, &uplink_socket_fd,
        &rdata_notify_fd, &pending_notify_fd);

    handle = add_fd (downlink_socket_fd);
    uplink_handle = add_fd (uplink_socket_fd);
    rdata_notify_handle = add_fd (rdata_notify_fd);   
    pending_notify_handle = add_fd (pending_notify_fd);

    //  Set POLLIN. We wont never want to stop polling for uplink = we never
    //  want to stop porocess NAKs.
    set_pollin (uplink_handle);
    set_pollin (rdata_notify_handle);
    set_pollin (pending_notify_handle);

    //  Set POLLOUT for downlink_socket_handle.
    set_pollout (handle);
}

void zmq::pgm_sender_t::unplug ()
{
    rm_fd (handle);
    rm_fd (uplink_handle);
    rm_fd (rdata_notify_handle);
    rm_fd (pending_notify_handle);
    encoder.set_inout (NULL);
}

void zmq::pgm_sender_t::revive ()
{
    set_pollout (handle);
    out_event ();
}

void zmq::pgm_sender_t::add_prefix (const blob_t &identity_)
{
    //  No need for tracerouting functionality in PGM socket at the moment.
    zmq_assert (false);
}

void zmq::pgm_sender_t::trim_prefix ()
{
    //  No need for tracerouting functionality in PGM socket at the moment.
    zmq_assert (false);
}

zmq::pgm_sender_t::~pgm_sender_t ()
{
    if (out_buffer) {
        free (out_buffer);
        out_buffer = NULL;
    }
}

void zmq::pgm_sender_t::in_event ()
{
    //  In event on sender side means NAK or SPMR receiving from some peer.
    pgm_socket.process_upstream ();
}

void zmq::pgm_sender_t::out_event ()
{
    //  POLLOUT event from send socket. If write buffer is empty, 
    //  try to read new data from the encoder.
    if (write_size == 0) {

        //  First two bytes (sizeof uint16_t) are used to store message 
        //  offset in following steps. Note that by passing our buffer to
        //  the get data function we prevent it from returning its own buffer.
        unsigned char *bf = out_buffer + sizeof (uint16_t);
        size_t bfsz = out_buffer_size - sizeof (uint16_t);
        int offset = -1;
        encoder.get_data (&bf, &bfsz, &offset);

        //  If there are no data to write stop polling for output.
        if (!bfsz) {
            reset_pollout (handle);
            return;
        }

        //  Put offset information in the buffer.
        write_size = bfsz + sizeof (uint16_t);
        put_uint16 (out_buffer, offset == -1 ? 0xffff : (uint16_t) offset);
    }

    //  Send the data.
    size_t nbytes = pgm_socket.send (out_buffer, write_size);

    //  We can write either all data or 0 which means rate limit reached.
    if (nbytes == write_size)
        write_size = 0;
    else
        zmq_assert (nbytes == 0);
}

#endif

