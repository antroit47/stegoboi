using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.IO;
using System.Threading.Tasks;

namespace Stegoboi
{
    /*
     * object representing an image, allows simple operations on it
     */ 
    [Serializable()]
    public class Image : ISerializable
    {
        public string Ipath { get; set; }
        public string Name { get { return (Ipath.LastIndexOf(Path.DirectorySeparatorChar) == -1) ? 
                            Ipath : Ipath.Substring(Ipath.LastIndexOf(Path.DirectorySeparatorChar) +1); } }
        public int Width { get { return Pixels.Width; } }
        public int Height { get { return Pixels.Height; } }
        public int Capacity1b { get { return Width * Height * 3; } } //in bits
        public Bitmap Pixels { get; set; }
        public bool IsValid = true;
        
        /*
         * Tries to open an image. Maked image invalid if the operation fails
         */
        public Image(string newPath)
        {
            try
            {
                Pixels = new Bitmap(newPath);
                IsValid = true;
            }
            catch (Exception)
            {
                Console.WriteLine("IMAGE LOADING FAILED: invalid image name " + newPath);
                IsValid = false;
                return;
            }
            Ipath = newPath;
        }

        /*
         * Creates an empty image of given resolution
         */
        public Image(int newWidth, int newHeight)
        {
            Pixels = new Bitmap(newWidth, newHeight);
        }

        /*
         * Constructor necessary to make image (de)serializable
         */
        public Image(SerializationInfo info, StreamingContext ctxt)
        {
            Ipath = (string)info.GetValue("Path", typeof(string));
            Pixels = (Bitmap)info.GetValue("Pixels", typeof(Bitmap));
            IsValid = (bool)info.GetValue("IsValid", typeof(bool));
        }

        /*
         * Method necessary to make image (de)serializable
         */
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Path", Ipath);
            info.AddValue("Pixels", Pixels);
            info.AddValue("IsValid", IsValid);
        }

        /*
         * Saving a valid image to the path given (outputPath should include extension).
         * Uses ImageFormat.Png to save, though format can be changed if necessary by
         * using different extension.
         * Returns bool based on the result of saving.
         */
        public bool SaveImage(string outputPath)
        {
            try
            {
                if (IsValid)
                {   
                    Pixels.Save(outputPath, ImageFormat.Png);
                    Console.WriteLine("Image "+ outputPath + " was saved successfully");
                }
                else
                {
                    Console.WriteLine("SAVING ERROR: invalid image " + outputPath);
                    return false;
                }
                
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("SAVING ERROR: null argument " + outputPath); 
                return false;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("SAVING ERROR: Illegal characters in path " + outputPath);   
                return false;
            }
            catch (ExternalException)
            {
                Console.WriteLine("SAVING ERROR: Invalid file name / overwriting the original image " + outputPath);
                return false;
            }
            catch (NotSupportedException)
            {
                Console.WriteLine("SAVING ERROR: Invalid file name " + outputPath);
                return false;
            }
            return true;
        }

        public void PrintInfo()
        {
            if (!IsValid)
            {
                Console.WriteLine("Image not loaded");
            }
            else
            {
                Console.WriteLine("Image:\n path: " + Ipath +
                    "\n size: " + Width + " x " + Height +
                    "\n maximal byte size: " + Capacity1b +
                    " B (without compression)\n 1 bit capacity: " + Capacity1b + " B\n");
            }
        }

        /*
         * sets least n signifficant bits to zero
         */
        public void ClearBits(int n)
        {
            byte[] bytes = ToBytes();
            if (bytes == null)
            {
                Console.WriteLine("ERROR: cannot clear lower bits of an invalid image");
                return;
            }
            DirectBitmap res = new DirectBitmap(Width, Height);
            int x = 0;
            int y = 0;
            int pWidth = Width;
            Parallel.For(0, bytes.Length / 4, i =>
            {
                  int r = bytes[i * 4 + 2];
                  int g = bytes[i * 4 + 1];
                  int b = bytes[i * 4];
                  r = r - (r % (int)Math.Pow(2, n));
                  g = g - (g % (int)Math.Pow(2, n));
                  b = b - (b % (int)Math.Pow(2, n));
                  res.SetPixel(x, y, Color.FromArgb(r, g, b));
                  x++;
                  if (x >= pWidth)
                  {
                      x = 0;
                      y++;
                  }
            });
            Pixels = res.Bitmap;
        }
        
