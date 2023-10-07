#include "core.h"

// extern def str_len_c(p: ptr) -> int
I32 str_len_c(PTR p) {
    return ((str)p)->len;
}

// extern def str_print_c(p: ptr)
void str_print_c(PTR p) {
    str s = (str)p;
    fwrite(&s->chars[0], 1, s->len, stdout);
    fputc('\n', stdout);
}

// extern def itoa_c(n: int) -> ptr
PTR itoa_c(I32 n) {
    // allocate 16 bytes = 4 length prefix + 12 number (highest value 4294967295 - 10 chars)
    str s = (str)calloc(1, 16);
    int charsCount = snprintf(&s->chars[0], 12, "%d", n);
    s->len = charsCount;
    return (PTR)s;
}

PTR str_join_c(PTR a, PTR b) {
    int totalLen = ((str)a)->len + ((str)b)->len;
    str result = (str)calloc(totalLen + 4, 1);
    result->len = totalLen;
    memcpy(&result->chars[0], &((str)a)->chars[0], ((str)a)->len);
    memcpy(&result->chars[((str)a)->len], &((str)b)->chars[0], ((str)b)->len);
    return result;
}
