using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using YamlDotNet;

namespace RealtimeSearch
{
    [Serializable]
    public class Config
    {
        public string[] SearchPaths { set; get; }

        public bool IsMonitorClipboard { set; get; }
        
        [YamlDotNet.Serialization.YamlIgnore]
        public string Path { set; get; }


        //----------------------------------------------------------------------------
        public Config()
        {
        }

        //----------------------------------------------------------------------------
        // シリアライズによるクローン
        public Config Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter f = new BinaryFormatter();
                f.Serialize(stream, this);
                stream.Position = 0L;
                return (Config)f.Deserialize(stream);
            }
        }

        //----------------------------------------------------------------------------
        // シリアライズの実験
        public string Serialize()
        {
            var serializer = new YamlDotNet.Serialization.Serializer();

            StringBuilder sb = new StringBuilder();
            using (StringWriter w = new StringWriter(sb))
            {
                serializer.Serialize(w, this);
            }

            //Save("HOGE.yaml");
            //Load("HOGE.yaml");

            return sb.ToString();
        }

        //----------------------------------------------------------------------------
        public void Save(string path)
        {
            using (var writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                var serializer = new YamlDotNet.Serialization.Serializer();
                serializer.Serialize(writer, this);
                this.Path = path;
            }
        }

        //----------------------------------------------------------------------------
        public static Config Load(string path)
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                var serializer = new YamlDotNet.Serialization.Deserializer();
                Config config = serializer.Deserialize<Config>(reader);
                config.Path = path;
                return config;
            }
        }
    }

}
