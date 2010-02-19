#include <stdio.h>
#include <zmq.h>

void main()
{
	printf("%i\n", sizeof(zmq_msg_t));
}