using System;
using System.IO;
using SDL2;

namespace C8Emu
{
    class C8Emu
    {
        unsafe static SDL.SDL_Surface *screenSurface;
        unsafe static SDL.SDL_Surface *windowSurface;
        static IntPtr emulatorWindow;

        static byte[] LoadFile(string fileName) {
            FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            fs.Close();

            return buffer;
        }

        static unsafe void DisplayUpdate(bool[] data) {
            SDL.SDL_Rect srcRect = new SDL.SDL_Rect();
            srcRect.x = 0;
            srcRect.y = 0;
            srcRect.w = 64;
            srcRect.h = 32;

            SDL.SDL_Rect dstRect = new SDL.SDL_Rect();
            dstRect.x = 0;
            dstRect.y = 0;
            dstRect.w = 256;
            dstRect.h = 128;

            SDL.SDL_BlitScaled((IntPtr)screenSurface, ref srcRect, (IntPtr)windowSurface, ref dstRect);
            SDL.SDL_UpdateWindowSurface(emulatorWindow);

            Console.SetCursorPosition(0, 0);

            for(int y = 0; y < 32; y++) {
                for(int x = 0; x < 64; x++) {
                    int index = y * 64 + x;

                    if(data[index]) {
                        Console.Write('#');
                    } else {
                        Console.Write(' ');
                    }
                }

                Console.WriteLine();
            }
        }

        static unsafe void Main(string[] args)
        {
            Chip8 core = new Chip8();

            if(args.Length != 1) {
                Console.WriteLine("Usage: c8emu <ROM file name>");
                return;
            }

            core.LoadROM(LoadFile(args[0]));

            if(SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0) {
                Console.WriteLine("Failed to initialize SDL.");
                return;
            }

            emulatorWindow = SDL.SDL_CreateWindow("c8emu", SDL.SDL_WINDOWPOS_UNDEFINED, SDL.SDL_WINDOWPOS_UNDEFINED, 256, 128, SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

            if(emulatorWindow == null) {
                Console.WriteLine("Failed to create the window.");
                SDL.SDL_Quit();
                return;
            }

            windowSurface = (SDL.SDL_Surface *)SDL.SDL_GetWindowSurface(emulatorWindow);

            screenSurface = (SDL.SDL_Surface *)SDL.SDL_CreateRGBSurface(SDL.SDL_SWSURFACE, 64, 32, 32, 0xff000000, 0x00ff0000, 0x0000ff00, 0x000000ff);

            if(screenSurface == null) {
                Console.WriteLine("Failed to create the screen surface.");
                SDL.SDL_DestroyWindow(emulatorWindow);
                SDL.SDL_Quit();
                return;
            }

            core.DisplayUpdate += DisplayUpdate;

            Console.Clear();

            while(true) {
                core.FrameAdvance();
                SDL.SDL_Delay(16);
            }
        }
    }
}
