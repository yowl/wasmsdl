using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ExampleWorld.wit.imports.wasi.webgpu;
using static ExampleWorld.wit.imports.wasi.webgpu.IWebgpu;
using ExampleWorld;
using AnimationFrameInterop = ExampleWorld.wit.imports.wasi.webgpu.AnimationFrameInterop;
using static ExampleWorld.wit.imports.wasi.io.v0_2_0.IPoll;
using ExampleWorld.wit.imports.wasi.io.v0_2_0;
using static ExampleWorld.wit.imports.wasi.webgpu.IAnimationFrame;

namespace wasmsdl
{
    unsafe class Program
    {
#if CODEGEN_WASM
        [UnmanagedCallersOnly(EntryPoint = "MainLoop", CallingConvention = CallingConvention.Cdecl)]
        static void MainLoop()
        {
            Render(renderer, texture);
        }
#endif

        static void Main()
        {
#if !CODEGEN_WASM
            var quit = false;
#endif
        }
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

namespace ExampleWorld
{
    public unsafe class ExampleWorldImpl : IExampleWorld
    {
        const uint Width = 640;
        const uint Height = 200;



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
            public fixed byte Data[(int)Width * (int)Height];
        }

        public static void Start()
        {
            rng = new MiniRandom(5005);

            // var gpu = WebgpuInterop.GetGpu();
            // var adapter = gpu.RequestAdapter(null);
            // var device = adapter.RequestDevice(null);

            var canvas = new IMiniCanvas.MiniCanvas(new IMiniCanvas.CreateDesc(Height, Width, false));
            Console.WriteLine("canvas");

            var graphicsContext = new IGraphicsContext.GraphicsContext();
            Console.WriteLine($"context");
            canvas.ConnectGraphicsContext(graphicsContext);
            Console.WriteLine("canvas ConnectGraphicsContext");

            // device.ConnectGraphicsContext(graphicsContext);

            var surface = new IFrameBuffer.Surface();
            Console.WriteLine("Surface");
            surface.ConnectGraphicsContext(graphicsContext);
            Console.WriteLine("Surface connected");

            var quit = false;

            InitFramebuff();
            Console.WriteLine("InitFramebuff");

            var frameListener = AnimationFrameInterop.Listener(canvas);
            var framePollable = frameListener.Subscribe();
            Console.WriteLine("framePollable");

            var pollables = new List<Pollable> { framePollable };

            Console.WriteLine("buffer");
            while (!quit)
            {
                var pollablesRes = PollInterop.Poll(pollables);
                if (pollablesRes.Contains(0u))
                {
                    //Console.WriteLine($"Contains {graphicsContext.Handle}");
                    var buffer = graphicsContext.GetCurrentBuffer();

                    var frameBuffer = IFrameBuffer.FrameBuffer.FromGraphicsBuffer(buffer);
                    var length = frameBuffer.Length();
                    //Console.WriteLine($"frameBuffer {length}");

                    Render(frameBuffer);

                    graphicsContext.Present();
                }
            }
        }


        private static void InitFramebuff()
        {
            for (int i = 0; i < Width; i++)
                firePixels.Data[(Height - 1) * Width + i] = 36;
        }

        static FirePixels firePixels;

        static MiniRandom rng;

        static void SpreadFire(uint src)
        {
            byte pixel = firePixels.Data[src];

            if (pixel == 0)
            {
                firePixels.Data[src - Width] = 0;
            }
            else
            {
                var rand = (int)rng.Next() & 3;
                var dst = (src - rand) + 1;
                firePixels.Data[dst - Width] = (byte)(pixel - (rand & 1));
            }
        }

        private static void RenderEffect(IFrameBuffer.FrameBuffer frameBuffer)
        {
            for (var x = 1u; x < Width; x++)
            {
                for (var y = 1u; y < Height; y++)
                {
                    SpreadFire(y * Width + x);
                }
            }

            // Convert palette buffer to RGB and write it to textureBuffer.
            for (var y = 0u; y < Height; y++)
            {
                for (var x = 0u; x < Width; x++)
                {
                    var pIx = y * Width + x;
                    var index = firePixels.Data[pIx];
                    frameBuffer.Set(pIx, (uint)((palette[index * 3 + 0] << 24)
                                         | (palette[index * 3 + 1] << 16)
                                         | (palette[index * 3 + 2] << 8)
                                         | 255));
                }
            }
        }

        private static void Render(IFrameBuffer.FrameBuffer frameBuffer)
        {
            RenderEffect(frameBuffer);
        }
    }
}