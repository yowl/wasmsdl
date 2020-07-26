using System;
using SDL2;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnmanagedCallersOnlyAttribute : Attribute
    {
        /// </summary>
        public CallingConvention CallingConvention;

        /// <summary>
        /// Optional. If omitted, no named export is emitted during compilation.
        /// </summary>
        public string? EntryPoint;
    }
}

namespace wasmsdl
{
    unsafe class Program
    {
#if CODEGEN_WASM
        [DllImport("*")]
        internal static extern unsafe void emscripten_set_main_loop(delegate*<void> f, int fps, int
            simulate_infinite_loop);
#endif
        const int Width = 320;
        const int Height = 180;

        static ReadOnlySpan<byte> palette => new byte[]
        {
            0x07, 0x07, 0x07,
            0x1F, 0x07, 0x07,
            0x2F, 0x0F, 0x07,
            0x47, 0x0F, 0x07,
            0x57, 0x17, 0x07,
            0x67, 0x1F, 0x07,
            0x77, 0x1F, 0x07,
            0x8F, 0x27, 0x07,
            0x9F, 0x2F, 0x07,
            0xAF, 0x3F, 0x07,
            0xBF, 0x47, 0x07,
            0xC7, 0x47, 0x07,
            0xDF, 0x4F, 0x07,
            0xDF, 0x57, 0x07,
            0xDF, 0x57, 0x07,
            0xD7, 0x5F, 0x07,
            0xD7, 0x5F, 0x07,
            0xD7, 0x67, 0x0F,
            0xCF, 0x6F, 0x0F,
            0xCF, 0x77, 0x0F,
            0xCF, 0x7F, 0x0F,
            0xCF, 0x87, 0x17,
            0xC7, 0x87, 0x17,
            0xC7, 0x8F, 0x17,
            0xC7, 0x97, 0x1F,
            0xBF, 0x9F, 0x1F,
            0xBF, 0x9F, 0x1F,
            0xBF, 0xA7, 0x27,
            0xBF, 0xA7, 0x27,
            0xBF, 0xAF, 0x2F,
            0xB7, 0xAF, 0x2F,
            0xB7, 0xB7, 0x2F,
            0xB7, 0xB7, 0x37,
            0xCF, 0xCF, 0x6F,
            0xDF, 0xDF, 0x9F,
            0xEF, 0xEF, 0xC7,
            0xFF, 0xFF, 0xFF
        };

        struct FirePixels
        {
            public fixed byte Data[Width * Height];
        }

        static FirePixels firePixels;

        static MiniRandom rng;

        static void SpreadFire(int src)
        {
            byte pixel = firePixels.Data[src];

            if (pixel == 0)
            {
                firePixels.Data[src - Width] = 0;
            }
            else
            {
                var rand = (int) rng.Next() & 3;
                var dst = (src - rand) + 1;
                firePixels.Data[dst - Width] = (byte) (pixel - (rand & 1));
            }
        }

        private static void RenderEffect(uint tick, IntPtr renderer)
        {
            for (int x = 1; x < Width; x++)
            {
                for (int y = 1; y < Height; y++)
                {
                    SpreadFire(y * Width + x);
                }
            }

            // Convert palette buffer to RGB and write it to output.
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var index = firePixels.Data[y * Width + x];
                    //Console.WriteLine($"draw point {palette[index * 3 + 0]} {palette[index * 3 + 1]} {palette[index * 3 + 2]}");
                    SDL.SDL_SetRenderDrawColor(renderer, palette[index * 3 + 0],  palette[index * 3 + 1], palette[index * 3 + 2], 255);
                    SDL.SDL_RenderDrawPoint(renderer, x, y);
                }
            }
        }

        private static void Render(IntPtr renderer, IntPtr texture)
        {
            SDL.SDL_SetRenderTarget(renderer, texture);
            // SDL.SDL_Rect r;
            // r.w = 10;
            // r.h = 10;
            // r.x = (int)rng.Next() % (Width - 20);
            // r.y = (int)rng.Next() % (Width - 20);
            //
            // SDL.SDL_SetRenderDrawColor(renderer, 0x00, 0x00, 0x00, 0x00);
            // SDL.SDL_RenderClear(renderer);
            // SDL.SDL_RenderDrawRect(renderer, ref r);
            // SDL.SDL_SetRenderDrawColor(renderer, 0xFF, 0x00, 0x00, 0x00);
            // SDL.SDL_RenderFillRect(renderer, ref r);

            RenderEffect(0 /* not used, was GetTickCount */, renderer);

            SDL.SDL_SetRenderTarget(renderer, IntPtr.Zero);
            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(renderer);

        }

        private static void InitFramebuff()
        {
            for (int i = 0; i < Width; i++)
                firePixels.Data[(Height - 1) * Width + i] = 36;
        }
#if CODEGEN_WASM
        [UnmanagedCallersOnly(EntryPoint = "MainLoop", CallingConvention = CallingConvention.Cdecl)]
        static void MainLoop()
        {
            Render(renderer, texture);
        }
#endif
        static IntPtr renderer;
        static IntPtr texture;
        static void Main()
        {
#if !CODEGEN_WASM
            var quit = false;
#endif
            rng = new MiniRandom(5005);

            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                Console.WriteLine($"Unable to initialize SDL. Error {SDL.SDL_GetError()}");
                return;
            }

            var window = SDL.SDL_CreateWindow("CoreRT Wasm SDL", SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                Width, Height, 0);
            if (window == IntPtr.Zero)
            {
                Console.WriteLine($"Unable to create window. Error {SDL.SDL_GetError()}");
                return;
            }

            renderer = SDL.SDL_CreateRenderer(window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (renderer == IntPtr.Zero)
            {
                Console.WriteLine($"Unable to create renderer. Error {SDL.SDL_GetError()}");
                return;
            }
            //texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING | (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, Width, Height);
            texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, Width, Height);
            // SDL.SDL_Event e;
            // var quit = false;

            SDL.SDL_RenderClear(renderer);
            SDL.SDL_SetRenderDrawColor(renderer, 7, 7, 7, 255);
            SDL.SDL_RenderPresent(renderer);

            InitFramebuff();
#if CODEGEN_WASM

            emscripten_set_main_loop(&MainLoop, 0, 0);
#else
            while (!quit)
            {
                SDL.SDL_Event e;
                while (SDL.SDL_PollEvent(out e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            quit = true;
                            break;
                        case SDL.SDL_EventType.SDL_KEYDOWN:
                            switch (e.key.keysym.sym)
                            {
                                case SDL.SDL_Keycode.SDLK_q:
                                    quit = true;
                                    break;
                            }
                            break;
                    }
                }
                Render(renderer, texture);
            }
            SDL.SDL_DestroyTexture(texture);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
#endif
        }

#if CODEGEN_WASM
    internal class Console
    {
        private static unsafe void PrintString(string s)
        {
            int length = s.Length;
            fixed (char* curChar = s)
            {
                for (int i = 0; i < length; i++)
                {
                    TwoByteStr curCharStr = new TwoByteStr();
                    curCharStr.first = (byte)(*(curChar + i));
                    printf((byte*)&curCharStr, null);
                }
            }
        }

        internal static void WriteLine(string s)
        {
            PrintString(s);
            PrintString("\n");
        }
    }

    struct TwoByteStr
    {
        public byte first;
        public byte second;
    }

    [DllImport("*")]
    private static unsafe extern int printf(byte* str, byte* unused);
#endif
    }
}
