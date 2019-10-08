// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.Threading.Tasks;

namespace DataX.Config
{
    public interface IRuntimeConfigStorage
    {
        /// <summary>
        /// Write a file to the given path
        /// </summary>
        /// <param name="destinationPath">file path to save</param>
        /// <param name="content">conent to write</param>
        /// <returns>path to the destination path</returns>
        Task<string> SaveFile(string destinationPath, string content);

        /// <summary>
        /// Deletes the destination file or folder
        /// </summary>
        /// <param name="destinationPath">path to file or folder to be deleted</param>
        /// <returns></returns>

        Task<string> Delete(string destinationPath);


        /// <summary>
        /// Deletes the destination files in folder
        /// </summary>
        /// <param name="destinationPath">path to file or folder to be deleted</param>
        /// <returns></returns>

        Task<string> DeleteAll(string destinationPath);
    }
}
