all: ../../dist/x86_Release/Palmtree.Math.Core.Uint.dll

clean:
	rm -f ../../dist/x86_Release/Palmtree.Math.Core.Uint.dll ../build/x86_Release/Palmtree.Math.Core.Uint.map
	rm -r -f 

test:
	notepad.exe
	notepad.exe

OBJS = 

../../dist/x86_Release/Palmtree.Math.Core.Uint.dll: $(OBJS)
	mkdir -p ../../dist/x86_Release
	gcc -o ../../dist/x86_Release/Palmtree.Math.Core.Uint.dll $(OBJS) -shared -lkernel32 -luser32 -mwindows -nostdlib -Wl,-e_DllMain@12 -Wl,-Map=../build/x86_Release/Palmtree.Math.Core.Uint.map

