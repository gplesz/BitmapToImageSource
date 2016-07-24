using BenchmarkDotNet.Attributes;
using BitmapToImageSource;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace BitmapToImageSource.Benchmarks
{
    public class BitmapToImageSourceTests
    {
        Bitmap bitmap;

        [Setup]
        public void SetupData()
        {
            bitmap = new Bitmap(1920, 1080);
        }

        [Benchmark]
        public BitmapImage TestBitmapToImageSource1()
        {
            return bitmap.ToBitmapImage();
        }

        [Benchmark]
        public BitmapImage TestBitmapToImageSource2()
        {
            return bitmap.ToBitmapImage2();
        }
    }
}
