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
        public static List<List<Point>> Get4WayRegions(Bitmap bitmap)
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
            List<KeyValuePair<Point, int>> previousLine = new List<KeyValuePair<Point, int>>();
            List<KeyValuePair<Point, int>> currentLine = new List<KeyValuePair<Point, int>>();

            for (int j = 1; j < bitmap.Height; j++)
            {
                for (int i = 1; i < bitmap.Width; i++)
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
                        if (topColor.SameWith(KnownColor.White) == true)
                        {
                            foreach(var item in previousLine)
                            {
                                if (item.Key == top)
                                {
                                    topBag = item.Value;
                                    break;
                                }
                            }
                        }
                        if (leftColor.SameWith(KnownColor.White) == true)
                        {
                            leftBag = currentLine.Last().Value;
                        }

                        if(topBag == null && leftBag == null)
                        {
                            dic[bag] = new List<Point>() { new Point(i, j) };
                            currentLine.Add(new KeyValuePair<Point, int>(new Point(i, j), bag));
                            bag++;
                        }
                        else
                        {
                            if (topBag == null)
                            {
                                dic[leftBag.Value].Add(new Point(i, j));
                                currentLine.Add(new KeyValuePair<Point, int>(new Point(i, j), leftBag.Value));
                                continue;
                            }
                            if (leftBag == null)
                            {
                                dic[topBag.Value].Add(new Point(i, j));
                                currentLine.Add(new KeyValuePair<Point, int>(new Point(i, j), topBag.Value));
                                continue;
                            }
                            if (topBag.Value == leftBag.Value)
                            {
                                dic[topBag.Value].Add(new Point(i, j));
                                currentLine.Add(new KeyValuePair<Point, int>(new Point(i, j), topBag.Value));
                            }
                            else
                            {
                                int small = Math.Min(topBag.Value, leftBag.Value);
                                int larg = Math.Max(topBag.Value, leftBag.Value);
                                dic[small].AddRange(dic[larg]);
                                dic.Remove(larg);
                                for(int k = 0; k < previousLine.Count; k++)
                                {
                                    if(previousLine[k].Value == larg)
                                    {
                                        previousLine[k] = new KeyValuePair<Point, int>(previousLine[k].Key, small);
                                    }
                                }
                                for (int k = 0; k < currentLine.Count; k++)
                                {
                                    if (currentLine[k].Value == larg)
                                    {
                                        currentLine[k] = new KeyValuePair<Point, int>(currentLine[k].Key, small);
                                    }
                                }

                                dic[small].Add(new Point(i, j));
                                currentLine.Add(new KeyValuePair<Point, int>(new Point(i, j), small));
                            }
                        }
                    }
                }

                List<KeyValuePair<Point, int>> tempLine;
                tempLine = previousLine;
                previousLine = currentLine;
                currentLine = tempLine;
                currentLine.Clear();
            }

            return dic.Values.ToList();
        }

        public static List<List<Point>> Get8WayRegions(Bitmap bitmap)
        {
            return null;
        }
    }
}
