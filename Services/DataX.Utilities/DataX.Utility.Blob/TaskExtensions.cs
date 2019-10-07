// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace DataX.Utilities.Storage
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Returns an awaitable which is configured to continue on any context
        /// </summary>
        public static ConfiguredTaskAwaitable<T> ContinueOnAnyContext<T>(this Task<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Returns an awaitable which will is configured to continue on any context
        /// </summary>
        public static ConfiguredTaskAwaitable ContinueOnAnyContext(this Task task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        /// <summary>
        /// Synchronously waits on a task and returns its result.  If the task threw an exception, this unwraps it and rethrows it preserving the original stack trace
        /// </summary>
        public static T SyncResultOrException<T>(this Task<T> task)
        {
            try
            {
                return task.Result;
            }
            catch (AggregateException ae)
            {
                var innerException = ae.Flatten().InnerException;
                ExceptionDispatchInfo.Capture(innerException).Throw();
                return default(T); // Unreachable
            }
        }

        /// <summary>
        /// Synchronously waits on a task.  If the task threw an exception, this unwraps it and rethrows it preserving the original stack trace
        /// </summary>
        public static void SyncWaitOrException(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                var innerException = ae.Flatten().InnerException;
                ExceptionDispatchInfo.Capture(innerException).Throw();
            }
        }

        /// <summary>
        /// Suppresses warnings about unawaited tasks and ensures that unhandled
        /// errors will cause the process to terminate.
        /// </summary>
        public static async void DoNotWait(this Task task)
        {
            await task.ContinueOnAnyContext();
        }

        /// <summary>
        /// Logs all exceptions from a task except those that return true from
        /// <see cref="IsCriticalException"/>, which are rethrown.
        /// </summary>
        public static async Task HandleAllExceptions(
            this Task task,
            Type callerType = null,
            [CallerFilePath] string callerFile = null,
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerName = null
        )
        {
            try
            {
                await task.ContinueOnAnyContext();
            }
            catch (Exception ex)
            {
                if (ex.IsCriticalException())
                {
                    throw;
                }

                var message = GetUnhandledExceptionString(ex, callerType, callerFile, callerLineNumber, callerName);
                // Send the message to the trace listener in case there is
                // somebody out there listening.
                Trace.TraceError(message);
            }
        }

        /// <summary>
        /// Retries the given task exponentially.
        /// </summary>
        public static async Task RetryExponentially(this Task task, bool retryOnException = true, int maxRetryAttempts = 5)
        {
            var retries = 1;
            while (true)
            {
                try
                {
                    await task.ContinueOnAnyContext();
                    break;
                }
                catch (Exception ex)
                {
                    if (retries > maxRetryAttempts)
                    {
                        throw new RetryExceededException($"Failure to complete task in specified retries {maxRetryAttempts}", ex);
                    }

                    if (!retryOnException)
                    {
                        throw;
                    }
                }

                await Task.Delay(1000 * retries);
                retries++;
            }
        }

        public class RetryExceededException : Exception
        {
            public RetryExceededException() : base() { }

            public RetryExceededException(string message) : base(message) { }

            public RetryExceededException(string message, Exception innerEx) : base(message, innerEx) { }
        }

        private static string GetUnhandledExceptionString(
            Exception ex,
            Type callerType,
            [CallerFilePath] string callerFile = null,
            [CallerLineNumber] int callerLineNumber = 0,
            [CallerMemberName] string callerName = null
        )
        {
            if (string.IsNullOrEmpty(callerName))
            {
                callerName = callerType != null ? callerType.FullName : string.Empty;
            }
            else if (callerType != null)
            {
                callerName = callerType.FullName + "." + callerName;
            }
            return string.Format(CultureInfo.CurrentUICulture,
                @"Unhandled exception in {3} ({1}:{2})
{0}",
                ex, callerFile ?? string.Empty, callerLineNumber, callerName);
        }

    }
}
