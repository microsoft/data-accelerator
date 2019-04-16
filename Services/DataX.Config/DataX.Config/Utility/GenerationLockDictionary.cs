// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataX.Config
{
    /// <summary>
    /// A thread-safe locks dictionary
    /// </summary>
    internal class GenerationLockDictionary
    {
        private readonly Dictionary<string, object> _locks = new Dictionary<string, object>();

        public class Lock: IDisposable
        {
            private readonly string _key;
            private readonly GenerationLockDictionary _dict;

            public Lock(GenerationLockDictionary dict, string key)
            {
                this._key = key;
                this._dict = dict;
            }

            public void Dispose()
            {
                this._dict.RemoveLockItem(_key);
                GC.SuppressFinalize(this);
            }
        }

        public GenerationLockDictionary()
        {
        }

        /// <summary>
        /// check if the given item is locked
        /// </summary>
        /// <param name="itemName">given name to check see if it is locked</param>
        /// <returns>True if it is locked or else False</returns>
        internal bool IsLocked(string itemName)
        {
            throw new NotImplementedException();
        }

        internal void LockItem(string itemName)
        {
            throw new NotImplementedException();
        }

        internal void RemoveLockItem(string itemName)
        {
            lock (this)
            {
                this._locks.Remove(itemName);
            }
        }

        /// <summary>
        /// Return a scope within which the lock is on, and the lock is off in exiting.
        /// </summary>
        /// <param name="itemName">given name to lease the lock</param>
        /// <returns>a scope of the lock</returns>
        internal IDisposable GetLock(string itemName)
        {
            lock (this)
            {
                if (this._locks.ContainsKey(itemName))
                {
                    return null;
                }
                else
                {
                    this._locks.Add(itemName, new object());
                    return new Lock(this, itemName);
                }
            }
        }
    }
}