        /*
         * Hides byte array (messageBytes) into the n least signifficant bits of the image.
         * If randomFill is true, the n bits of each color after there is no message left
         * will be filled in with random data.
         * Returns false, if invalid image or the data does not fit
         */
        public bool HideMessageByteArray(int n, byte[] messageBytes, bool randomFill = false) 
        {
            BitArray messageBits = new BitArray(messageBytes);
            //Console.WriteLine("hiding: "+ messageBytes.Length + " bytes");
            if (!IsValid)
            {
                Console.WriteLine("HIDING ABORTED: Operation on invalid Image");
                return false;
            }
            if (messageBits.Length > n*Capacity1b) {
                Console.WriteLine("HIDING ABORTED: message of length (" + messageBits.Length +
                    " b) cannot fit into the free space in the picture ("+ n*Capacity1b +" b)" );
                return false;
            }

            bool messageEnd = false;
            int bitPos = 0;
            byte[] bytes = ToBytes();
            DirectBitmap res = new DirectBitmap(Width, Height);
            int x = 0;
            int y = 0;
            int pWidth = Width;
            Object thisLock = new Object();
            Random rand = new Random();
            //TODO - non-functional parallel implementation - perhaps not a good idea to make it like this tho
            /*
            Parallel.For(0, bytes.Length / 4, i =>
            {
                if (i * n >= messageBits.Count) //we ran out of message to hide
                {
                    if (!randomFill)
                    {
                        Color newcol = Color.FromArgb(bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4]);
                        res.SetPixel(i % pWidth, i / pWidth, newcol);
                    }
                    else
                    {
                        int[] colors = { bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4] };
                        for (int c = 0; c < 3; c++)
                        {
                            int newNum = 0;
                            colors[c] = colors[c] - (colors[c] % (int)Math.Pow(2, n)); //clear n lowest bits
                            for (int j = 1; j <= n; j++)
                            {
                                newNum *= 2;
                                newNum += rand.Next(2);
                            }
                            colors[c] += newNum;
                        }
                        Color newcol = Color.FromArgb(colors[0], colors[1], colors[2]);
                        res.SetPixel(i % pWidth, i / pWidth, newcol);
                    }
                }
                else
                {
                    int[] colors = { bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4] };
                    for (int c = 0; c < 3; c++)
                    {
                        if (i*n >= messageBits.Count) //NEW
                        {
                            messageEnd = true;
                        }
                        else
                        {
                            int bitPos = i * n;
                            colors[c] = colors[c] - (colors[c] % (int)Math.Pow(2, n));
                            int newNum = 0;
                            for (int j = 1; j <= n; j++)
                            {
                                if (bitPos < messageBits.Count)
                                {
                                    newNum *= 2;
                                    newNum += messageBits[bitPos] == true ? 1 : 0;
                                    bitPos++;
                                }
                                else
                                {
                                    newNum *= 2;
                                }
                            }
                            colors[c] += newNum;
                        }
                    }
                    Color newcol = Color.FromArgb(colors[0], colors[1], colors[2]);
                    res.SetPixel(i % pWidth, i / pWidth, newcol);
                }
            });*/
            
            for (int i = 0; i < (bytes.Length / 4); i++)
            {
                if (messageEnd) //ran out of message to hide
                {
                    if (!randomFill)
                    {
                        Color newcol = Color.FromArgb(bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4]);
                        res.SetPixel(x, y, newcol);
                    }
                    else
                    {
                        int[] colors = { bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4] };
                        for (int c = 0; c < 3; c++)
                        {
                            int newNum = 0;
                            colors[c] = colors[c] - (colors[c] % (int)Math.Pow(2, n)); //clear n lowest bits
                            for (int j = 1; j <= n; j++)
                            {
                                newNum *= 2;
                                newNum += rand.Next(2);
                            }
                            colors[c] += newNum;
                        }
                        Color newcol = Color.FromArgb(colors[0], colors[1], colors[2]);
                        res.SetPixel(x, y, newcol);
                    }
                }
                else
                {
                    int[] colors = { bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4] };
                    for (int c = 0; c < 3; c++)
                    {
                        if (bitPos >= messageBits.Count)
                        {
                            messageEnd = true;
                        }
                        else
                        {
                            colors[c] = colors[c] - (colors[c] % (int)Math.Pow(2, n));
                            int newNum = 0;
                            for (int j = 1; j <= n; j++)
                            {
                                if (bitPos < messageBits.Count)
                                {
                                    newNum *= 2;
                                    newNum += messageBits[bitPos] == true ? 1 : 0;
                                    bitPos++;
                                }
                                else
                                {
                                    newNum *= 2;
                                }
                            }
                            colors[c] += newNum;
                        }
                    }
                    Color newcol = Color.FromArgb(colors[0], colors[1], colors[2]);
                    res.SetPixel(x, y, newcol);
                }
                x++;
                if (x >= Width)
                {
                    x = 0;
                    y++;
                }
            }
            Pixels = res.Bitmap;
            return true;
        }

        /*
         * Reads a message from image of given messageLength in bytes
         * out of n least signifficant bits, returns byte array
         */
        public byte[] ReadFromImage(int n, int messageLength)
        {
            byte[] messageBytes = new byte[messageLength];
            if (messageLength > Capacity1b * n / 8)
            {
                Console.WriteLine("READING ERROR: image cannot contain given amount of data");
                return messageBytes;
            }

            byte[] bytes = ToBytes();
            BitArray messageBits = new BitArray(messageLength * 8);
            int bitPos = 0;
            int bitMax = messageLength * 8;
            bool readingMsg = true;
            for (int i = 0; i < (bytes.Length / 4) && readingMsg; i++)
            {
                byte[] colors = { bytes[i * 4 + 2], bytes[i * 4 + 1], bytes[i * 4] };
                for (int c = 0; c < 3; c++)
                {
                    if (bitPos > bitMax)
                    {
                        readingMsg = false;
                        break;
                    }
                    else
                    {
                        for (int j = n - 1; j >= 0; j--) //bitdepth iter
                        {
                            if (bitPos >= bitMax)
                            {
                                readingMsg = false;
                                break;
                            }
                            messageBits[bitPos] = ((colors[c] >> j) & 1) != 0;
                            bitPos++;
                        }
                    }
                }
            }
            messageBits.CopyTo(messageBytes, 0);
            return messageBytes;
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Image f = (Image)obj;
                return (Ipath == f.Ipath) && (Width == f.Width) && (Height == f.Height) && (HasSamePixels(f));
            }
        }

        private bool HasSamePixels(Image f)
        {
            byte[] bytes = ToBytes();
            byte[] fbytes = f.ToBytes();

            for (int i = 0; i < bytes.Length / 4; i++)
            {
                if (bytes[i * 4 + 2] != fbytes[i * 4 + 2] ||
                    bytes[i * 4 + 1] != fbytes[i * 4 + 1] ||
                    bytes[i * 4] != fbytes[i * 4])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 1474680594;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Ipath);
            hashCode = hashCode * -1521134295 + EqualityComparer<Bitmap>.Default.GetHashCode(Pixels);
            return hashCode;
        }

        /*
         * Creates a byte[] out of Pixels
         * based on https://stackoverflow.com/questions/12168654/image-processing-with-lockbits-alternative-to-getpixel
         */
        public byte[] ToBytes()
        {
            if (Pixels == null)
                return null;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            BitmapData data = Pixels.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            IntPtr ptr = data.Scan0;
            int numBytes = data.Stride * Height;
            byte[] bytes = new byte[numBytes];
            Marshal.Copy(ptr, bytes, 0, numBytes);
            Pixels.UnlockBits(data);
            return bytes;
        }
    }
}
