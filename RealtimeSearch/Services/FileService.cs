﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeeLaboratory.RealtimeSearch
{
    public class FileService : IFileService
    {
        private readonly object _lock = new();

        public void Delete(string folderPath, string fileName)
        {
            lock (_lock)
            {
                var path = Path.Combine(folderPath, fileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public T? Read<T>(string folderPath, string fileName)
        {
            lock (_lock)
            {
                var path = Path.Combine(folderPath, fileName);
                if (!File.Exists(path)) return default;

                var json = File.ReadAllBytes(path);
                return Read<T>(new ReadOnlySpan<byte>(json));
            }
        }

#if false
        private static T? Read<T>(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return Read<T>(new ReadOnlySpan<byte>(ms.ToArray()));
            }
        }
#endif

        private static T? Read<T>(ReadOnlySpan<byte> json)
        {
            return JsonSerializer.Deserialize<T>(json, CreateJsonSerializerOptions());
        }


        public void Write<T>(string folderPath, string fileName, T content, bool isCreateBackup)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(content, CreateJsonSerializerOptions());

            lock (_lock)
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var destinationPath = Path.Combine(folderPath, fileName);
                var sourcePath = destinationPath + ".new";
                var backupPath = isCreateBackup ? destinationPath + ".bkup" : null;

                if (File.Exists(destinationPath))
                {
                    File.WriteAllBytes(sourcePath, json);
                    File.Replace(sourcePath, destinationPath, backupPath);
                }
                else
                {
                    File.WriteAllBytes(destinationPath, json);
                }
            }
        }


        private static JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
                IgnoreReadOnlyProperties = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
