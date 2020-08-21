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

            for(int y = 0; y < 32; y++) {
                for(int x = 0; x < 64; x++) {
                    ((uint *)screenSurface->pixels)[y * 64 + x] = data[y * 64 + x] ? 0xffffffff : 0x000000ff;
                }
            }

            SDL.SDL_BlitScaled((IntPtr)screenSurface, ref srcRect, (IntPtr)windowSurface, ref dstRect);
            SDL.SDL_UpdateWindowSurface(emulatorWindow);
        }

        static void HandleKeyEvent(SDL.SDL_Keycode keyCode, Chip8 core, bool pressed) {
            switch(keyCode) {
                case SDL.SDL_Keycode.SDLK_1:
                    core.SetKey(1, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_2:
                    core.SetKey(2, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_3:
                    core.SetKey(3, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_4:
                    core.SetKey(12, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_a:
                    core.SetKey(4, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_z:
                    core.SetKey(5, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_e:
                    core.SetKey(6, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_r:
                    core.SetKey(13, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_q:
                    core.SetKey(7, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_s:
                    core.SetKey(8, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_d:
                    core.SetKey(9, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_f:
                    core.SetKey(14, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_w:
                    core.SetKey(10, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_x:
                    core.SetKey(0, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_c:
                    core.SetKey(11, pressed);
                    break;
                
                case SDL.SDL_Keycode.SDLK_v:
                    core.SetKey(15, pressed);
                    break;
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

            //Console.Clear();

            while(true) {
                core.FrameAdvance();

                SDL.SDL_Event e;
                while(SDL.SDL_PollEvent(out e) != 0) {
                    switch(e.type) {
                        case SDL.SDL_EventType.SDL_WINDOWEVENT:
                            if(e.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE) {
                                SDL.SDL_FreeSurface((IntPtr)screenSurface);
                                SDL.SDL_DestroyWindow(emulatorWindow);
                                SDL.SDL_Quit();
                                return;
                            }

                            break;

                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            HandleKeyEvent(e.key.keysym.sym, core, true);
                            break;

                        case SDL.SDL_EventType.SDL_KEYUP:
                            HandleKeyEvent(e.key.keysym.sym, core, false);
                            break;
                    }
                }

                SDL.SDL_Delay(16);
            }
        }
    }
}
