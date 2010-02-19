#include <assert.h>
#include <stdio.h>
#include <stdlib.h>

#include <zmq.h>

int main()
{
	int rc;
	long long msg_id;
	void *ctx, *s;
	zmq_msg_t *msg;

	ctx = zmq_init (1, 1, 0);
	s = zmq_socket (ctx, ZMQ_PUB);
	rc = zmq_bind(s, "tcp://127.0.0.1:5555");
	assert (rc == 0);

	for (msg_id = 1; ; msg_id++)
	{
		msg = LocalAlloc(LMEM_FIXED, sizeof(zmq_msg_t));
		rc = zmq_msg_init_size(msg, 8);
		assert (rc == 0);
		memcpy(zmq_msg_data(msg), &msg_id, 8);
		if ((msg_id % 10000) == 0)
			printf("%lld\n", msg_id);
		rc = zmq_send (s, msg, 0);
		assert (rc == 0);
		zmq_msg_close(msg);
		LocalFree(msg);
	}
}