// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;

namespace DataX.Config.Local
{
    /// <summary>
    /// Runtime storage for local based on local file system
    /// </summary>
    [Export(typeof(IRuntimeConfigStorage))]
    public class RuntimeTimeStorage : IRuntimeConfigStorage
    {
        public async Task<string> SaveFile(string destinationPath, string content)
        {
           await SaveToLocal(destinationPath, content);
           return destinationPath;
        }

        public Task<string> Delete(string destinationPath)
        {
            DeleteLocal(destinationPath);
            return Task.FromResult(destinationPath);
        }

        public Task<string> DeleteAll(string destinationPath)
        {
            throw new NotImplementedException();
        }

        private void DeleteLocal(string destinationPath)
        {
            var uri = new System.Uri(destinationPath);
            var absPath = uri.AbsolutePath;

            // Passed in path could be a file or directory.
            // Check and do appropriate delete
            if (Directory.Exists(absPath))
            {
                Directory.Delete(absPath,true);
            }
            else if (File.Exists(absPath))
            {
                File.Delete(absPath);
            }
        }

        private async Task SaveToLocal(string path, string content)
        {
            var uri = new System.Uri(path);
            var absPath = uri.AbsolutePath;
            var dir = Path.GetDirectoryName(absPath);
            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using (StreamWriter w = new StreamWriter(absPath))
            {
               await w.WriteAsync(content);
            }
        }
    }
}
