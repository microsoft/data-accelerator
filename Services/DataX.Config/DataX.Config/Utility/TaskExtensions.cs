// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataX.Config
{
    public static class TaskExtensions
    {
        public static async Task SequentialTask<T>(this IEnumerable<T> inputs, Func<T, Task> processor)
        {
            foreach (var input in inputs)
            {
                await processor(input);
            }
        }

        public static async Task ChainTask<T>(this IEnumerable<T> inputs, Func<T, Task> processor) where T: ISequentialProcessor
        {
            foreach(var taskGroup in inputs.GroupBy(i => i.GetOrder()))
            {
                var groupInputs = taskGroup.ToList();
                if (groupInputs.Count == 1)
                {
                    await processor(groupInputs[0]);
                }
                else
                {
                    await Task.WhenAll(groupInputs.Select(i => processor(i)));
                }
            }
        }
    }
}
