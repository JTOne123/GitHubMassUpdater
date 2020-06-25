using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleLogger
{
    public class Logger
    {
        private readonly string filepath;

        public Logger(string filepath)
        {
            this.filepath = filepath;
        }

        public void Log(Exception ex)
        {
            CreateDir();

            File.AppendAllLines(filepath, new List<string> { $"[ERROR] {ex.Message} {ex.StackTrace}" });
        }
        public void Log(string info)
        {
            CreateDir();

            File.AppendAllLines(filepath, new List<string> { $"[INFO] {info}" });
        }

        private void CreateDir()
        {
            var path = Path.GetDirectoryName(filepath);
            Directory.CreateDirectory(path);
        }
    }
}