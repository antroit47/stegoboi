using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Stegoboi
{
    /*
     * An object to represent a file, which can be hidden in an image. 
     * As most methods of ImageManager work with objects, it is 
     * better to have a file represented as object aswell.
     */ 
    [Serializable()]
    class StegoFile : ISerializable 
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }

        public StegoFile(string name, string path)
        {
            Content = null;
            FileName = null;
            try
            {
                FileName = name;
                FileStream stream = File.OpenRead(path);
                Content = new byte[stream.Length];
                stream.Read(Content, 0, Content.Length);
                stream.Close();
            }
            catch (Exception)
            {
                Console.WriteLine("FILE READING ERROR");
            }
            
        }

        public StegoFile(SerializationInfo info, StreamingContext ctxt)
        {
            FileName = (string)info.GetValue("Filename", typeof(string));
            Content = (byte[])info.GetValue("Content", typeof(byte[]));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Filename", FileName);
            info.AddValue("Content", Content);
        }

        public async Task SaveAsync(string path) //path has to include the file name + extension
        {
            try
            {
                using (Stream file = File.OpenWrite(path))
                {
                    await file.WriteAsync(Content, 0, Content.Length);
                    Console.WriteLine("File " + FileName + " was saved successfully");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("SAVING ERROR: cannot save file: " + FileName + " as (" + path + ")");
            }
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                StegoFile f = (StegoFile)obj;
                return (FileName == f.FileName) && (Content.SequenceEqual(f.Content));
            }
        }

        public override int GetHashCode()
        {
            var hashCode = -149129942;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Content);
            return hashCode;
        }
    }
}
