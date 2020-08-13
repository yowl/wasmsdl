using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SkiaFireWpf;
using SkiaSharp;

namespace SkiaFire
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        const int Width = 640;
        const int Height = 200;

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

        static DispatcherTimer t;
        SKSurface surface;
            SKCanvas canvas;
        WriteableBitmap bitmap;
        SKBitmap skImage;
        public MainWindow()
        {
            InitFramebuff();
            bitmap = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32, null);
            surface = SKSurface.Create(
                width: Width,
                height: Height,
                colorType: SKColorType.Bgra8888,
                alphaType: SKAlphaType.Premul);
            canvas = surface.Canvas;
            skImage = new SKBitmap(new SKImageInfo(Width, Height, SKColorType.Rgba8888, SKAlphaType.Premul));
            InitializeComponent();
            img.Source = bitmap;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            t = new DispatcherTimer(TimeSpan.FromMilliseconds(18), DispatcherPriority.Normal,  Render, Dispatcher.CurrentDispatcher);
        }

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
                var rand = (int)rng.Next() & 3;
                var dst = (src - rand) + 1;
                firePixels.Data[dst - Width] = (byte)(pixel - (rand & 1));
            }
        }

        private  void RenderEffect()
        {
            for (int x = 1; x < Width; x++)
            {
                for (int y = 1; y < Height; y++)
                {
                    SpreadFire(y * Width + x);
                }
            }

            // Convert palette buffer to RGB and write it to textureBuffer.
            unsafe
            {
                for (var y = 0; y < Height; y++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var pIx = y * Width + x;
                        var index = firePixels.Data[pIx];
                        var texBuffer = (int*) (skImage.GetPixels());
                        texBuffer[pIx] = (palette[index * 3 + 0] << 24)
                                         | (palette[index * 3 + 1] << 16)
                                         | (palette[index * 3 + 2] << 8)
                                         | 255;
                    }
                }
            }
        }

        private void Render(object? state, EventArgs eventArgs)
        {
            // fixed (int* bPtr = buffer.TextureBuffer)
            // {
            //     SDL.SDL_UpdateTexture(texture, IntPtr.Zero, (IntPtr)bPtr, Width * sizeof(int));
            // }
            // SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
            // SDL.SDL_RenderPresent(renderer);

//                bitmap.Lock();

                RenderEffect();

            // copy buffer to bitmap
            // for (var y = 0; y < Height; y++)
            // {
            //     for (var x = 0; x < Width; x++)
            //     {
            //         buffer.TextureBuffer[pIx] = (palette[index * 3 + 0] << 24)
            //                                     | (palette[index * 3 + 1] << 16)
            //                                     | (palette[index * 3 + 2] << 8)
            //                                     | 255;
            //     }
            // }
            canvas.DrawBitmap(skImage, 0, 0);
            canvas.
            var snapshot = surface.Snapshot();
            using (SKData data = snapshot.Encode(SKEncodedImageFormat.Png, 100))
            {
                var image = new BitmapImage();
                using (var mem = new MemoryStream(data.ToArray()))
                {
                    mem.Position = 0;
                    image.BeginInit();
                    image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = null;
                    image.StreamSource = mem;
                    image.EndInit();
                }
                image.Freeze();
                img.Source = bitmap;
            }
            // bitmap.AddDirtyRect(new Int32Rect(0, 0, Width, Height));
            //
            // bitmap.Unlock();
        }

        private static void InitFramebuff()
        {
            for (int i = 0; i < Width; i++)
                firePixels.Data[(Height - 1) * Width + i] = 36;
        }

    }
}
