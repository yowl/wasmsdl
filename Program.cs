using System;
using System.Runtime.InteropServices;
//using SkiaSharp;

namespace wasmsdl
{
    unsafe class Program
    {
#if CODEGEN_WASM
        [DllImport("*")]
        internal static extern unsafe void emscripten_set_main_loop(delegate*<void> f, int fps, int simulate_infinite_loop);
#endif
        const int Width = 640;
        const int Height = 200;
        //static EMSCRIPTEN_WEBGL_CONTEXT_HANDLE glContext;
        static IntPtr glContext;

        // [StructLayout(LayoutKind.Sequential)]
        // struct ESContext
        // {
        //     /// Put your user data here...
        //     internal void* userData;
        //
        //     /// Window width
        //     int width;
        //
        //     /// Window height
        //     int height;
        //
        //     /// Window handle
        //     //EGLNativeWindowType hWnd;
        //     IntPtr hWnd;
        //
        //     /// EGL display
        //     //EGLDisplay eglDisplay;
        //     IntPtr eglDisplay;
        //
        //     /// EGL context
        //     IntPtr eglContext;
        //
        //     /// EGL surface
        //     IntPtr eglSurface;
        //
        //     /// Callbacks
        //     // void (ESCALLBACK* drawFunc) ( struct _escontext * );
        //     // void (ESCALLBACK* keyFunc) ( struct _escontext *, unsigned char, int, int );
        //     // void (ESCALLBACK* updateFunc) ( struct _escontext *, float deltaTime );
        //     
        //     IntPtr drawFunc;
        //     IntPtr keyFunc;
        //     IntPtr updateFunc;
        // };

        struct UserData
        {
            // Handle to a program object
            uint programObject;

        };

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

        struct Buffer
        {
            public fixed int TextureBuffer[Width * Height];
        }

        static FirePixels firePixels;
        static Buffer buffer;

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

        private static void RenderEffect()
        {
            for (int x = 1; x < Width; x++)
            {
                for (int y = 1; y < Height; y++)
                {
                    SpreadFire(y * Width + x);
                }
            }

            // var texBuffer = (int*)(skImage.GetPixels());
            // // Convert palette buffer to RGB and write it to textureBuffer.
            // for (var y = 0; y < Height; y++)
            // {
            //     for (var x = 0; x < Width; x++)
            //     {
            //         var pIx = y * Width + x;
            //         var index = firePixels.Data[pIx];
            //         texBuffer[pIx] = (255 << 24)
            //                          | (palette[index * 3 + 0] << 16)
            //                          | (palette[index * 3 + 1] << 8)
            //                          | palette[index * 3 + 2];
            //
            //     }
            // }
        }

        private static void Render() //IntPtr renderer, IntPtr texture)
        {
            RenderEffect();

//            canvas.DrawBitmap(skImage, 0, 0); // at top left
            // need something like this?  SDL.SDL_RenderPresent(renderer);
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
            Render(); //enderer, texture);
        }
#endif

        [StructLayout(LayoutKind.Sequential)]
        struct EmscriptenWebGLContextAttributes
        {
            int alpha;
            int depth;
            int stencil;
            int antialias;
            int premultipliedAlpha;
            int preserveDrawingBuffer;
            int powerPreference;
            int failIfMajorPerformanceCaveat;
        
            internal int majorVersion;
            int minorVersion;
        
            int enableExtensionsByDefault;
            int explicitSwapControl;
            int proxyContextToMainThread;
            int renderViaOffscreenBackBuffer;
        };
        
        
        [DllImport("*")]
        static extern IntPtr emscripten_webgl_create_context(byte* s, EmscriptenWebGLContextAttributes* attr);
        [DllImport("*")]
        static extern void emscripten_webgl_init_context_attributes(EmscriptenWebGLContextAttributes* attr);
        [DllImport("*")]
        static extern void emscripten_webgl_make_context_current(IntPtr context);

        // "#canvas"  in the DOM
        static ReadOnlySpan<byte> s => new byte[]
        {
            0x23,
            0x63,0x61,0x6E,0x76, 0x61, 0x73,
            0x0
        };

        // static SKCanvas canvas;
        // static SKBitmap skImage;
        static void Main()
        {
#if !CODEGEN_WASM
            var quit = false;
#endif
            rng = new MiniRandom(5005);

            //skImage = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul));

            EmscriptenWebGLContextAttributes attrs;
            emscripten_webgl_init_context_attributes(&attrs);
            attrs.majorVersion = 2;
            fixed (byte* n = &(s[0]))
            {
                glContext = emscripten_webgl_create_context(n, &attrs);
            }
            emscripten_webgl_make_context_current(glContext);
	    emscripten_webgl_commit_frame  
            // if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            // {
            //     Console.WriteLine($"Unable to initialize SDL.");
            //     return;
            // }
            //
            // byte* title = null;
            //
            // var window = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            //     Width, Height, 0);
            // if (window == IntPtr.Zero)
            // {
            //     Console.WriteLine($"Unable to create window.");
            //     return;
            // }

#if CODEGEN_WASM

            emscripten_set_main_loop(&MainLoop, 0, 0);
#endif
            // renderer = SDL.SDL_CreateRenderer(window, 0, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            // if (renderer == IntPtr.Zero)
            // {
            //     Console.WriteLine($"Unable to create renderer.");
            //     return;
            // }
            // texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, Width, Height);
            //
            InitFramebuff();
#if !CODEGEN_WASM

            // while (!quit)
            // {
            //     Render(renderer, texture);
            // }
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
