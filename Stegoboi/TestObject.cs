using System;
using System.Runtime.Serialization;

namespace Stegoboi
{
    /*
     * This is a very simple serializable testing object.
     */
    [Serializable()]
    class TestObject : ISerializable
    {
        string Test1 { get; set; }
        int Test2 { get; set; }

        public TestObject(string a, int b)
        {
            Test1 = a;
            Test2 = b;
        }

        public TestObject(SerializationInfo info, StreamingContext ctxt)
        {
            Test1 = (string)info.GetValue("Test1", typeof(string));
            Test2 = (int)info.GetValue("Test2", typeof(int));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Test1", Test1);
            info.AddValue("Test2", Test2);
        }

        public string Info()
        {
            return Test1 + Test2.ToString();
        }
    }
}
