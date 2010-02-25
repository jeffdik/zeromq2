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

#include <new>

#include "../bindings/c/zmq.h"

#include "dispatcher.hpp"
#include "socket_base.hpp"
#include "app_thread.hpp"
#include "io_thread.hpp"
#include "platform.hpp"
#include "err.hpp"
#include "pipe.hpp"

#if defined ZMQ_HAVE_WINDOWS
#include "windows.h"
#endif

zmq::dispatcher_t::dispatcher_t (int app_threads_, int io_threads_,
      int flags_) :
    sockets (0),
    terminated (false)
{
#ifdef ZMQ_HAVE_WINDOWS
    //  Intialise Windows sockets. Note that WSAStartup can be called multiple
    //  times given that WSACleanup will be called for each WSAStartup.
    WORD version_requested = MAKEWORD (2, 2);
    WSADATA wsa_data;
    int rc = WSAStartup (version_requested, &wsa_data);
    zmq_assert (rc == 0);
    zmq_assert (LOBYTE (wsa_data.wVersion) == 2 &&
        HIBYTE (wsa_data.wVersion) == 2);
#endif

    //  Create application thread proxies.
    for (int i = 0; i != app_threads_; i++) {
        app_thread_info_t info;
        info.associated = false;
        info.app_thread = new (std::nothrow) app_thread_t (this, i, flags_);
        zmq_assert (info.app_thread);
        app_threads.push_back (info);
        signalers.push_back (info.app_thread->get_signaler ());
    }

    //  Create I/O thread objects.
    for (int i = 0; i != io_threads_; i++) {
        io_thread_t *io_thread = new (std::nothrow) io_thread_t (this,
            i + app_threads_, flags_);
        zmq_assert (io_thread);
        io_threads.push_back (io_thread);
        signalers.push_back (io_thread->get_signaler ());
    }

    //  Create the administrative thread. Nothing special is needed. NULL
    //  is used instead of signaler given that as for now, administrative
    //  thread doesn't receive any commands. The only thing it is used for
    //  is sending 'stop' command to I/O threads on shutdown.
    signalers.push_back (NULL);

    //  Create command pipe matrix.
    command_pipes = new  (std::nothrow) command_pipe_t [signalers.size () *
         signalers.size ()];
    zmq_assert (command_pipes);

    //  Launch I/O threads.
    for (int i = 0; i != io_threads_; i++)
        io_threads [i]->start ();
}

int zmq::dispatcher_t::term ()
{
    term_sync.lock ();
    zmq_assert (!terminated);
    terminated = true;
    bool destroy = (sockets == 0);
    term_sync.unlock ();
    
    if (destroy)
        delete this;

    return 0;
}

zmq::dispatcher_t::~dispatcher_t ()
{
    //  Ask I/O threads to terminate. If stop signal wasn't sent to I/O
    //  thread subsequent invocation of destructor would hang-up.
    for (io_threads_t::size_type i = 0; i != io_threads.size (); i++)
        io_threads [i]->stop ();

    //  Wait till I/O threads actually terminate.
    for (io_threads_t::size_type i = 0; i != io_threads.size (); i++)
        delete io_threads [i];

    //  Close all application theads, sockets, io_objects etc.
    for (app_threads_t::size_type i = 0; i != app_threads.size (); i++)
        delete app_threads [i].app_thread;

    //  Deallocate all the orphaned pipes.
    while (!pipes.empty ())
        delete *pipes.begin ();

    //  TODO: Deallocate any commands still in the pipes. Keep in mind that
    //  simple reading from a pipe and deallocating commands won't do as
    //  command pipe has template parameter D set to true, meaning that
    //  read may return false even if there are still commands in the pipe.
    delete [] command_pipes;

#ifdef ZMQ_HAVE_WINDOWS
    //  On Windows, uninitialise socket layer.
    int rc = WSACleanup ();
    wsa_assert (rc != SOCKET_ERROR);
#endif
}

int zmq::dispatcher_t::thread_slot_count ()
{
    return signalers.size ();
}

