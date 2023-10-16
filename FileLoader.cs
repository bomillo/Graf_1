using Microsoft.Maui.Graphics.Win2D;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Image = Microsoft.Maui.Graphics.IImage;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.Data.Text;

namespace Graf
{
    internal class FileLoader
    {
        public static (Image, Bitmap) LoadPPM(Stream stream) {
            var tokens = TokenizePPM(stream, true);
            if(tokens.PPM6)
            {
               return LoadPPM6(stream);
          } else { return LoadPPM3(stream);}
        }

        public static (Image, Bitmap) LoadPPM6(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            Stopwatch sw = new();
            sw.Start();
            var tokens = TokenizePPM(stream);
            sw.Stop();

            var bytes = new byte[tokens.bytesarr.Length];

            var s1 = sw.Elapsed;
            sw.Restart();
            for (int i = 0; i < tokens.bytesarr.Length; i += 3)
            {
                bytes[i] = (byte)((tokens.bytesarr[i+2] / (float)tokens.max) * byte.MaxValue);
                bytes[i + 1] = (byte)((tokens.bytesarr[i + 1] / (float)tokens.max) * byte.MaxValue);
                bytes[i + 2] = (byte)((tokens.bytesarr[i ] / (float)tokens.max) * byte.MaxValue);

            }
            sw.Stop();

            var s2 = sw.Elapsed;
            sw.Restart();

            var a = LoadFromByteArray((tokens.w, tokens.h), bytes);
            sw.Stop();
            var s3 = sw.Elapsed;

            return a;
        }

        

        public static (bool PPM6, int w, int h, int max, (int start, int len)[] valuesIndex, char[] values, byte[] bytesarr) TokenizePPM(Stream stream, bool returnEarly = false) {
            StreamReader reader = new StreamReader(stream,Encoding.ASCII);
            
            List<string> tokens = new List<string>();

            int index = 0;
            (int start, int len)[] values = null;

            bool isComment = false;
            bool isToken = false;
            int tokenStartIndex = -1;


            int bufferSize = (int)(stream.Length);//1024*512*512;

            if (returnEarly) {
                bufferSize = 10000;
            }

            char[] buffer = new char[bufferSize];
            int charsRead = reader.ReadBlock(buffer,0, bufferSize);

            for (int i = 0; i < charsRead; i++)
            {
                var readChar = buffer[i];
                if (readChar == '#')
                {
                    isComment = true;
                    if (isToken)
                    {
                        AddToken();
                    }
                }
                else if (char.IsWhiteSpace(readChar))
                {
                    if (!isComment && isToken)
                    {
                        AddToken();
                    }

                    if (readChar == '\n')
                    {
                        isComment = false;
                    }
                }
                else if (!isComment)
                {
                    if (!isToken) {
                        isToken = true;
                        tokenStartIndex = i;
                    }
                }

                if (tokens.Count == 1 && returnEarly) {
                    return (tokens[0] == "P6", 0, 0, 0, null, null,null);
                }

                if (tokens.Count == 4 && tokens[0] == "P6")
                { 
                    var c = buffer[i];
                    while (c!= '\n') {
                        i++;
                        c = buffer[i];
                    }
                    i++;
                    char[] newBuffer = new char[int.Parse(tokens[1])*int.Parse(tokens[2])*3];
                    //buffer.CopyTo(newBuffer, i+1);
                    Array.Copy(buffer, i, newBuffer, 0, newBuffer.Length);

                    stream.Seek(i, SeekOrigin.Begin);
                    using var strm = new MemoryStream();
                    stream.CopyTo(strm);
                    return (tokens[0] == "P6", int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), null, null, strm.ToArray());
                }

                void AddToken() {
                    isToken = false;

                    if (values is not null)
                    {
                            values[index++] = (tokenStartIndex, i - tokenStartIndex);
                        
                    }
                    else 
                    {
                        var sb = new StringBuilder();

                        sb.Append(buffer.Skip(tokenStartIndex).Take(i- tokenStartIndex).ToArray());

                        tokens.Add(sb.ToString());
                    }

                    if (tokens.Count == 4 && values is null) {
                        values = new (int start, int len)[int.Parse(tokens[1]) * int.Parse(tokens[2]) * 3];
                    }
                    
                }
            }
            return (tokens[0] == "P6", int.Parse(tokens[1]), int.Parse(tokens[2]), int.Parse(tokens[3]), values, buffer,null);

        }

        public static (Image, Bitmap) LoadPPM3(Stream stream) {
            stream.Seek(0, SeekOrigin.Begin);
            Stopwatch sw = new();
            sw.Start();
            var tokens = TokenizePPM(stream);
            sw.Stop();

            var bytes = new byte[tokens.valuesIndex.Length];
            var s1 = sw.Elapsed;
            sw.Restart();
            for (int i = 0; i < tokens.valuesIndex.Length; i+=3) {
                bytes[i] = (byte)((FastParseInt(tokens.valuesIndex[i+2])/(float)tokens.max)*byte.MaxValue);
                bytes[i+1] = (byte)((FastParseInt(tokens.valuesIndex[i+1])/(float)tokens.max)*byte.MaxValue);
                bytes[i+2] = (byte)((FastParseInt(tokens.valuesIndex[i])/(float)tokens.max)*byte.MaxValue);

                ushort FastParseInt((int start, int len) index)
                {
                    ushort y = 0;
                    for (int j = 0; j < index.len; j++)
                        y = (ushort)(y * 10 + (tokens.values[j+ index.start] - '0'));

                    return y;
                }
            }
            sw.Stop();

            var s2 = sw.Elapsed;
            sw.Restart();

            var a = LoadFromByteArray((tokens.w, tokens.h), bytes);
            sw.Stop();
            var s3 = sw.Elapsed;

            return a;
        }


        public static (Image,Bitmap) LoadFromByteArray((int x,int y) size, byte[] bytes) {

            
            /*
            using Bitmap bmp = new Bitmap(size.x, size.y);
  
            int bytesPerPixel = 3; // 3 bytes for RGB (red, green, blue)

            // Copy the pixel values from the byte array to the bitmap.
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    int offset = y * bmp.Width * bytesPerPixel + x * bytesPerPixel;
                    System.Drawing.Color color = System.Drawing.Color.FromArgb(bytes[offset+1], bytes[offset + 1], bytes[offset]);
                    bmp.SetPixel(x, y, color);
                }
            }

            using var stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            

            */
             Bitmap bitmap = new Bitmap(size.x,size.y, PixelFormat.Format24bppRgb);
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            try
            {
                for (int y = 0; y < size.y; y++)
                {
                    IntPtr ptr = bitmapData.Scan0 + bitmapData.Stride * y;
                    System.Runtime.InteropServices.Marshal.Copy(bytes, y * size.x * 3, ptr, size.x*3);
                }

            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            var stream = new MemoryStream();


            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            return (new W2DImageLoadingService().FromStream(stream), bitmap) ;
        }

    }
}
