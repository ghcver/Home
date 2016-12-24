using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JDAutoLogin
{
    public static class AuthCodeRecognition
    {
        private static Dictionary<string, Bitmap> letterDic;

        static AuthCodeRecognition()
        {
            letterDic = new Dictionary<string, Bitmap>();

            foreach (var item in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                string[] parts = item.Split('.');
                if (parts.Length == 4 && parts[1] == "Letter" && parts[3] == "png")
                {
                    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(item);
                    letterDic.Add(parts[2], new Bitmap(stream));
                }
            }
        }

        public static string Recognize(Bitmap bitmap)
        {
            try
            {
                if (bitmap.GetPixel(0, 0).R != 0 && bitmap.GetPixel(0, 0).G != 0 && bitmap.GetPixel(0, 0).B != 0)
                {
                    return string.Empty;
                }

                Bitmap newBitmap = new Bitmap(100, 24);
                for (int i = 35; i < 135; i++)
                {
                    for (int j = 4; j < 28; j++)
                    {
                        if (IsWhite(bitmap.GetPixel(i, j)) == true)
                        {
                            newBitmap.SetPixel(i - 35, j - 4, Color.White);
                        }
                        else
                        {
                            newBitmap.SetPixel(i - 35, j - 4, Color.Black);
                        }
                    }
                }

                List<Tuple<int, string, double, int>> letterList = new List<Tuple<int, string, double, int>>();
                for (int i = 0; i < newBitmap.Width - 30; i++)
                {
                    foreach (var item in letterDic)
                    {
                        int match = 0;
                        int count = 0;
                        for (int j = 0; j < item.Value.Width; j++)
                        {
                            for (int k = 0; k < item.Value.Height; k++)
                            {
                                count++;
                                if (item.Value.GetPixel(j, k) == newBitmap.GetPixel(i + j, k))
                                {
                                    match++;
                                }
                            }
                        }
                        double percent = match * 100 / count;
                        if (percent > 80)
                        {
                            letterList.Add(new Tuple<int, string, double, int>(i, item.Key, percent, item.Value.Width));
                        }
                    }
                }

                string code = "";
                int startIndex = 0;
                int endIndex = 5;

                for (int j = 0; j < 4; j++)
                {
                    Tuple<int, string, double, int> tuple = letterList.Where(i => i.Item1 >= startIndex && i.Item1 <= endIndex).OrderByDescending(i => i.Item3).First();
                    code = code + tuple.Item2;
                    startIndex = tuple.Item1 + tuple.Item4 - 5;
                    endIndex = tuple.Item1 + tuple.Item4 + 5;
                }
                return code;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        private static bool IsWhite(Color color)
        {
            if (color.R > 180)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
