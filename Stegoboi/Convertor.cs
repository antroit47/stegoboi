using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Stegoboi
{
    /*
     * An object which can convert relevant information
     * (strings / serializable objects) to byte arrays and vice versa
     */
    public static class Convertor
    {
        public static byte[] StringToBytes(string s)
        {
            return Encoding.ASCII.GetBytes(s);
        }

        public static string BytesToString(byte[] b)
        {
            return Encoding.ASCII.GetString(b);
        }
        
        public static byte[] ObjectToBytes(object obj)
        {
            if (obj == null)
            {
                Console.WriteLine("ERROR from ObjectToBytes: The object is null");
                return null;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                byte[] bytes = ms.ToArray();
                ms.Flush();
                return bytes;
            }
            
        }

        public static object BytesToObject(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {   try
                {
                    ms.Position = 0;
                    object obj = new BinaryFormatter().Deserialize(ms);
                    return obj;
                }
                catch (Exception)
                {
                    Console.WriteLine("ERROR from BytesToObject: Invalid object within an Image");
                    return null;
                }
            }
        }
    }
}
