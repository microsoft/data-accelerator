// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataX.Flow.InteractiveQuery.HDInsight
{
    /// <summary>
    /// Implementation of IKernel for HDInsight stack
    /// </summary>
    public sealed class HDInsightKernel : IDisposable, IKernel
    {
        public WebSocket Socket;
        private readonly byte[] _shellBuffer, _iopubBuffer;
        private readonly List<byte> _shellRead, _iopubRead;
        private readonly string _basePath, _server;
        public readonly Guid SessionGuid;
        private readonly Dictionary<string, string> _cookies;
        private readonly X509Certificate2Collection _certificates;
        private readonly Dictionary<string, string> _headers;
        private readonly Dictionary<Guid, Action<Dictionary<string, object>>> _handlers = new Dictionary<Guid, Action<Dictionary<string, object>>>();

        public HDInsightKernel(string id, string basePath, Dictionary<string, string> cookies = null, X509Certificate2Collection certificates = null, WebSocket socket = null, Dictionary<string, string> headers = null)
        {
            Id = id;
            _basePath = basePath;
            _server = new Uri(basePath).GetLeftPart(UriPartial.Authority);
            _shellRead = new List<byte>();
            _iopubRead = new List<byte>();
            _shellBuffer = new byte[1024];
            _iopubBuffer = new byte[1024];
            _certificates = certificates;
            _headers = headers;
            SessionGuid = Guid.NewGuid();

            _cookies = cookies;
            Socket = socket ?? ConnectWebSocket("shell");
            ReadSocket();

            SendCommand(
                "kernel_info_request",
                new Dictionary<string, object>()
            );
        }

        public HDInsightKernel ReplaceSocket(WebSocket socket)
        {
            return new HDInsightKernel(
                Id,
                _basePath,
                _cookies,
                _certificates,
                socket
            );
        }

        public string Id { get; }

        private ClientWebSocket ConnectWebSocket(string target)
        {
            string shellPath = ShellPath;
            var webSocket = new ClientWebSocket();
            var options = webSocket.Options;
            if (_certificates != null)
            {
                options.ClientCertificates = _certificates;
            }
            var socketCookies = options.Cookies = new CookieContainer();
            if (_cookies != null)
            {
                foreach (var cookie in _cookies)
                {
                    socketCookies.Add(
                        new Cookie(
                            cookie.Key,
                            cookie.Value,
                            "/",
                            new Uri(_server).Host
                        )
                    );
                }
            }
            if (_headers != null)
            {
                foreach (var keyValue in _headers)
                {
                    options.SetRequestHeader(keyValue.Key, keyValue.Value);
                }
            }
            options.SetRequestHeader("Origin", _server);
            webSocket.ConnectAsync(new Uri(shellPath), CancellationToken.None).GetAwaiter().GetResult();
            return webSocket;
        }

        public string ShellPath
        {
            get
            {
                var kernelsPath = _basePath + "/api/kernels";
                string protocol = new Uri(_server).Scheme == "https" ? "wss://" : "ws://";

                var shellPath = protocol + new Uri(_server).Authority + new Uri(kernelsPath).PathAndQuery + "/" + Id + "/" + "channels?session_id=" + SessionGuid.ToString("N");
                return shellPath;
            }
        }

        private void ReadSocket()
        {
            Socket.ReceiveAsync(new ArraySegment<byte>(_shellBuffer), CancellationToken.None).ContinueWith(ShellReader);
        }

        private void ShellReader(Task<WebSocketReceiveResult> result)
        {
            if (result.IsFaulted)
            {
                if (result.Exception.InnerException is WebSocketException)
                {
                    return;
                }

                Console.WriteLine("WebSocket Read Faulted: {0}", result.Exception.InnerException);
            }
            _shellRead.AddRange(new ArraySegment<byte>(_shellBuffer, 0, result.Result.Count));
            if (result.Result.EndOfMessage)
            {
                var msg = Encoding.UTF8.GetString(_shellRead.ToArray());
                InvokeHandler(msg);
                _shellRead.Clear();
            }

            ReadSocket();
        }

        private void InvokeHandler(string txt)
        {
            // TODO Refactor
            //var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(txt);
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(txt);
            Debug.WriteLine("Message received: " + txt);
            if (data != null && data.TryGetValue("parent_header", out object parentHeader))
            {
                //var phDict = parentHeader as Dictionary<string, object>;
                if (parentHeader is JObject phDict)
                {
                    if (phDict["msg_id"] != null)
                    {
                        //string msgId = msgIdObj as string;
                        string msgId = phDict["msg_id"].ToString();
                        // The R kernel can return guid's with only a single hyphen
                        // which Guid.ParseConnectionString doesn't like, so we do the replacement of
                        // - here to normalize the guid.
                        var msgIdGuid = Guid.Parse(msgId.Replace("-", ""));
                        if (msgIdGuid != null && _handlers.TryGetValue(msgIdGuid, out Action<Dictionary<string, object>> handler))
                        {
                            handler(data);
                        }
                    }
                }
            }
        }

        public void SendCommand(string msgType, Dictionary<string, object> content, Action<Dictionary<string, object>> handler = null)
        {
            var msgId = Guid.NewGuid();
            var msg = new Dictionary<string, object>() {
                {
                    "content",
                    content
                },
                {
                    "header",
                    new Dictionary<string, object>() {
                        {"msg_type", msgType},
                        {"session", SessionGuid.ToString("N")},
                        {"msg_id", msgId.ToString("N") },
                        {"username", "username"},
                    }
                },
                { "parent_header", new Dictionary<string, object>() },
                { "metadata", new Dictionary<string, object>() }
            };

            if (handler != null)
            {
                _handlers[msgId] = handler;
            }
            // TODO Refactor
            //var msgText = new JavaScriptSerializer().Serialize(msg);
            JObject msgObject = JObject.FromObject(msg);
            var msgText = JsonConvert.SerializeObject(msgObject);
            SendMessage(Socket, msgText);
        }


        private void SendMessage(WebSocket socket, string msgText)
        {
            var bytes = Encoding.UTF8.GetBytes(msgText);

            socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            ).GetAwaiter().GetResult();
        }

        private const int _ExecuteCodeTimeout = 300000;

        public string ExecuteCode(string code)
        {
            return ExecuteCode(code, _ExecuteCodeTimeout, true);
        }

        public string ExecuteCode(string code, int timeout = _ExecuteCodeTimeout, bool waitForOutput = true)
        {
            return ExecuteCode(code, out string dummy, timeout, waitForOutput);
        }

        public string ExecuteCode(string code, out string error, int timeout = _ExecuteCodeTimeout, bool waitForOutput = true)
        {
            var content = new Dictionary<string, object>() {
                    {"code", code},
                    {"silent", false },
                    {"store_history", true },
                    {"user_expressions", new Dictionary<string, object>()},
                    {"allow_stdin", false}
                };

            List<string> output = new List<string>();
            string errorMsg = null;
            using (AutoResetEvent are = new AutoResetEvent(false))
            {
                bool receivedReply = false;
                bool receivedResult = false;
                SendCommand(
                    "execute_request",
                    content,
                    response => {
                        string msgType = (response["header"] as JObject)["msg_type"].ToString();
                        Debug.WriteLine(msgType);
                        Dictionary<string, object> responseContent = (response["content"] as JObject).ToObject<Dictionary<string, object>>();
                        switch (msgType)
                        {
                            case "status":
                                break;
                            case "display_data":
                                Console.WriteLine("Display data");
                                SaveContentOutput(response, output);
                                break;
                            case "error":
                                errorMsg = (string)responseContent["evalue"];
                                break;
                            case "execute_result":
                                SaveContentOutput(response, output);
                                receivedResult = true;
                                break;
                            case "execute_reply":
                                if ((string)responseContent["status"] == "error")
                                {
                                    errorMsg = (string)responseContent["evalue"];
                                }

                                receivedReply = true;
                                break;
                            case "stream":
                                output.Add((response["content"] as JObject).ToObject<Dictionary<string, object>>()["text"].ToString());
                                break;
                        }

                        if (receivedReply)
                        {
                            if (output.Count > 0 ||
                               !waitForOutput ||
                               receivedResult ||
                               !string.IsNullOrEmpty(errorMsg))
                            {
                                try
                                {
                                    are.Set();
                                }
                                catch (ObjectDisposedException)
                                {
                                }
                            }
                        }
                    }
                );
                if (!are.WaitOne(timeout))
                {
                    throw new InvalidOperationException($"No response to execution within {timeout} ms.");
                }

                error = errorMsg;
                return string.Join("", output);
            }
        }

        private static void SaveContentOutput(Dictionary<string, object> response, List<string> output)
        {
            var content = (response["content"] as JObject).ToObject<Dictionary<string, object>>();
            var data = (content["data"] as JObject).ToObject<Dictionary<string, object>>();
            if (data.TryGetValue("text/html", out object dataContent))
            {
                output.Add(dataContent.ToString());
            }
            else if (data.TryGetValue("text/plain", out dataContent))
            {
                output.Add(dataContent.ToString());
            }
        }

        public void Dispose()
        {
            try
            {
                Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).RunSynchronously();
            }
            catch (Exception)
            {
            }
        }
        public WebSocketState GetWebSocketState()
        {
            return Socket.State;
        }
    }

    internal class MessageReceivedEventArgs : EventArgs
    {
        public readonly string Message;

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }
}
