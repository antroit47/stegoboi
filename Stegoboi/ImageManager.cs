using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stegoboi
{
    /*
     * A class that does more complicated operations on images and on 
     * data in general. Vast amount of methods provides functionality 
     * to the consoleSteg (as a result they use console), however, 
     * they can also be used on  their own, not necessarily as a part 
     * of the console application.
     * All the hiding methods can generate a key file is generateKey is 
     * true and finalImageName(s) and keyName are supplied. In this case,
     * all the images are saved right away. In the key file
     * there is a description of the hiding process that took place,
     * the names of the final images that were created, the data necessary 
     * to read the secret and the type of the secret.
     * The format of the key is as follows: 
     * *Type of hiding that took place*
     * *output image name* *the lower bits* *data length* *the type of the data*
     * *in case of mutliple images being used, the same as the line above*
     * The key can be loaded vie UseKey method.
     */
    public static class ImageManager
    {
        public static bool SameDimensions(Image a, Image b)
        {
            return (a.Width == b.Width && a.Height == b.Height);
        }

        /*
         * Returns a new image which visualizes the difference of two images, 
         * if anyDiff is true, any difference will be fully visible. If it's false,
         * the bigger the difference, the the lighter the pixel. 
         * Returns null if the dimensions dont match.
         */
        public static Image CompareTwoImages(Image img1, Image img2, bool anyDiff = false)
        {
            if (!SameDimensions(img1, img2))
            {
                Console.WriteLine("COMPARISON FAILED: different dimensions");
                return null;
            }
            else
            {
                Image diff = new Image(img1.Width, img1.Height);
                DirectBitmap res = new DirectBitmap(img1.Width, img2.Height);
                byte[] bytes = img1.ToBytes();
                byte[] fbytes = img2.ToBytes();

                int x = 0, y = 0;
                for (int i = 0; i < bytes.Length / 4; i++)
                {
                    int r = 0, g = 0, b = 0;
                    r = Math.Abs(bytes[i * 4 + 2] - fbytes[i * 4 + 2]);
                    g = Math.Abs(bytes[i * 4 + 1] - fbytes[i * 4 + 1]);
                    b = Math.Abs(bytes[i * 4] - fbytes[i * 4]);
                    int dif = ((r + g + b) / 3);
                    if (anyDiff)
                    {
                        if (dif != 0)
                            dif = 255;
                    }
                    Color newcol = Color.FromArgb(dif, dif, dif);
                    res.SetPixel(x, y, newcol);
                    x++;
                    if (x >= img1.Width)
                    {
                        x = 0;
                        y++;
                    }
                }
                diff.Pixels = res.Bitmap;
                return diff;
            }
        }

        /*
         * Hides a serializable object(data) into an image(img) -
         *   this changes the parameter img
         * Returns true if the hiding was successful - false otherwise
         * 
         * If randomFill is true, and there remains some space in an image
         * after the hiding, the rest will be filled with random data.
         * If generateKey is true, the image will be saved and a key file
         * will be created. Final image name then must be provided(finalImageName)
         * - must include path, and it will be saved right away.
         * Key name + path (keyName) can be provided. If not, random new key 
         * file will be created in the OutputFiles folder.
         * If generateKey is false, but the finalImageName is provided anyways, 
         * the image will save under that name
         */
        public static bool HideObject(ref Image img, int lowerBits, object data,  bool randomFill = false,
                bool generateKey = false, string finalImageName = "", string keyName = "")
        {
            byte[] secret = Convertor.ObjectToBytes(data);
            if (!img.HideMessageByteArray(lowerBits, secret, randomFill))
            {
                return false;
            }

            if (generateKey || finalImageName != "")
            {
                if (!img.SaveImage(finalImageName))
                    return false;
            }
            
            if (generateKey)
            {
                keyName = CreateKeyName(keyName);
                
                string finalJustName = (finalImageName.LastIndexOf(Path.DirectorySeparatorChar) == -1) ?
                            finalImageName : finalImageName.Substring(finalImageName.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                string keyString = "Single image\n" + finalJustName + " " + lowerBits + " " + secret.Length + " " + data.GetType();

                try
                {
                    using (StreamWriter sw = File.CreateText(keyName))
                    {
                        sw.Write(keyString);
                    }
                    Console.WriteLine("Key file " + keyName + " created");
                }
                catch(Exception)
                {
                    Console.WriteLine("invalid KeyName or KeyPath : " + keyName);
                    return false;
                }
            }

            Console.WriteLine("Secret of length: " + secret.Length + " B inserted");
            return true;
        }


        /*
         * Hides the same serializable object(data) into all images in an 
         * List(imgs) in parallel. Created images will be suplied names from
         * finalImageNames array. Save destination is the finalImagePath.
         * If finalImageNames array is empty, finalImagePath + original name
         * is used as the new name
         * Returns true if the hiding was successful - false otherwise
         * 
         * randomFill and the parameters regarding key generation are explained in the HideObject method
         */
        public static bool HideSingleDataIntoMany(ref List<Image> imgs, int lowerBits, object data, bool randomFill = false,
                bool generateKey = false, string keyName = "", List<string> finalImageNames = null, string finalImagePath = "")
        {
            byte[] secret = Convertor.ObjectToBytes(data);
            int counter = 1;
            object thisLock = new object();
            int imCount = imgs.Count;
            bool result = true;
            Parallel.ForEach(imgs, img =>                                       
            {
                if (!img.HideMessageByteArray(lowerBits, secret, randomFill))
                {
                    Console.WriteLine("HIDING DATA ERROR: hiding aborted");
                    result = false;
                }
                lock (thisLock)
                {
                    Console.WriteLine("Data inserted into: " + counter + " / " + imCount + " images");
                    counter++;
                }
                    
            });
            if (!result)
            {
                return false;
            }

            //saving
            if (generateKey || finalImageNames != null)
            {
                if (imgs.Count != finalImageNames.Count)
                {
                    Console.WriteLine("SAVING ERROR: the amounts of images and provided names do not match");
                }
                //ref imgs cannot be used here. Making a copy + Parallel.For still saves a lot of time tho
                List<Image> imgsCopy = imgs; 
                bool saveResult = true;
                Parallel.For(0, imgs.Count, n =>
                {
                    string newName = "";
                    if (finalImageNames == null) //keeping original names
                    {
                        newName = finalImagePath + imgsCopy[n].Name;
                    }
                    else
                    {
                        newName = finalImageNames[n];
                    }
                    if (!imgsCopy[n].SaveImage(newName))
                    {
                        saveResult = false;
                    }
                });
                if (!saveResult)
                    return false;
            }
            if (generateKey)
            {
                string keyString = "Same data in many\n";
                int count = 0;
                foreach (Image im in imgs) //TODO use count as a control instead of foreach
                {
                    string newName = "";  
                    if (finalImageNames == null) //original names
                    {
                        newName = finalImagePath + im.Name;
                    }
                    else
                    {
                        newName = finalImageNames[count];
                    }
                    //keyline
                    string finalJustName = (newName.LastIndexOf(Path.DirectorySeparatorChar) == -1) ?
                            newName : newName.Substring(newName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    keyString += finalJustName + " " + lowerBits + " " + secret.Length + " " + data.GetType() + "\n";
                    count++;
                }
                Console.WriteLine("Secret of length: " + secret.Length + " B inserted into all images");

                keyName = CreateKeyName(keyName);

                using (StreamWriter sw = File.CreateText(keyName))
                {
                    sw.Write(keyString);
                }
                Console.WriteLine("Key file " + keyName + " created");
            }
            return true;
        }

        /*
         * Hides a serializable object(data) across multiple images from an
         * List(imgs). Order of images matters (the hiding happens in 
         * that order). In case the data fits into a subset of images in imgs,
         * the remaining images will not be used. If generateKey is true,
         * these images will not be added into the key and will not be required
         * to reconstruct the data.
         * 
         * randomFill and the parameters regarding key generation are explained in the HideObject method
         */
        public static bool HideAcrossMany(ref List<Image> imgs, int lowerBits, object data, bool randomFill = false,
                bool generateKey = false, string keyName = "", List<string> finalImageNames = null, string finalImagePath = "")
        {
            
            byte[] secret = Convertor.ObjectToBytes(data);
            List<Image> realImgs = new List<Image>();
            int secretSize = secret.Length;
            int originalSecretSize = secret.Length;
            int count = 1;
            List<byte[]> dataParts = new List<byte[]>();
            byte[] currentPart;
            int currentStart = 0;
            //checking if data fits into the images and splitting it into chunks which fit into respective images
            foreach (Image img in imgs)
            {
                int currentSize = ((img.Capacity1b * lowerBits) / 8) < secretSize ? ((img.Capacity1b * lowerBits) / 8) : secretSize;
                currentPart = new byte[currentSize];
                Buffer.BlockCopy(secret, currentStart, currentPart, 0, currentSize);
                //Console.WriteLine("chunk of size: "+ currentSize + "starting at " + currentStart + "image space: " + ((img.Capacity1b * lowerBits) / 8));
                dataParts.Add(currentPart);
                currentStart += currentSize;
                secretSize -= (img.Capacity1b * lowerBits) / 8;
                realImgs.Add(img);
                if (secretSize <= 0)
                {
                    break;
                }
                count++;
            }
            if (secretSize > 0)
            {
                Console.WriteLine("ABORTING: Data (" + originalSecretSize + " B) does " +
                    "not fit into the given number of lowest bits in the provided " +
                    "pictures (" + -(secretSize - secret.Length) + " B)");
                return false;
            }
            else
            {
                Console.WriteLine("Data (" + originalSecretSize + " B) fits into " + count +
                    "/" + imgs.Count + " images (" + -(secretSize - secret.Length) +
                    " B), the rest will not be used");
            }

            //hiding the data
            bool result = true;
            Object thisLock = new Object();
            int imCount = 1;
            Parallel.For(0, realImgs.Count, partCount =>
            {
                if (!realImgs[partCount].HideMessageByteArray(lowerBits, dataParts[partCount], randomFill))
                {
                    Console.WriteLine("HIDING DATA ERROR: hiding aborted");
                    result = false;
                }
                lock (thisLock)
                {
                    Console.WriteLine("finished: " + (imCount) + " / " + realImgs.Count + " images");
                    imCount++;
                }

            });
            if (!result)
                return false;
            Console.WriteLine("Secret of length: " + originalSecretSize + " B inserted into the images");
            
            //saving
            if (generateKey || finalImageNames != null)
            {
                if (imgs.Count != finalImageNames.Count)
                {
                    Console.WriteLine("SAVING ERROR: the amounts of images and provided names do not match");
                }
                
                bool saveResult = true;
                Parallel.For(0, realImgs.Count, n =>
                {
                    string newName = "";
                    if (finalImageNames == null) //keeping the original names
                    {
                        newName = finalImagePath + realImgs[n].Name;
                    }
                    else
                    {
                        newName = finalImageNames[n];
                    }
                    if (!realImgs[n].SaveImage(newName))
                    {
                        saveResult = false;
                    }
                });
                if (!saveResult)
                    return false;
            }
            
            if (generateKey) //generating the key
            {
                string keyString = "One data across many\n";
                count = 0;
                if (finalImageNames != null && imgs.Count != finalImageNames.Count)
                {
                    Console.WriteLine("SAVING ERROR: the amounts of images and provided names do not match");
                }

                foreach (Image im in realImgs)
                {
                    string newName = "";
                    if (finalImageNames == null)
                    {
                        newName = finalImagePath + im.Name;
                    }
                    else
                    {
                        newName = (string)finalImageNames[count];
                    }
                    string finalJustName = (newName.LastIndexOf(Path.DirectorySeparatorChar) == -1) ?
                            newName : newName.Substring(newName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                    keyString += finalJustName + " " + lowerBits + " " + originalSecretSize + " " + data.GetType() + "\n";
                    count++;
                }
                keyName = CreateKeyName(keyName);

                using (StreamWriter sw = File.CreateText(keyName))
                {
                    sw.Write(keyString);
                }
                Console.WriteLine("Key file " + keyName + " created");
            }
            return true;
        }

        private static string CreateKeyName(string keyName)
        {
            if (keyName == "")
            {
                keyName = ("KEY" + (new Random()).Next()) + ".txt";
            }
            keyName = keyName.TrimEnd(Path.DirectorySeparatorChar);
            if (Directory.Exists(keyName))
            {
                keyName = keyName + Path.DirectorySeparatorChar + "KEY" + (new Random()).Next() + ".txt";
            }
            return keyName;
        }

        /*
         * Reads data from img as byte[] and transfers it to object
         * Returns null if the procedure fails
         * This method is a reverse method to HideObject
         */
        public static object ReadObject(Image img, int lowerBits, int length)
        {
            return Convertor.BytesToObject(img.ReadFromImage(lowerBits, length));
        }

        /*
         * Reads the same data from multiple images(in imgs) as byte[] 
         * and transfers it to object. 
         * Returns null if reading fails or if the images contain different data.
         * This method is a reverse method to HideSingleDataIntoMany
         */
        public static object ReadFromMany(List<Image> imgs, int lowerBits, int length)
        {
            bool first = true;
            object result = null;
            foreach (Image img in imgs)
            {
                if (first)
                {
                    result = Convertor.BytesToObject(img.ReadFromImage(lowerBits, length));
                    if (result == null)
                    {
                        Console.WriteLine("ERROR: invalid data in image");
                        return null;
                    }
                    first = false;
                }
                else
                {
                    if (!result.Equals(Convertor.BytesToObject(img.ReadFromImage(lowerBits, length))))
                    {
                        Console.WriteLine("ERROR: images contain different hidden data");
                        return null;
                    }
                }
            }
            Console.WriteLine("Success, all images contain the same data");
            return result;
        }

        /*
         * Reads one "big" data split across multiple images(in imgs - 
         * order of the images matters) as byte[] and transfers it to object. 
         * The imgs can contain redundant images which will not be used.
         * Returns null if reading fails.
         * This method is a reverse method to HideAcrossMany
         */
        public static object ReadDataAcrossMany(List<Image> imgs, int lowerBits, int length)
        {
            List<Image> realImgs = new List<Image>();
            byte[] result = new byte[0];
            int remainingSecretSize = length;
            int count = 1;
            //reading + putting the data back together
            foreach (Image img in imgs)
            {
                if (remainingSecretSize <= 0)
                    break;
                int partSize = (img.Capacity1b * lowerBits / 8) < remainingSecretSize ? (img.Capacity1b * lowerBits)/8 : remainingSecretSize;
                //Console.WriteLine("retrieving part of: " + partSize + " B");
                byte[] part = img.ReadFromImage(lowerBits, partSize);
                byte[] newResult = new byte[result.Length + part.Length];
                System.Buffer.BlockCopy(result, 0, newResult, 0, result.Length);
                System.Buffer.BlockCopy(part, 0, newResult, result.Length, part.Length);
                result = newResult;
                count++;
                remainingSecretSize = remainingSecretSize - partSize;
            }
            
            if (remainingSecretSize > 0)
            {
                Console.WriteLine("ERROR: Data (" + length + " B) is too big" +
                    " to fit into the provided pictures (" + -(remainingSecretSize - length) + " B)");
                return null;
            }
            else
            {
                Console.WriteLine("Data (" + length + " B) loaded from the images ");
            }
            
            object res = Convertor.BytesToObject(result);
            return res;
        }

        /*
         * Given a keyName (including its path) and the location of the images used in the key,
         * this method reads whatever information(byte[]) was hidden in the images - Information
         * is converted to an object, which was passed by redderence(data).
         * Returns type of the data (as string) which was read(4th element of every key line), "NULL" if the data extraction was not successful.
         */
        public static string UseKey(string keyName, string imagesLocation, out object data)
        {
            data = null;
            try
            {
                using (StreamReader sr = File.OpenText(keyName))
                {
                    switch (sr.ReadLine())
                    {
                        case "Single image":
                            string line = (sr.ReadLine());
                            if(!ParseLine(line, out string fileName, out int lowerBits, out int length, out string type))
                                return "NULL";
                            Image img = new Image(imagesLocation + Path.DirectorySeparatorChar + fileName);
                            if (!img.IsValid)
                            {
                                Console.WriteLine("IMAGE LOADING ERROR: Image from key file not found (" + imagesLocation +
                                    Path.DirectorySeparatorChar + fileName + ")");
                                return "NULL";
                            } 
                            data = Convertor.BytesToObject(img.ReadFromImage(lowerBits, length));
                            Console.WriteLine("Successfully read an object of type: " + type);
                            return type;

                        case "Same data in many":
                            List<Image> images = new List<Image>();
                            type = "NULL";
                            lowerBits = 0;
                            length = 0;
                            while (sr.Peek() >= 0)
                            {
                                line = sr.ReadLine();
                                if (!ParseLine(line, out fileName, out lowerBits, out length, out type))
                                {
                                    Console.WriteLine("Damaged key file");
                                    return "NULL";
                                }
                                images.Add(new Image(imagesLocation + Path.DirectorySeparatorChar + fileName));
                            }
                            data = ReadFromMany(images, lowerBits, length);
                            return type;
                        default:
                            Console.WriteLine("Damaged key file");
                            return "NULL";

                        case "One data across many":
                            images = new List<Image>();
                            type = "NULL";
                            lowerBits = 0;
                            length = 0;
                            while (sr.Peek() >= 0)
                            {
                                line = sr.ReadLine();
                                if (!ParseLine(line, out fileName, out lowerBits, out length, out type))
                                    return "NULL";
                                images.Add(new Image(imagesLocation + Path.DirectorySeparatorChar + fileName));               
                            }
                            data = ReadDataAcrossMany(images, lowerBits, length);
                            return type;
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid key file name / damaged key file");
                return "NULL";
            }
        }

        /*
         * This method parses a line if a key file into 4 out parameters
         * Returns false if the line does not match expected format
         */
        private static bool ParseLine(string line, out string fileName, out int lowerBits, out int length, out string type)
        {
            fileName = null;
            type = null;
            lowerBits = 0;
            length = 0;

            string pattern = @"(.*) (\d) (\d*) (.*)";
            if (Regex.IsMatch(line, pattern))
            {
                Match match = Regex.Match(line, pattern);
                fileName = match.Groups[1].Value;
                lowerBits = Int32.Parse(match.Groups[2].Value);
                length = Int32.Parse(match.Groups[3].Value);
                type = match.Groups[4].Value;
                return true;
            }
            Console.WriteLine("Invalid Image entry in a file");
            return false;
        }
    }
}
