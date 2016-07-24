using BenchmarkDotNet.Attributes;
using System.Drawing;

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
        public void TestBitmapToImageSource1()
        {

        }
    }
}
