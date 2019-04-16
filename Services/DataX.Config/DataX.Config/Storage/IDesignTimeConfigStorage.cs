// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using DataX.Config.ConfigDataModel;
using DataX.Contract;
using System.Threading.Tasks;

namespace DataX.Config
{
    public interface IDesignTimeConfigStorage
    {
        Task<Result> SaveByName(string name, string content, string collectionName);
        Task<string> GetByName(string name, string collectionName);
        Task<string[]> GetByNames(string[] names, string collectionName);
        Task<string[]> GetAll(string collectionName);
        Task<string[]> GetByFieldValue(string fieldValue, string fieldName, string collectionName);
        Task<Result> UpdatePartialByName(string partialFieldValue, string fieldPath, string name, string collectionName);
        Task<Result> DeleteByName(string name, string collectionName);
    }
}
