# Stegoboi 1.0


#KEYWORDS IN THE USAGE SECTION:

path           = path to a file, enclose with " " in case it contains spaces.

inputImageList = paths to all input images sparated by spaces. 
                 Can be replaced by -a[ directoryPath]

-a [directoryPath] = this reads all .png, .bmp, .jpg files in specified folder
                     (current folder if folderPath is empty)
                     
directoryPath    = path to a directory (trailing \\ is not necessary), enclose in " " if it 

                   contains spaces (do not type the trailing \\ in this case)
                   
lowestBits       = int (0-8), the least signifficant color bits which are used to hide/extract the data

dataLength       = the length of the hidden data needed for its extraction

outputImageList  = paths to all output images sparated by spaces. 

                   Can be replaced by -o [directoryPath]
-o [directoryPath] = this keeps all the original names and saves the files into the folderPath
                     or current directory if folderPath is empty
[-k [keyPath]]   = generates a key (simplifies the process of extracting the data later).
                   If the keyPath is empty or if it only specifies the directory, random 
                   key name will be selected
dataSpecifier    = specifies the data and the type to be hidden
                   S "secret data string"           for string data
                   I secretImagePath                for an image 
                   F secretFilePath                 for a file of different type
resultTypeSpecifier = specifies the type of the data to be extracted + the path to save it
                   S                                for string data - displayed in console
                   I extractedImagePath             for an output image 
                   F extractedFilePath              for an output file of different type
[-r]                = -r introduces random fill on all remaining free space in an image 
                      after the data to hide runs out
keyImageDirPath = path to a directory containing all images used by a key
                  
USAGE:
-i  imagePath  = enumerate info about an image
-hs inputImagePath lowestBits dataSpecifier outputImagePath [-k keyPath] [-r] 
       = hide data into a single image
-hw inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] 
       = hide same data into multiple images (watermarking)
-hb inputImageList/-a lowestBits dataSpecifier outputImageList/-o [-k keyPath] [-r] 
       = split larger amount of data acros multiple images (their order matters for correct extraction)
-rs inputImagePath lowestBits dataLength resultTypeSpecifier      
       = read the data from a single image
-rw inputImageList/-a lowestBits dataLength resultTypeSpecifier        
       = read data from multiple images and check if its the same (watermark checking)
-rb inputImageList/-a lowestBits dataLength resultTypeSpecifier        
       = read and combine larger data across multiple images
-rk keyPath keyImageDirPath [resultDirectoryPath]       
       = reading the data from the sources specified by the key 
         In case the extracted data is an image/file, the resultDirectoryPath will be used to save it.
-c imagePath1 imagePath2 [-p]   
       = compare two images of the same dimensions for difference
         -p makes the difference progressive(the bigger the differrence in pixels, the lighter the color)
         with -p left out, any difference will be a white pixel
-v     = print version number
-h     = print this manual

EXAMPLES:
 -hs "..\..\InputFiles\a.jpg" 7 F  "..\..\InputFiles\a.mp3" "..\..\OutputFiles\result.jpg" -k "..\..\OutputFiles\k.txt" -r
 -hw "..\..\InputFiles\a.jpg" "..\..\InputFiles\100.jpg" "..\..\InputFiles\image2.png" 7 S  "asdqwe qasd sefg sdfg" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
 -hb -a "..\..\InputFiles" 7 F  "..\..\InputFiles\a.mp3" -o "..\..\OutputFiles" -k "..\..\OutputFiles" -r
 -rs "..\..\OutputFiles\result.jpg" 7 20654 I "..\..\OutputFiles\secret.jpg"
 -rw -a "..\..\OutputFiles" 7 232 F "..\..\OutputFiles\result.txt"
 -rb -a "..\..\OutputFiles" 1 45 S
 -rk "..\..\OutputFiles\k.txt" "..\..\OutputFiles" "..\..\OutputFiles"
 -c "..\..\InputFiles\a.jpg" "..\..\InputFiles\a - Copy.jpg" "..\..\OutputFiles\result.png"
