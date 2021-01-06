using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace NegativeEddy.Bots.Composer.Serialization
{
    class ObjectSerializer
    {
        private readonly JsonSerializer _serializer = new JsonSerializer();

        public object FromStream(Stream stream)
        {
            using (stream)
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
                    {
                        return _serializer.Deserialize(jsonTextReader);
                    }
                }
            }
        }

        public Stream ToStream(object input)
        {
            MemoryStream streamPayload = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(streamPayload, encoding: Encoding.Default, bufferSize: 1024, leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Formatting.None;
                    _serializer.Serialize(writer, input);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            streamPayload.Position = 0;
            return streamPayload;
        }
    }
}
