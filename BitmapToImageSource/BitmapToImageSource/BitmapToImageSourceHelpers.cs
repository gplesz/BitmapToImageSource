using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BitmapToImageSource
{
    public static class BitmapToImageSourceHelpers
    {
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Bmp);
                //memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        //létrehozunk egy osztályszintű MemoryStream-et
        private readonly static MemoryStream memoryStream = new MemoryStream();
        //ezzel pedig megakadályozzuk, hogy párhuzamosan két szálon egymás streamjéhez hozzáférjenek
        private readonly static object memoryLock = new object();

        public static BitmapImage ToBitmapImage2(this Bitmap bitmap)
        {

            lock (memoryLock)
            {
                bitmap.Save(memoryStream, ImageFormat.Bmp);
                //memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}
