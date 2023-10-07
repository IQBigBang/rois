## Rois
Rois is an experimental language inspired by Rust and Haskell.

### Getting started
To run the compiler you need a [C# environment](https://dotnet.microsoft.com/en-us/download). To actually run the code, you need a C compiler.
You can get easily get started by downloading the [ris.py](https://github.com/IQBigBang/rois/blob/main/ris/ris.py) script from the repository and running it:
```
> cd my_test_folder
> python ./ris.py init .
```

### Features
Current and future features:

 - [X] Basic arithmetic
 - [X] Control flow (if, while)
	 - [ ] Looping construct (for) 
 - [X] Pattern matching
 - [X] C FFI
 - [X] Classes and methods
	 - [ ] Access modifiers (private),  custom constructors
 - [ ] Strings (WIP)
 - [ ] List, Map, Optional
 - [ ] Sum types/Enum classes/ADTs
 - [ ] GC
 - [ ] Polymorphism
 - [ ] SELF-HOSTING COMPILER

### LSP

A syntax highlighter with trivial integration of compiler errors
is implemented in the `lsp-extension` folder.

### Example program

```
include Str

def fib(n: int) -> int:
	match n:
		0 -> return 1
		1 -> return 1
		_ -> return fib(n-1) + fib(n - 2)

def main():
	let x = fib(25)
	print("The result is ".join(itoa(x)))
```