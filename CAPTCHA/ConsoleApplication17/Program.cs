using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication17
{
    class Program
    {
        static void Main(string[] args)
        {
            Bitmap bitmap = new Bitmap(@"D:\1.jpg");
            //Bitmap bitmap = new Bitmap(@"D:\1.png");
            bitmap = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format24bppRgb);

            BinarizationBase binarization = new Binarization3();
            bitmap = binarization.Binarize(bitmap);
            bitmap.Save(@"D:\result.png");

            List<List<Point>> regions = ConnectedRegion.Get4WayRegions(bitmap);
            int i = 10;
            foreach(var item in regions)
            {
                Bitmap single = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
                foreach(var innerItem in item)
                {
                    single.SetPixel(innerItem.X - 1, innerItem.Y - 1, Color.White);
                }
                single.Save(@"D:\" + i + ".png");
                single.Dispose();
                i++;
            }
        }
    }
}
