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
using OpenTK.Graphics.ES11;
using SkiaFireWpf;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace SkiaFireWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        const int iWidth = 640;
        const int iHeight = 200;

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
            rng = new MiniRandom(5005);

            InitFramebuff();
            surface = SKSurface.Create(
                width: iWidth,
                height: iWidth,
                colorType: SKColorType.Bgra8888,
                alphaType: SKAlphaType.Premul);
            canvas = surface.Canvas;
            skImage = new SKBitmap(new SKImageInfo(iWidth, iHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
            InitializeComponent();
            skElement.PaintSurface += SkElementOnPaintSurface;

        }

        void SkElementOnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;

            canvas.DrawBitmap(skImage, 0, 0);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            t = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Normal, Render, Dispatcher.CurrentDispatcher);
        }

        struct FirePixels
        {
            public fixed byte Data[iWidth * iHeight];
        }

        static FirePixels firePixels;

        static MiniRandom rng;

        static void SpreadFire(int src)
        {
            byte pixel = firePixels.Data[src];

            if (pixel == 0)
            {
                firePixels.Data[src - iWidth] = 0;
            }
            else
            {
                var rand = (int)rng.Next() & 3;
                var dst = (src - rand) + 1;
                firePixels.Data[dst - iWidth] = (byte)(pixel - (rand & 1));
            }
        }

        private void RenderEffect()
        {
            for (int x = 1; x < iWidth; x++)
            {
                for (int y = 1; y < iHeight; y++)
                {
                    SpreadFire(y * iWidth + x);
                }
            }

            var texBuffer = (int*)(skImage.GetPixels());
            // Convert palette buffer to RGB and write it to textureBuffer.
            for (var y = 0; y < iHeight; y++)
            {
                for (var x = 0; x < iWidth; x++)
                {
                    var pIx = y * iWidth + x;
                    var index = firePixels.Data[pIx];
                    texBuffer[pIx] = (255 << 24)
                                     | (palette[index * 3 + 0] << 16)
                                     | (palette[index * 3 + 1] << 8)
                                     | palette[index * 3 + 2];

                }
            }
        }

        private void Render(object state, EventArgs eventArgs)
        {

            RenderEffect();
            skElement.InvalidateVisual();
        }

        private static void InitFramebuff()
        {
            for (int i = 0; i < iWidth; i++)
                firePixels.Data[(iHeight - 1) * iWidth + i] = 36;
        }

    }
}
