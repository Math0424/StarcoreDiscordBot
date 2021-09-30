using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace StarcoreDiscordBot
{
    class ReaderWriter
    {

        public static void Save<T>(T objectToSave, string filePath)
        {
            var ser = new DataContractSerializer(typeof(T));
            var sett = new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t",
            };
            using var writer = XmlWriter.Create(filePath, sett);
            ser.WriteObject(writer, objectToSave);
            writer.Close();
        }

        public static T Load<T>(string filePath)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open);
            using var reader = XmlDictionaryReader.CreateTextReader(fileStream, new XmlDictionaryReaderQuotas());
            var serialize = new DataContractSerializer(typeof(T));
            T obj = (T)serialize.ReadObject(reader, true);
            reader.Close();
            fileStream.Close();
            return obj;
        }

    }
}
