using CommanderDiscord.Data.Models.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CommanderDiscord.Services
{
    public class FileService
    {

        public FileService()
        { }

        public async Task<Stream> GetFileAsync(string asdasd)
        {
            Stream s = new FileStream( asdasd,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true);
            return s;
        }
    }
}
