using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication17
{
    class ConnectedRegion
    {
        public static List<List<Point>> GetRegions(Bitmap bitmap)
        {
            Bitmap temp = bitmap;
            bitmap = new Bitmap(temp.Width + 1, temp.Height + 1, PixelFormat.Format24bppRgb);
            Graphics graphics = null;
            try
            {
                graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.Transparent);
                graphics.DrawImage(temp, 1, 1, temp.Width, temp.Height);
            }
            finally
            {
                if (graphics != null)
                {
                    graphics.Dispose();
                }
            }

            int bag = 0;
            Dictionary<int, List<Point>> dic = new Dictionary<int, List<Point>>();

            for (int i = 1; i < bitmap.Width; i++)
            {
                for (int j = 1; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);
                    if (color.SameWith(KnownColor.White) == true)
                    {
                        Point top = new Point(i, j - 1);
                        Point left = new Point(i - 1, j);
                        int? topBag = null;
                        int? leftBag = null;
                        Color topColor = bitmap.GetPixel(i, j - 1);
                        Color leftColor = bitmap.GetPixel(i - 1, j);
                        if(topColor.SameWith(KnownColor.White) == true || leftColor.SameWith(KnownColor.White) == true)
                        {
                            foreach (var item in dic)
                            {
                                if (item.Value.Contains(top) == true)
                                {
                                    topBag = item.Key;
                                }
                                if (item.Value.Contains(left) == true)
                                {
                                    leftBag = item.Key;
                                }
                            }
                            if(topBag == null)
                            {
                                dic[leftBag.Value].Add(new Point(i, j));
                                continue;
                            }
                            if (leftBag == null)
                            {
                                dic[topBag.Value].Add(new Point(i, j));
                                continue;
                            }

                            if(topBag.Value == leftBag.Value)
                            {
                                dic[topBag.Value].Add(new Point(i, j));
                            }
                            else
                            {
                                int small = Math.Min(topBag.Value, leftBag.Value);
                                int larg = Math.Max(topBag.Value, leftBag.Value);
                                dic[small].Add(new Point(i, j));
                                dic[small].AddRange(dic[larg]);
                                dic.Remove(larg);
                            }
                        }
                        else
                        {
                            dic[bag] = new List<Point>() { new Point(i, j)};
                            bag++;
                        }
                    }
                }
            }

            return dic.Values.ToList();
        }
    }
}
