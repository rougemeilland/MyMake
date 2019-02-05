all: ../../dist/x64_Release/Palmtree.Math.Core.Uint.dll

clean:
	rm -f ../../dist/x64_Release/Palmtree.Math.Core.Uint.dll ../build/x64_Release/Palmtree.Math.Core.Uint.map
	rm -r -f 

test:
	notepad.exe
	notepad.exe

OBJS = 

../../dist/x64_Release/Palmtree.Math.Core.Uint.dll: $(OBJS)
	mkdir -p ../../dist/x64_Release
	gcc -o ../../dist/x64_Release/Palmtree.Math.Core.Uint.dll $(OBJS) -shared -lkernel32 -luser32 -mwindows -nostdlib -Wl,-eDllMain -Wl,-Map=../build/x64_Release/Palmtree.Math.Core.Uint.map