zmq::socket_base_t *zmq::dispatcher_t::create_socket (int type_)
{
    app_threads_sync.lock ();

    //  Find whether the calling thread has app_thread_t object associated
    //  already. At the same time find an unused app_thread_t so that it can
    //  be used if there's no associated object for the calling thread.
    //  Check whether thread ID is already assigned. If so, return it.
    app_threads_t::size_type unused = app_threads.size ();
    app_threads_t::size_type current;
    for (current = 0; current != app_threads.size (); current++) {
        if (app_threads [current].associated &&
              thread_t::equal (thread_t::id (), app_threads [current].tid))
            break;
        if (!app_threads [current].associated)
            unused = current;
    }

    //  If no app_thread_t is associated with the calling thread,
    //  associate it with one of the unused app_thread_t objects.
    if (current == app_threads.size ()) {
        if (unused == app_threads.size ()) {
            app_threads_sync.unlock ();
            errno = EMTHREAD;
            return NULL;
        }
        app_threads [unused].associated = true;
        app_threads [unused].tid = thread_t::id ();
        current = unused;
    }

    app_thread_t *thread = app_threads [current].app_thread;
    app_threads_sync.unlock ();

    socket_base_t *s = thread->create_socket (type_);
    if (!s)
        return NULL;

    term_sync.lock ();
    sockets++;
    term_sync.unlock ();

    return s;
}

void zmq::dispatcher_t::destroy_socket ()
{
    //  If zmq_term was already called and there are no more sockets,
    //  terminate the whole 0MQ infrastructure.
    term_sync.lock ();
    zmq_assert (sockets > 0);
    sockets--;
    bool destroy = (sockets == 0 && terminated);
    term_sync.unlock ();

    if (destroy)
       delete this;
}

void zmq::dispatcher_t::no_sockets (app_thread_t *thread_)
{
    app_threads_sync.lock ();
    app_threads_t::size_type i;
    for (i = 0; i != app_threads.size (); i++)
        if (app_threads [i].app_thread == thread_) {
            app_threads [i].associated = false;
            break;
        }
    zmq_assert (i != app_threads.size ());
    app_threads_sync.unlock ();
}

void zmq::dispatcher_t::write (int source_, int destination_,
    const command_t &command_)
{
    command_pipe_t &pipe =
        command_pipes [source_ * signalers.size () + destination_];
    pipe.write (command_);
    if (!pipe.flush ())
        signalers [destination_]->signal (source_);
}

bool zmq::dispatcher_t::read (int source_,  int destination_,
    command_t *command_)
{
    return command_pipes [source_ * signalers.size () +
        destination_].read (command_);
}

zmq::io_thread_t *zmq::dispatcher_t::choose_io_thread (uint64_t affinity_)
{
    //  Find the I/O thread with minimum load.
    zmq_assert (io_threads.size () > 0);
    int min_load = -1;
    io_threads_t::size_type result = 0;
    for (io_threads_t::size_type i = 0; i != io_threads.size (); i++) {
        if (!affinity_ || (affinity_ & (uint64_t (1) << i))) {
            int load = io_threads [i]->get_load ();
            if (min_load == -1 || load < min_load) {
                min_load = load;
                result = i;
            }
        }
    }
    zmq_assert (min_load != -1);
    return io_threads [result];
}

void zmq::dispatcher_t::register_pipe (class pipe_t *pipe_)
{
    pipes_sync.lock ();
    bool inserted = pipes.insert (pipe_).second;
    zmq_assert (inserted);
    pipes_sync.unlock ();
}

void zmq::dispatcher_t::unregister_pipe (class pipe_t *pipe_)
{
    pipes_sync.lock ();
    pipes_t::size_type erased = pipes.erase (pipe_);
    zmq_assert (erased == 1);
    pipes_sync.unlock ();
}

int zmq::dispatcher_t::register_endpoint (const char *addr_,
    socket_base_t *socket_)
{
    endpoints_sync.lock ();

    bool inserted = endpoints.insert (std::make_pair (addr_, socket_)).second;
    if (!inserted) {
        errno = EADDRINUSE;
        endpoints_sync.unlock ();
        return -1;
    }

    endpoints_sync.unlock ();
    return 0;
}

void zmq::dispatcher_t::unregister_endpoints (socket_base_t *socket_)
{
    endpoints_sync.lock ();

    endpoints_t::iterator it = endpoints.begin ();
    while (it != endpoints.end ()) {
        if (it->second == socket_) {
            endpoints_t::iterator to_erase = it;
            it++;
            endpoints.erase (to_erase);
            continue;
        }
        it++;
    }
        
    endpoints_sync.unlock ();
}

zmq::socket_base_t *zmq::dispatcher_t::find_endpoint (const char *addr_)
{
     endpoints_sync.lock ();

     endpoints_t::iterator it = endpoints.find (addr_);
     if (it == endpoints.end ()) {
         endpoints_sync.unlock ();
         errno = ECONNREFUSED;
         return NULL;
     }
     socket_base_t *endpoint = it->second;

     //  Increment the command sequence number of the peer so that it won't
     //  get deallocated until "bind" command is issued by the caller.
     //  The subsequent 'bind' has to be called with inc_seqnum parameter
     //  set to false, so that the seqnum isn't incremented twice.
     endpoint->inc_seqnum ();

     endpoints_sync.unlock ();
     return endpoint;
}

