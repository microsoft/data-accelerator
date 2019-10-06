// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config.Test.Mock
{
    [Shared]
    [Export]
    [Export(typeof(IRuntimeConfigStorage))]
    public class RuntimeStorage : IRuntimeConfigStorage
    {
        public readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
     
        public string Get(string key)
        {
            return Cache.GetValueOrDefault(key);
        }

        public async Task<string> SaveFile(string destinationPath, string content)
        {
            await Task.Yield();
            Cache[destinationPath] = content;
            return destinationPath;
        }

        public Task<string> Delete(string destinationPath)
        {
            // get all the items that match the destinationPath key
            var items = Cache.Where(x => x.Key.Contains(destinationPath));
            foreach(var item in items)
            {
                Cache.Remove(item.Key);
            }
            return Task.FromResult(destinationPath);
        }

        public Task<string> DeleteAll(string destinationPath)
        {
            throw new NotImplementedException();
        }
    }
}
