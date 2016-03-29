using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication17
{
    abstract class BinarizationBase
    {
        public abstract Bitmap Binarize(Bitmap bitmap);
    }

    class Binarization1 : BinarizationBase
    {
        private byte th = 120;

        public override Bitmap Binarize(Bitmap bitmap)
        {
            bitmap = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            for (int i = 1; i < bitmap.Width - 1; i++)
            {
                for (int j = 1; j < bitmap.Height - 1; j++)
                {
                    if (Hit(bitmap.GetPixel(i, j)) == true)
                    {
                        int hit = 0;
                        if (Hit(bitmap.GetPixel(i, j - 1)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i + 1, j - 1)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i + 1, j)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i + 1, j + 1)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i, j + 1)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i - 1, j + 1)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i - 1, j)) == true)
                        {
                            hit++;
                        }
                        if (Hit(bitmap.GetPixel(i - 1, j - 1)) == true)
                        {
                            hit++;
                        }
                        if (hit >= 4)
                        {
                            bitmap.SetPixel(i, j, Color.White);
                        }
                        else
                        {
                            bitmap.SetPixel(i, j, Color.Black);
                        }
                    }
                    else
                    {
                        bitmap.SetPixel(i, j, Color.Black);
                    }
                }
            }
            bitmap = bitmap.Clone(new Rectangle(1, 1, bitmap.Width - 2, bitmap.Height - 2), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            return bitmap;
        }

        private bool Hit(Color color)
        {
            if (color.R > th && color.G > th && color.B > th)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    class Binarization2 : BinarizationBase
    {
        private byte th = 10;

        private class ColorRange
        {
            public byte R_L;
            public byte R_U;
            public byte G_L;
            public byte G_U;
            public byte B_L;
            public byte B_U;
        }

        private ColorRange GetColorRange(Color color)
        {
            ColorRange range = new ColorRange();
            range.R_L = (byte)(color.R - th < 0 ? 0 : color.R - th);
            range.R_U = (byte)(color.R + th > 255 ? 255 : color.R + th);
            range.G_L = (byte)(color.G - th < 0 ? 0 : color.G - th);
            range.G_U = (byte)(color.G + th > 255 ? 255 : color.G + th);
            range.B_L = (byte)(color.B - th < 0 ? 0 : color.B - th);
            range.B_U = (byte)(color.B + th > 255 ? 255 : color.B + th);

            return range;
        }

        public override Bitmap Binarize(Bitmap bitmap)
        {
            Dictionary<Color, int> colorDic = new Dictionary<Color, int>();

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);
                    if(colorDic.ContainsKey(color) == true)
                    {
                        colorDic[color] = colorDic[color] + 1;
                    }
                    else
                    {
                        colorDic[color] = 1;
                    }
                    
                }
            }

            Dictionary<Color, int> colorRank = new Dictionary<Color, int>();
            foreach(var item in colorDic)
            {
                int rank = 0;
                Color color = item.Key;
                ColorRange colorRange = GetColorRange(color);
                foreach (var innerItem in colorDic)
                {
                    Color innerColor = innerItem.Key;
                    if((innerColor.R >= colorRange.R_L && innerColor.R <= colorRange.R_U) && (innerColor.G >= colorRange.G_L && innerColor.G <= colorRange.G_U) && (innerColor.B >= colorRange.B_L && innerColor.B <= colorRange.B_U))
                    {
                        rank = rank + innerItem.Value;
                    }
                }
                colorRank[color] = rank;
            }

            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            Color hitColor = colorRank.OrderByDescending(i => i.Value).ToArray()[0].Key;
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    ColorRange colorRange = GetColorRange(hitColor);
                    Color color = bitmap.GetPixel(i, j);
                    if ((color.R >= colorRange.R_L && color.R <= colorRange.R_U) && (color.G >= colorRange.G_L && color.G <= colorRange.G_U) && (color.B >= colorRange.B_L && color.B <= colorRange.B_U))
                    {
                        result.SetPixel(i, j, Color.White);
                    }
                    else
                    {
                        result.SetPixel(i, j, Color.Black);
                    }
                }
            }

            return result;
        }
    }

    

    class Binarization3 : BinarizationBase
    {
        private byte th = 200;

        public override Bitmap Binarize(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);
                    result.SetPixel(i, j, Color.FromArgb((color.R + color.G + color.B) / 3, (color.R + color.G + color.B) / 3, (color.R + color.G + color.B) / 3));
                }
            }

            for (int i = 0; i < result.Width; i++)
            {
                for (int j = 0; j < result.Height; j++)
                {
                    Color color = result.GetPixel(i, j);
                    if (color.R > th)
                    {
                        result.SetPixel(i, j, Color.White);
                    }
                    else
                    {
                        result.SetPixel(i, j, Color.Black);
                    }
                }
            }

            return result;
        }
    }
}
