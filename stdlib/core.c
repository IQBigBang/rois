#include "core.h"

void* CLOENV = 0;

// def main() -> void 
extern void GF_main_V();

int main() {
	// gc_init
	GF_main_V();
	return 0;
}