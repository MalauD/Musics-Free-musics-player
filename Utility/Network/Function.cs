﻿using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Utility.Network
{
    public class Function
    {
        public static MessageTCP Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return new MessageTCP { Data = memoryStream.ToArray() };
            }
        }

        public static object Deserialize(MessageTCP message)
        {
            using (var memoryStream = new MemoryStream(message.Data))
            {
                return (new BinaryFormatter()).Deserialize(memoryStream);
            }
        }

        public static float Map(float n, float start1, float stop1, float start2, float stop2)
        {
            return ((n - start1) / (stop1 - start1)) * (stop2 - start2) + start2;
        }

        public static string[] GetFiles(string SourceFolder, string Filter,System.IO.SearchOption searchOption)
        {
            List<string> alFiles = new List<string>();
            string[] MultipleFilters = Filter.Split('|');
            foreach (string FileFilter in MultipleFilters)
                alFiles.AddRange(Directory.GetFiles(SourceFolder, FileFilter, searchOption));
            return (string[])alFiles.ToArray();
        }
    }
}
