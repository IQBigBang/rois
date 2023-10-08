#include "core.h"
#include <stdlib.h>

void* CLOENV = 0;

// def main() -> void 
extern void GF_main_V();

int main() {
	// gc_init
	GF_main_V();
	return 0;
}

void __rtfail(const char* desc) {
	printf("Unexpected failure during execution.\nThis may be due to uncomplete pattern matching.\n");
	exit(1);
}