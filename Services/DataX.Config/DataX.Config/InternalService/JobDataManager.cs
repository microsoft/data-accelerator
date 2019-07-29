// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.Utility;
using DataX.Contract;
using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;

namespace DataX.Config
{
    [Shared]
    [Export]
    public class JobDataManager
    {
        [ImportingConstructor]
        public JobDataManager(IRuntimeConfigStorage storage)
        {
            this.Storage = storage;
        }

        private IRuntimeConfigStorage Storage { get; }

        /// <summary>
        /// Figure out the destination folder of runtime configs
        ///     Resolve the destination folder from flow config
        ///     Calculate the version of generation
        ///     Finalize the destination folder with the configured one and the version
        /// </summary>
        /// <param name="baseFolderPath">the base folder path to determine the final destination folder for job configs</param>
        /// <param name="version">the version to enclose in the folder path</param>
        /// <returns></returns>
        public string FigureOutDestinationFolder(string baseFolderPath, string version)
        {
            if (baseFolderPath == null)
            {
                return null;
            }

            var folderName = "Generation_" + version;
            return ResourcePathUtil.Combine(baseFolderPath, folderName);
        }

        /// <summary>
        /// Write content to the given path
        /// </summary>
        /// <param name="destinationPath">file path to save</param>
        /// <param name="content">conent to write</param>
        /// <returns>path to the destination path</returns>
        public async Task<string> SaveFile(string destinationPath, string content)
        {
            return await this.Storage.SaveFile(destinationPath, content);
        }

        /// <summary>
        /// Delete the root folder containing all the job configs
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public async Task<string> DeleteConfigs(string destinationPath)
        {
            return await this.Storage.Delete(destinationPath);
        }

        /// <summary>
        /// Delete the root folder containing all the job configs
        /// </summary>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public async Task<string> DeleteAll(string destinationPath)
        {
            return await this.Storage.DeleteAll(destinationPath);
        }
    }
}
