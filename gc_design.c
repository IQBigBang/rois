/*


*/

typedef void marking_func_t(void);
typedef void delete_func_t(void);

struct gc_header_t {
	marking_func_t mark;
	delete_func_t del;
};