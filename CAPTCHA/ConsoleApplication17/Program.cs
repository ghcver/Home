using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication17
{
    class Program
    {
        static void Main(string[] args)
        {
            Bitmap bitmap = new Bitmap(@"D:\1.png");
            //Bitmap bitmap = new Bitmap(@"D:\1.jpg");
            BinarizationBase binarization = new Binarization3();
            bitmap = binarization.Binarize(bitmap);
            bitmap.Save(@"D:\result.png");
        }
    }
}
