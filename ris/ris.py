import argparse
import subprocess
import os
import shutil
import sys

REPO = 'https://github.com/IQBigBang/rois.git'
BRANCH = 'c-backend'
BOILERPLATE_MAKEFILE = """
.PHONY: run build all
all: build
run: build
\t./$(OUTNAME)
build: $(OUTNAME)

$(OUTNAME): .ris/obj/output.o
\tCC .ris/obj/output.o .ris/std/libstdrois.a -o $(OUTNAME)

.ris/obj/output.o: $(MAIN)
\t.ris/bin/RoisLang.exe $(MAIN) -o .ris/obj/output.c
\t$(CC) .ris/obj/output.c -g -O1 -c -o .ris/obj/output.o
"""


def color_red(text):
    return "\x1b[31m" + text + "\x1b[0m"


def color_green(text):
    return "\x1b[32m" + text + "\x1b[0m"


def stdlib_build(ris_dir, cc):
    # Check prerequisites
    if subprocess.run([cc, "--version"], capture_output=True).returncode != 0:
        print(color_red("error") + ": failed to find `" + cc + "`")
        sys.exit(1)
    if subprocess.run(["ar", "--version"], capture_output=True).returncode != 0:
        print(color_red("error") + ": failed to find `ar`")
        sys.exit(1)

    stdlibPath = os.path.join(ris_dir, 'rois', 'stdlib')
    # Compile all c files
    n = 1
    for f in os.listdir(stdlibPath):
        if f.endswith('.c'):
            cmd = [cc, '-g', '-O1', '-c', f, '-o', str(n) + '.o']
            n += 1
            print(' '.join(cmd))
            # Run the command
            if subprocess.run(cmd, cwd=stdlibPath).returncode != 0:
                print(color_red("error") + ": failed to build standard library")
                sys.exit(1)
    # Create the `std` folder in risDir
    os.makedirs(os.path.join(ris_dir, 'std'), exist_ok=True)
    # Link the standard library files into an archive
    if subprocess.run(['ar', 'rc', '../../std/libstdrois.a', '*.o'], cwd=stdlibPath).returncode != 0:
        print(color_red("error") + ": failed to build standard library")
        sys.exit(1)
    # Move the *.ro files into the main folder
    for f in os.listdir(stdlibPath):
        if f.endswith('.ro'):
            os.rename(os.path.join(stdlibPath, f), os.path.join(ris_dir, '..', os.path.basename(f)))


# Download and build Rois
def do_download(path, keep_compiler, cc):
    if cc is None:
        cc = input("Enter C compiler name (gcc): ")
        if cc == "":
            cc = "gcc"
    path = os.path.abspath(path)
    risDir = os.path.join(path, '.ris')
    if os.path.exists(risDir):
        print(color_red("error") + ": ris is already initialized in this folder")
        sys.exit(1)
    # Create .ris
    os.makedirs(risDir)
    # Check all prerequisites
    if subprocess.run(["git", "--version"], capture_output=True).returncode != 0:
        print(color_red("error") + ": failed to find `git`")
        sys.exit(1)
    if subprocess.run(["dotnet", "--version"], capture_output=True).returncode != 0:
        print(color_red("error") + ": failed to find `dotnet`")
        sys.exit(1)

    # Clone the repo
    if subprocess.run(["git", "clone", '-b', BRANCH, REPO], cwd=risDir).returncode != 0:
        print(color_red("error") + ": failed to clone the `rois` repo")
        sys.exit(1)

    # Save information about how recent the clone is
    gitLogOut = subprocess.run(["git", "log", '--format=%H', "-n1"], capture_output=True, encoding='utf8')
    with open(os.path.join(risDir, 'gitversion.txt'), 'w', encoding='utf8') as f:
        f.write(gitLogOut.stdout.strip())

    # Build the project
    if subprocess.run(["dotnet", "build", "RoisLang.csproj", "-c", "Release", "-o", os.path.join(risDir, 'bin')],
                      cwd=os.path.join(risDir, 'rois')).returncode != 0:
        print(color_red("error") + ": failed to build RoisLang")
        sys.exit(1)

    # Build the standard library
    stdlib_build(risDir, cc)

    # Remove the repo
    if not keep_compiler:
        shutil.rmtree(os.path.join(risDir, 'rois'), ignore_errors=True)
    print(color_green("Rois compiler successfully installed"))


def do_init(path, no_download, keep_compiler):
    cc = input("Enter C compiler name (gcc): ")
    if cc == "":
        cc = "gcc"
    if not no_download:
        do_download(path, keep_compiler, cc)
    main_file_name = input("Enter main file name (main.ro): ")
    if main_file_name == "":
        main_file_name = "main.ro"
    program_name = input("Enter output executable file name (program): ")
    if program_name == "":
        program_name = "program"
    # Create the Makefile
    with open(os.path.join(path, 'Makefile'), 'w', encoding='utf8') as f:
        f.write(f'CC={cc}\n')
        f.write(f'MAIN={main_file_name}\n')
        f.write(f'OUTNAME={program_name}\n')
        f.write(BOILERPLATE_MAKEFILE)
    print(color_green("Initialization finished"))

parser = argparse.ArgumentParser(description='Rois Installation System')
subparsers = parser.add_subparsers(help='which action to proceed with', dest='action', required=True)
parser_download = subparsers.add_parser('download', help='download the Rois compiler')
parser_download.add_argument('path', help='path to the working folder')
parser_download.add_argument('--keep-compiler', action='store_true', help='don\'t delete the compiler sources')
parser_init = subparsers.add_parser('init', help='download and initialize Rois in this folder')
parser_init.add_argument('path', help='path to the working folder')
parser_init.add_argument('--no-download', action='store_true', help='don\'t download the compiler')
parser_init.add_argument('--keep-compiler', action='store_true', help='don\'t delete the compiler sources')

args = parser.parse_args()
if args.action == 'download':
    do_download(args.path, args.keep_compiler, None)
elif args.action == 'init':
    do_init(args.path, args.no_download, args.keep_compiler)
