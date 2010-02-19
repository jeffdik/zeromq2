#include <assert.h>
#include <stdio.h>
#include <stdlib.h>

#include <zmq.h>

int main()
{
	int rc;
	long long msg_id;
	void *ctx, *s;
	zmq_msg_t msg;

	ctx = zmq_init (1, 1, 0);
	s = zmq_socket (ctx, ZMQ_SUB);
	zmq_setsockopt (s, ZMQ_SUBSCRIBE, "", 0);
	zmq_connect (s, "tcp://127.0.0.1:5555");

	while (TRUE) {
		rc = zmq_msg_init (&msg);
		assert (rc == 0);
		rc = zmq_recv(s, &msg, 0);
		assert (rc == 0);
		//memcpy(&msg_id, zmq_msg_data(&msg), 8);
		msg_id = *((long long*) zmq_msg_data(&msg));
		if ((msg_id % 10000) == 0)
			printf("msg_id: %lld\n", msg_id);
		//printf("%d\n", zmq_msg_size(&msg));

		zmq_msg_close (&msg);
	}
}