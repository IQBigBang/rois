#ifndef CORE_H
#define CORE_H

#include <stdint.h>

#define I32 int32_t
#define bool int32_t
#define PTR void*
#define CHAR int32_t

#define true ((bool)1)
#define false ((bool)0)

#define null ((void*)0)

typedef void* ANYREF;

extern void* CLOENV;

void __attribute__ ((noinline)) __rtfail(const char* desc);

int main();

#endif /* CORE_H */
