using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;


namespace Catneep.Data
{
    public static class DataManager
    {

        //Save and loading archives

        //Save
        /*
        public const string saveFileName = "SaveFile";

        public static void SaveGame()
        {
            SaveData(new SaveFile(), Application.dataPath, saveFileName);
        }
        */

        public static void SaveData(object data, string path, string fileName)
        {
            SaveData(data, Path.Combine(path, fileName));
        }
        public static void SaveData(object data, string fileFullPath)
        {
            FileStream stream = File.Open(fileFullPath, FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter { Binder = new VersionDeserializationBinder() };
            bformatter.Serialize(stream, data);
            stream.Close();
        }


        //Load

        public static ReturnT LoadData<ReturnT>(string path, string fileName, string extension)
        {
            return LoadData<ReturnT>(path, fileName + "." + extension);
        }
        public static ReturnT LoadData<ReturnT>(string path, string fileName)
        {
            return LoadData<ReturnT>(Path.Combine(path, fileName));
        }
        public static ReturnT LoadData<ReturnT>(string fileFullPath)
        {
            return ReadDataFromStream<ReturnT>(File.Open(fileFullPath, FileMode.Open));
        }

        public static ReturnT ReadDataFromTextAsset<ReturnT>(TextAsset textAsset)
        {
            return ReadDataFromStream<ReturnT>(new MemoryStream(textAsset.bytes));
        }
        public static ReturnT ReadDataFromStream<ReturnT>(Stream stream)
        {
            BinaryFormatter bformatter = new BinaryFormatter { Binder = new VersionDeserializationBinder() };
            object data = bformatter.Deserialize(stream);
            stream.Close();

            return (ReturnT)data;
        }

        //Binary serializer
        private sealed class VersionDeserializationBinder : SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(typeName))
                {
                    Type typeToDeserialize = TryToGetOldType(typeName);

                    if (typeToDeserialize != null) return typeToDeserialize;

                    assemblyName = Assembly.GetExecutingAssembly().FullName;
                    // The following line of code returns the type. 
                    typeToDeserialize = Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
                    return typeToDeserialize;
                }
                return null;
            }
            Type TryToGetOldType(string typeName)
            {
                switch (typeName)
                {
                    // Example:
                    //case "OldType":
                    //  return typeof(NewType);
                    default:
                        return null;
                }
            }
        }
    }
}
