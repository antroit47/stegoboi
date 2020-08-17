using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Stegoboi
{
    class Program
    {
        /*
         * Stegoboi 1.0
         * 
         * KEYWORDS IN THE USAGE SECTION:
         * path           = path to a file, enclose with " " in case it contains spaces.
         * inputImageList = paths to all input images sparated by spaces. 
         *                  Can be replaced by -a[ directoryPath]
         * -a [directoryPath] = this reads all .png, .bmp, .jpg files in specified folder
         *                      (current folder if folderPath is empty)
         * directoryPath    = path to a directory (trailing \\ is not necessary), enclose in " " if it 
         *                    contains spaces (do not type the trailing \\ in this case)
         * lowestBits       = int (0-8), the least signifficant color bits which are used to hide/extract the data
         * dataLength       = the length of the hidden data needed for its extraction
         * outputImageList  = paths to all output images sparated by spaces. 
         *                    Can be replaced by -o [directoryPath]
         * -o [directoryPath] = this keeps all the original names and saves the files into the folderPath
         *                      or current directory if folderPath is empty
         * [-k [keyPath]]   = generates a key (simplifies the process of extracting the data later).
         *                    If the keyPath is empty or if it only specifies the directory, random 
         *                    key name will be selected
         * dataSpecifier    = specifies the data and the type to be hidden
         *                    S "secret data string"           for string data
         *                    I secretImagePath                for an image 
         *                    F secretFilePath                 for a file of different type
         * resultTypeSpecifier = specifies the type of the data to be extracted + the path to save it
         *                    S                                for string data - displayed in console
         *                    I extractedImagePath             for an output image 
         *                    F extractedFilePath              for an output file of different type
         * [-r]                = -r introduces random fill on all remaining free space in an image 
         *                       after the data to hide runs out
         * keyImageDirPath = path to a directory containing all images used by a key
         *                   
         * USAGE:
         * -i  imagePath  = enumerate info about an image
         * -hs inputImagePath lowestBits dataSpecifier outputImagePath [-k keyPath] [-r] 
         *        = hide data into a single image
         * -hw inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] 
         *        = hide same data into multiple images (watermarking)
         * -hb inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] 
         *        = split larger amount of data acros multiple images (their order matters for correct extraction)
         * -rs inputImagePath lowestBits dataLength resultTypeSpecifier      
         *        = read the data from a single image
         * -rw inputImageList/-a lowestBits dataLength resultTypeSpecifier        
         *        = read data from multiple images and check if its the same (watermark checking)
         * -rb inputImageList/-a lowestBits dataLength resultTypeSpecifier        
         *        = read and combine larger data across multiple images
         * -rk keyPath keyImageDirPath [resultDirectoryPath]       
         *        = reading the data from the sources specified by the key 
         *          In case the extracted data is an image/file, the resultDirectoryPath will be used to save it.
         * -c imagePath1 imagePath2 [-p]   
         *        = compare two images of the same dimensions for difference
         *          -p makes the difference progressive(the bigger the differrence in pixels, the lighter the color)
         *          with -p left out, any difference will be a white pixel
         * -v     = print version number
         * -h     = print this manual
         * 
         * EXAMPLES:
         *  -hs "..\..\InputFiles\a.jpg" 7 F  "..\..\InputFiles\a.mp3" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles\k.txt" -r
         *  -hw "..\..\InputFiles\a.jpg" "..\..\InputFiles\100.jpg" "..\..\InputFiles\image2.png" 7 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         *  -hb -a "..\..\InputFiles" 7 F  "..\..\InputFiles\a.mp3" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         *  -rs "..\..\OutputFiles\result.jpg" 7 20654 I "..\..\OutputFiles\secret.jpg"
         *  -rw -a "..\..\OutputFiles" 7 232 F "..\..\OutputFiles\result.txt"
         *  -rb -a "..\..\OutputFiles" 1 45 S
         *  -rk "..\..\OutputFiles\k.txt" "..\..\OutputFiles" "..\..\OutputFiles"
         *  -c "..\..\InputFiles\a.jpg" "..\..\InputFiles\a - Copy.jpg" "..\..\OutputFiles\result.png"
         */
        static void Main(string[] args)
        {
            if (!ArgParser.Parse(args))
                Console.WriteLine("Use -h to see the manual");

            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }

    /*
     * A class which parses the arguments given to the program and uses the methods provided by 
     * other classes to load, read from, hide into and save images.
     */
    public static class ArgParser
    {
        private enum Action { i,hs,hw,hb,rs,rw,rb,rk,c,v,h}

        public static bool Parse(string[] args)
        {
            if (args.Length == 0)
                return false;

            if (args[0][0] != '-' || !Enum.TryParse(args[0].Remove(0, 1), out Action resultAction))
            {
                Console.WriteLine("Invalid first argument: " + args[0]);
                return false;
            }
            
            switch (resultAction)
            {
                case (Action.v):
                    Console.WriteLine("Stegoboi 1.0");
                    return true;
                case (Action.h):
                    PrintManual();
                    return true;
                case (Action.i):
                    return ResolveI(args);
                case (Action.hs):
                    return ResolveH(args);
                case (Action.hw):
                    return ResolveH(args);
                case (Action.hb):
                    return ResolveH(args);
                case (Action.rs):
                    return ResolveR(args);
                case (Action.rw):
                    return ResolveR(args);
                case (Action.rb):
                    return ResolveR(args);
                case (Action.rk):
                    return ResolveKey(args);
                case (Action.c):
                    return ResolveCopy(args);
            }
            return true;
        }

        /*
        simple test 
         -i "..\..\InputFiles\a.jpg"
        */
        private static bool ResolveI(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Missing the image path");
                return false;
            }
            if (ReadImagePath(args, 1, out Image img))
            {
                img.PrintInfo();
                return true;
            }
            return false;
        }

        /*
        test the simplest hs without key, without -r
         -hs "..\..\InputFiles\a.jpg" 3 S "asdqwe asd awsef s t serg ertg ertg tttt" "..\..\OutputFiles\result.jpg
        test hs with everything
         -hs "..\..\InputFiles\a.jpg" 3 S "asdqwe asd awsef s t serg ertg ertg tttt" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles\k.txt" -r
        test hide file -k -r
         -hs "..\..\InputFiles\a.jpg" 7 F  "..\..\InputFiles\a.mp3" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles\k.txt" -r
        no key name
         -hs "..\..\InputFiles\a.jpg" 7 F  "..\..\InputFiles\a.mp3" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles"
        
        test hw all from folder keep original names 
        -hw -a "..\..\InputFiles" 7 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
        selected images, keep originals
        -hw "..\..\InputFiles\a.jpg" "..\..\InputFiles\100.jpg" "..\..\InputFiles\image2.png" 7 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
        changed the names to custom
        -hw "..\..\InputFiles\a.jpg" "..\..\InputFiles\100.jpg" "..\..\InputFiles\image2.png" 7 F  "..\..\InputFiles\boink.txt" "..\..\OutputFiles\1.jpg" "..\..\OutputFiles\2.jpg" "..\..\OutputFiles\3.jpg" -k "..\..\OutputFiles" -r
        saved to the curr directory
        -hw "..\..\InputFiles\a.jpg" "..\..\InputFiles\100.jpg" "..\..\InputFiles\image2.png" 7 F  "..\..\InputFiles\boink.txt" -o
             
        test hb
        -hb -a "..\..\InputFiles" 7 F  "..\..\InputFiles\a.mp3" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
        */
        private static bool ResolveH(string[] args)
        {
            int currArgPosition = 1;
            Image inputImg = null;
            List<Image> inputImgs = null;
            if (args[0] == "-hs")
            {
                if (!ReadImagePath(args, currArgPosition++, out inputImg))
                    return false;
            }
            else
            {
                if (!ReadInputImagePaths(args, ref currArgPosition, out inputImgs))
                    return false;
            }

            if (!ReadInt(args, currArgPosition++, out int lowerBits, true))
                return false;
            if (!ReadDataSpecifier(args, ref currArgPosition, out object data))
                return false;
            string outputPath = "";
            List<string> outputPaths = new List<string>();

            //loading output name(s)
            if (args[0] == "-hs")
            {
                if (currArgPosition >= args.Length)
                {
                    Console.WriteLine("Missing the outputFile path");
                    return false;
                }
                outputPath = args[currArgPosition++];
            }
            else
            {
                if (!ReadOutputNames(args, ref currArgPosition, ref outputPaths, inputImgs))
                    return false;
            }
            
            bool useKey = ReadKey(args, ref currArgPosition, out string keyPath);
            bool useRandomFill = ReadRandomFill(args, ref currArgPosition);
            
            if (args[0] == "-hs")   
                return ImageManager.HideObject(ref inputImg, lowerBits, data, useRandomFill, useKey, outputPath, keyPath);
            if (args[0] == "-hw")   
                return ImageManager.HideSingleDataIntoMany(ref inputImgs, lowerBits, data, useRandomFill, useKey, keyPath, outputPaths);
            else                    
                return ImageManager.HideAcrossMany(ref inputImgs, lowerBits, data, useRandomFill, useKey, keyPath, outputPaths);
        }

        /*test single, string
         * -hs "..\..\InputFiles\a.jpg" 7 S  "yooooooooo" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles"
         * -rs "..\..\OutputFiles\result.jpg" 7 34 S
         * 
         *test single, image
         * -hs "..\..\InputFiles\a.jpg" 7 I  "..\..\InputFiles\100.jpg" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles"
         * -rs "..\..\OutputFiles\result.jpg" 7 20654 I "..\..\OutputFiles\secret.jpg"
         * 
         *test single, file
         * -hs "..\..\InputFiles\a.jpg" 7 F  "..\..\InputFiles\a.mp3" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles"
         * -rs "..\..\OutputFiles\result.jpg" 7 4851554 F "..\..\OutputFiles\result.mp3"
         * 
         *test watermarking, string
         * -hw -a "..\..\InputFiles" 7 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         * -rw -a "..\..\OutputFiles" 7 45 S
         * 
         *test watermarking, image
         * -hw -a "..\..\InputFiles" 7 I "..\..\InputFiles\image.png" -o "..\..\OutputFiles" -k "..\..\OutputFiles"
         * -rw -a "..\..\OutputFiles" 7 550 I "..\..\OutputFiles\result.jpg"
         * 
         *test watermarking, file
         * -hw -a "..\..\InputFiles" 7 F "..\..\InputFiles\boink.txt" -o "..\..\OutputFiles" -k "..\..\OutputFiles"
         * -rw -a "..\..\OutputFiles" 7 232 F "..\..\OutputFiles\result.txt"
         * 
         *test big data, string
         * -hb -a "..\..\InputFiles" 1 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         * -rb -a "..\..\OutputFiles" 1 45 S
         * 
         *test big data, image
         * -hb -a "..\..\InputFiles" 1 I  "..\..\InputFiles\big.jpg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         * -rb -a "..\..\OutputFiles" 1 3306173 I "..\..\OutputFiles\result.png"
         * 
         *test big data, file
         * -hb -a "..\..\InputFiles" 1 F  "..\..\InputFiles\a.mp3" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
         * -rb -a "..\..\OutputFiles" 1 4851554 F "..\..\OutputFiles\result.mp3"
         */
        private static bool ResolveR(string[] args)
        {
            int currArgPosition = 1;
            Image inputImg = null;
            List<Image> inputImgs = null;
            if (args[0] == "-rs")
            {
                if (!ReadImagePath(args, currArgPosition++, out inputImg))
                    return false;
            }
            else
            {
                if (!ReadInputImagePaths(args, ref currArgPosition, out inputImgs))
                    return false;
            }

            if (!ReadInt(args, currArgPosition++, out int lowerBits, true))
                return false;
            if (!ReadInt(args, currArgPosition++, out int dataLength, false))
                return false;

            string type = "";
            string outputPath = "";
            if (currArgPosition >= args.Length)
            {
                Console.WriteLine("Missing the type identifier");
                return false;
            }
            switch (args[currArgPosition++])
            {
                case "S":
                    type = "string";
                    break;
                case "I":
                    type = "image";
                    if (currArgPosition >= args.Length)
                    {
                        Console.WriteLine("Missing the outputImage path");
                        return false;
                    }
                    outputPath = args[currArgPosition++];
                    break;
                case "F":
                    type = "file";
                    if (currArgPosition >= args.Length)
                    {
                        Console.WriteLine("Missing the outputFile path");
                        return false;
                    }
                    outputPath = args[currArgPosition++];
                    break;
                default:
                    Console.WriteLine("Invalid data specifier: " + args[currArgPosition - 1]);
                    return false;
            }
            object result = null;
            if (args[0] == "-rs") 
                result = ImageManager.ReadObject(inputImg, lowerBits, dataLength);
            if (args[0] == "-rw")
                result = ImageManager.ReadFromMany(inputImgs, lowerBits, dataLength);
            if (args[0] == "-rb")
                result = ImageManager.ReadDataAcrossMany(inputImgs, lowerBits, dataLength);
            if (result == null)
                return false;
            try
            {
                switch (type)
                {
                    case "string":
                        Console.WriteLine("Retrieved hidden data: ");
                        Console.WriteLine((string)result + "\n");
                        break;
                    case "image":
                        if (!((Image)result).SaveImage(outputPath))
                            return false;
                        break;
                    case "file":
                        Console.WriteLine("The file will be created shortly \n");
                        Task t = Task.Run(async () => await ((StegoFile)result).SaveAsync(outputPath));
                        t.Wait();
                        break;
                }
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("Error: The data in the image cannot be converted to selected type");
            }
            return true;
        }

        /*
         *key single string
         * -hs "..\..\InputFiles\a.jpg" 7 S  "yooooooooo" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles\k.txt"
         * -rk "..\..\OutputFiles\k.txt" "..\..\OutputFiles"
         *key watermark image
         * -hw -a "..\..\InputFiles" 7 I "..\..\InputFiles\image.png" -o "..\..\OutputFiles" -k "..\..\OutputFiles\k.txt"
         * -rk "..\..\OutputFiles\k.txt" "..\..\OutputFiles" "..\..\OutputFiles2"
         *key big data file
         * -hb -a "..\..\InputFiles" 1 F "..\..\InputFiles\a.mp3" -o "..\..\OutputFiles" -k "..\..\OutputFiles\k.txt"
         * -rk "..\..\OutputFiles\k.txt" "..\..\OutputFiles" "..\..\OutputFiles"
         */
        private static bool ResolveKey(string[] args)
        {   
            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Invalid keyPath: " + args[1]);
                return false;
            }
            string inputFileDirectory = "";
            if (args.Length > 2)
            {
                if (!Directory.Exists(args[2]))
                {
                    Console.WriteLine("Invalid inputFileDirectory path: " + args[2]);
                    return false;
                }
                inputFileDirectory = args[2];
            }
            string resultType = ImageManager.UseKey(args[1], inputFileDirectory, out object result);
            if (resultType == "NULL")
                return false;
            if (resultType == "System.String")
            {
                Console.WriteLine("Retrieved hidden data: ");
                Console.WriteLine((string)result + "\n");
                return true;
            }
            else
            {
                string outputFilePath = "";
                if (args.Length > 3)
                {
                    outputFilePath = args[3] + Path.DirectorySeparatorChar;
                }
                if (resultType == "Stegoboi.Image")
                {
                    return ((Image)result).SaveImage(outputFilePath + ((Image)result).Name);
                }
                if (resultType == "Stegoboi.StegoFile")
                {
                    Console.WriteLine("The file will be created shortly \n");
                    Task t = Task.Run(async () => await ((StegoFile)result).SaveAsync(outputFilePath + ((StegoFile)result).FileName));
                    t.Wait();
                    return true;
                }
                else
                {
                    Console.WriteLine("Unknown result type of the secret: " + resultType);
                    return false;
                }
            }
        }

        /*
         * -c "..\..\InputFiles\a.jpg" "..\..\InputFiles\a - Copy.jpg" "..\..\OutputFiles\result.png" 
         * -c "..\..\InputFiles\a.jpg" "..\..\InputFiles\a - Copy.jpg" "..\..\OutputFiles\result.png" -p
         */
        private static bool ResolveCopy(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Missing inputImagePath");
                return false;
            }
            if (!ReadImagePath(args, 1, out Image img1) || !ReadImagePath(args, 2, out Image img2))
                return false;
            if (args.Length < 4)
            {
                Console.WriteLine("Missing outputImagePath");
                return false;
            }
            bool progressive = false;
            if (args.Length > 4)
            {
                if (args[4] == "-p")
                {
                    progressive = true;
                }
                else
                {
                    Console.WriteLine("Unknown modifier: " + args[4]);
                    return false;
                }
            }
            Image result = ImageManager.CompareTwoImages(img1, img2, !progressive);
            if (result == null)
            {
                return false;
            }
            return result.SaveImage(args[3]);
        }
        
        private static bool ReadImagePath(string[] args, int argPosition, out Image img)
        {
            img = null;
            if (argPosition >= args.Length)
            {
                Console.WriteLine("Missing the imagePath");
                return false;
            }
            if (File.Exists(args[argPosition]))
            {
                img = new Image(args[argPosition]);
                if (!img.IsValid)
                {
                    Console.WriteLine("Invalid image: " + args[argPosition]);
                    return false;
                }
                return true;
            }
            else
            {
                Console.WriteLine("Invalid image path: " + args[argPosition]);
                return false;
            }
        }

        private static bool ReadInputImagePaths(string[] args, ref int argPosition, out List<Image> inputImgs)
        {
            inputImgs = new List<Image>();
            if (argPosition >= args.Length)
            {
                Console.WriteLine("Missing the list of inputFile paths or [-a inputFolder]");
                return false;
            }
            if (args[argPosition] == "-a") //all images in the folder
            {
                argPosition++; 
                if (argPosition >= args.Length)
                {
                    Console.WriteLine("Missing the inputFiles folder path");
                    return false;
                }
                string inputFileFolder = "";
                if (!int.TryParse(args[argPosition], out _)) //-a foldername
                {
                    if (!Directory.Exists(args[argPosition]))
                    {
                        Console.WriteLine("Invalid output directory: " + args[argPosition]);
                        return false;
                    }
                    inputFileFolder = args[argPosition++];
                }
                
                List<string> filesFound = new List<string>();
                foreach (var ext in (new string[] { "jpg", "png", "bmp" }))
                {
                    filesFound.AddRange(Directory.GetFiles(inputFileFolder, string.Format("*.{0}", ext), SearchOption.TopDirectoryOnly));
                }
                foreach (string fileName in filesFound)
                {
                    Image img = new Image(fileName);
                    if (!img.IsValid)
                        return false;
                    Console.WriteLine("Image found: " + fileName);
                    inputImgs.Add(img);
                }
                Console.WriteLine("-> " + inputImgs.Count + " images loaded");
            }
            else //specific image select
            {
                while (true)
                {
                    if (args.Length <= argPosition)
                        break;
                    if (File.Exists(args[argPosition]))
                    {
                        Image img = new Image(args[argPosition]);
                        if (!img.IsValid)
                        {
                            Console.WriteLine("Invalid image: " + args[argPosition]);
                            return false;
                        }
                        inputImgs.Add(img);
                        argPosition++;
                    }
                    else
                    {
                        if (int.TryParse(args[argPosition], out _))
                            break;
                        Console.WriteLine("Invalid image path: " + args[argPosition]);
                        return false;
                    }
                }
            }
            if (inputImgs.Count == 0)
            {
                Console.WriteLine("Missing the input file paths");
                return false;
            }
            return true;
        }

        private static bool ReadFilePath(string[] args, int argPosition, out StegoFile file)
        {
            file = null;
            if (argPosition >= args.Length)
            {
                Console.WriteLine("Missing file path");
                return false;
            }
            if (File.Exists(args[argPosition]))
            {
                file = new StegoFile(Path.GetFileName(args[argPosition]), args[argPosition]);
                return true;
            }
            else
            {
                Console.WriteLine("Invalid file path: " + args[argPosition]);
                file = null;
                return false;
            }
        }

        private static bool ReadInt(string[] args, int argPosition, out int n, bool getLowerBits)
        {
            if (argPosition >= args.Length)
            {
                n = -1;
                if (getLowerBits)
                    Console.WriteLine("Missing the lowestBits value");
                else
                    Console.WriteLine("Missing the dataLength value");
                return false;
            }
            if (!Int32.TryParse(args[argPosition], out n) || n < 0 || (getLowerBits && n > 8))
            {
                if (getLowerBits)
                    Console.WriteLine("invalid format of lowerBits (must be 0-8): " + args[argPosition]);
                else
                    Console.WriteLine("invalid format of dataLength: " + args[argPosition]);
                return false;
            }
            return true;
        }

        private static bool ReadDataSpecifier(string[] args, ref int argPosition, out object data)
        {
            data = null;
            if (argPosition >= args.Length)
            {
                Console.WriteLine("Missing data specifier");
                return false;
            }
            switch (args[argPosition++])
            {
                case "S":
                    if (argPosition >= args.Length)
                    {
                        Console.WriteLine("Missing the string to hide");
                        return false;
                    }
                    data = args[argPosition++];
                    return true;
                case "I":
                    if (!ReadImagePath(args, argPosition++, out Image img))
                        return false;
                    data = img;
                    return true;
                case "F":
                    if (!ReadFilePath(args, argPosition++, out StegoFile file))
                        return false;
                    data = file;
                    return true;
                default:
                    Console.WriteLine("Invalid data specifier: " + args[argPosition-1]);
                    data = null;
                    return false;
            }
        }
       
        private static bool ReadOutputNames(string[] args, ref int argPosition, ref List<string> outputPaths, List<Image> inputImgs)
        {
            if (argPosition >= args.Length)
            {
                Console.WriteLine("Missing the list of output paths");
                return false;
            }
            if (args[argPosition] == "-o") //keeping the original names
            {
                string outputFolder = "";
                argPosition++;

                if ((argPosition < args.Length) && !(args[argPosition] == "-k") && !(args[argPosition] == "-r"))
                {
                    if (!Directory.Exists(args[argPosition]))
                    {
                        Console.WriteLine("Invalid output directory: " + args[argPosition]);
                        return false;
                    }
                    outputFolder = args[argPosition++];
                }
                if (outputFolder != "")
                    outputFolder += Path.DirectorySeparatorChar;
                foreach (Image img in inputImgs)
                {
                    outputPaths.Add(outputFolder + img.Name);
                }
            }
            else
            {
                while (true) //loading the new names
                {
                    if (args.Length <= argPosition || args[argPosition] == "-k" || args[argPosition] == "-r")
                        break;
                    outputPaths.Add(args[argPosition]);
                    argPosition++;
                }
            }
            if (outputPaths.Count != inputImgs.Count)
            {
                Console.WriteLine("The number of inputPaths and the outputPaths must be the same");
                return false;
            }
            return true;
        }
        
        private static bool ReadKey(string[] args, ref int argPosition, out string keyName)
        {
            keyName = "";
            if (args.Length <= argPosition)
            {
                return false;
            }
            
            if (!(args[argPosition] == "-k"))
            {
                return false;
            }
            else
            {
                argPosition++;
                if (args.Length <= argPosition || args[argPosition] == "-r") //key name not supplied - random key
                {
                    keyName = "";
                }
                else
                {
                    keyName = args[argPosition];
                    argPosition++;
                }
            }
            
            return true;
        }

        private static bool ReadRandomFill(string[] args, ref int argPosition)
        {
            if (args.Length <= argPosition)
                return false;
            if (args[argPosition] == "-r")
            {
                argPosition++;
                return true;
            }
            return false;
        }

        //https://www.buildmystring.com/build.php
        private static void PrintManual()
        {
            Console.WriteLine(
                " Stegoboi 1.0\n" +
                "\n" +
                "KEYWORDS IN THE USAGE SECTION:\n" +
                "path           = path to a file, enclose with \" \" in case it contains spaces\n" +
                "inputImageList = paths to all input images sparated by spaces. \n" +
                "                 Can be replaced by -a[ directoryPath]\n" +
                "-a [directoryPath] = this reads all .png, .bmp, .jpg files in specified folder\n" +
                "                     (current folder if folderPath is empty)\n" +
                "directoryPath = path to a directory(trailing \\ is not necessary), enclose in \" \" if it\n" +
                "                contains spaces(do not type the trailing \\ in this case)\n" +
                "lowestBits       = int (0-8), the least signifficant color bits which are used to hide/extract the data\n" +
                "dataLength       = the length of the hidden data needed for its extraction\n" +
                "outputImageList  = paths to all output images sparated by spaces. \n" +
                "                   Can be replaced by -o [directoryPath]\n" +
                "-o [directoryPath] = this keeps all the original names and saves the files into the folderPath\n" +
                "                     or current directory if folderPath is empty\n" +
                "[-k [keyPath]]   = generates a key (simplifies the process of extracting the data later).\n" +
                "                   If the keyPath is empty or if it only specifies the directory, random \n" +
                "                   key name will be selected\n" +
                "dataSpecifier    = specifies the data and the type to be hidden\n" +
                "                   S \"secret data string\"           for string data\n" +
                "                   I secretImagePath                for an image \n" +
                "                   F secretFilePath                 for a file of different type\n" +
                "resultTypeSpecifier = specifies the type of the data to be extracted + the path to save it\n" +
                "                   S                                for string data - displayed in console\n" +
                "                   I extractedImagePath             for an output image \n" +
                "                   F extractedFilePath              for an output file of different type\n" +
                "[-r]            = introduces random fill on all remaining free space in an image \n" +
                "                  after the data to hide runs out\n" +
                "keyImageDirPath = path to a directory containing all images used by a key\n" +
                "\n" +
                "USAGE:\n" +
                "-i  imagePath  = enumerate info about an image\n" +
                "-hs inputImagePath lowestBits dataSpecifier outputImagePath [-k keyPath] [-r]\n" +
                "       = hide data into a single image\n" +
                "-hw inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] \n" +
                "       = hide same data into multiple images (watermarking)\n" +
                "-hb inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] \n" +
                "       = split larger amount of data acros multiple images (their order matters for correct extraction)\n" +
                "-rs inputImagePath lowestBits dataLength resultTypeSpecifier      \n" +
                "       = read the data from a single image\n" +
                "-rw inputImageList/-a lowestBits dataLength resultTypeSpecifier        \n" +
                "       = read data from multiple images and check if its the same (watermark checking)\n" +
                "-rb inputImageList/-a lowestBits dataLength resultTypeSpecifier        \n" +
                "       = read and combine larger data across multiple images\n" +
                "-rk keyPath keyImageDirPath [resultDirectoryPath]       \n" +
                "       = reading the data from the sources specified by the key \n" +
                "         In case the extracted data is an image/file, the resultDirectoryPath will be used to save it.\n" +
                "-c imagePath1 imagePath2 [-p]   \n" +
                "       = compare two images of the same dimensions for difference\n" +
                "         -p makes the difference progressive(the bigger the differrence in pixels, the lighter the color)\n" +
                "         with -p left out, any difference will be a white pixel\n" +
                "-v     = print version number\n" +
                "-h     = print this manual\n" +
                "\n" +
                "EXAMPLES:\n" +
                "-hs \"..\\..\\InputFiles\\a.jpg\" 7 F  \"..\\..\\InputFiles\\a.mp3\" \"..\\..\\OutputFiles\\result.jpg\" -k \"..\\..\\OutputFiles\\k.txt\" -r\n" +
                "-hw \"..\\..\\InputFiles\\a.jpg\" \"..\\..\\InputFiles\\100.jpg\" \"..\\..\\InputFiles\\image2.png\" 7 S  \"asdqwe qasd sefg sdfg\" -o \"..\\..\\OutputFiles\" -k \"..\\..\\OutputFiles\" -r\n" +
                "-hb -a \"..\\..\\InputFiles\" 7 F  \"..\\..\\InputFiles\\a.mp3\" -o \"..\\..\\OutputFiles\" -k \"..\\..\\OutputFiles\" -r\n" +
                "-rs \"..\\..\\OutputFiles\\result.jpg\" 7 20654 I \"..\\..\\OutputFiles\\secret.jpg\"\n" +
                "-rw -a \"..\\..\\OutputFiles\" 7 232 F \"..\\..\\OutputFiles\\result.txt\"\n" +
                "-rb -a \"..\\..\\OutputFiles\" 1 45 S" +
                "-rk \"..\\..\\OutputFiles\\k.txt\" \"..\\..\\OutputFiles\" \"..\\..\\OutputFiles\"\n" +
                "-c \"..\\..\\InputFiles\\a.jpg\" \"..\\..\\InputFiles\\a - Copy.jpg\" \"..\\..\\OutputFiles\\result.png\"\n"
                );
        }
    }
}
