﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace GetNewsFromUniplac.MinimalCometLibrary
{
    public class ImportIO
    {
        private const string MessagingChannel = "/messaging";

        private readonly string apiKey;
        private readonly string url;
        private readonly CookieContainer cookieContainer = new CookieContainer();
        private readonly Dictionary<Guid, Query> queries = new Dictionary<Guid, Query>();
        private readonly BlockingCollection<Dictionary<string, object>> messageQueue = new BlockingCollection<Dictionary<string, object>>();

        private Guid userGuid;
        private int msgId;
        private string clientId;
        private bool isConnected;

        public ImportIO(string host = "https://query.import.io", Guid userGuid = default(Guid), string apiKey = null)
        {
            this.userGuid = userGuid;
            this.apiKey = apiKey;

            url = host + "/query/comet/";
            clientId = null;
        }

        public void Login(string username, string password, string host = "https://api.import.io")
        {
            var loginParams = "username=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password);
            var loginUrl = host + "/auth/login";
            var loginRequest = (HttpWebRequest)WebRequest.Create(loginUrl);

            loginRequest.Method = "POST";
            loginRequest.ContentType = "application/x-www-form-urlencoded";
            loginRequest.ContentLength = loginParams.Length;

            loginRequest.CookieContainer = cookieContainer;

            using (var dataStream = loginRequest.GetRequestStream())
            {
                dataStream.Write(Encoding.UTF8.GetBytes(loginParams), 0, loginParams.Length);

                var loginResponse = (HttpWebResponse)loginRequest.GetResponse();

                if (loginResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Could not log in, code:" + loginResponse.StatusCode);
                }
            }
        }

        public void Connect()
        {
            if (isConnected)
            {
                return;
            }

            Handshake();

            var subscribeData = new Dictionary<string, object> { { "subscription", MessagingChannel } };
            Request("/meta/subscribe", subscribeData);

            isConnected = true;

            new Thread(Poll).Start();

            new Thread(PollQueue).Start();
        }

        public List<string> AuthenticateConnector(string connectorGuid, string connectorAuthDomain, string domainUsername, string domainPassword)
        {
            var requestUrl = string.Format("https://api.import.io/store/connector/{0}/_query", connectorGuid);
            requestUrl = AppendApiKey(requestUrl);

            var data = new Dictionary<string, object>
            {
                { "loginOnly", true },
                { "additionalInput", new Dictionary<string, object>
                    {
                        { connectorGuid, new Dictionary<string, object>
                            {
                                { "domainCredentials", new Dictionary<string, object>
                                    {
                                        { connectorAuthDomain, new Dictionary<string, object>
                                            {
                                                { "username", domainUsername },
                                                { "password", domainPassword }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var dataJson = JsonConvert.SerializeObject(data);
            var request = BuildWebRequest(requestUrl, dataJson);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    var responseJson = responseStream.ReadToEnd();
                    var responseList = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseJson);

                    return JsonConvert.DeserializeObject<List<string>>(responseList["cookies"].ToString());
                }
            }
        }

        public List<object> list = new List<object>(); 

        public void DoQuery(Dictionary<string, object> query, QueryHandler queryHandler)
        {
            var requestId = Guid.NewGuid();
            queries.Add(requestId, new Query(queryHandler));

            query.Add("requestId", requestId);
            Request("/service/query", new Dictionary<string, object> { { "data", query } });
        }

        public void Disconnect()
        {
            Request("/meta/disconnect");
            isConnected = false;
        }

        private void Handshake()
        {
            var handshakeData = new Dictionary<string, object>();
            handshakeData.Add("version", "1.0");
            handshakeData.Add("minimumVersion", "0.9");
            handshakeData.Add("supportedConnectionTypes", new List<string> { "long-polling" });
            handshakeData.Add("advice", new Dictionary<string, int> { { "timeout", 60000 }, { "interval", 0 } });
            var responseList = Request("/meta/handshake", handshakeData, "handshake");
            clientId = (string)responseList[0]["clientId"];
        }

        private List<Dictionary<string, object>> Request(string channel, Dictionary<string, object> data = null, string path = "", bool doThrow = true)
        {
            var dataPacket = new Dictionary<string, object>
                             {
                                 { "channel", channel },
                                 { "connectionType", "long-polling" },
                                 { "id", (msgId++).ToString() }
                             };

            if (clientId != null)
            {
                dataPacket.Add("clientId", clientId);
            }

            if (data != null)
            {
                foreach (var entry in data)
                {
                    dataPacket.Add(entry.Key, entry.Value);
                }
            }

            var requestUrl = url + path;

            requestUrl = AppendApiKey(requestUrl);

            var dataJson = JsonConvert.SerializeObject(new List<object> { dataPacket });

            var request = BuildWebRequest(requestUrl, dataJson);

            try
            {
                var response = (HttpWebResponse)request.GetResponse();

                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    var responseJson = responseStream.ReadToEnd();
                    var responseList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseJson);
                    foreach (var responseDict in responseList)
                    {
                        if (responseDict.ContainsKey("successful") && (bool)responseDict["successful"] != true)
                        {
                            if (doThrow)
                            {
                                throw new Exception("Unsuccessful request");
                            }
                        }

                        if (!responseDict["channel"].Equals(MessagingChannel))
                        {
                            continue;
                        }

                        if (responseDict.ContainsKey("data"))
                        {
                            messageQueue.Add(
                                ((Newtonsoft.Json.Linq.JObject)responseDict["data"])
                                    .ToObject<Dictionary<string, object>>());
                        }
                    }

                    return responseList;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error occurred {0}", exception.Message);

                return new List<Dictionary<string, object>>();
            }
        }

        private HttpWebRequest BuildWebRequest(string requestUrl, string dataJson)
        {
            var request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
            request.ContentLength = dataJson.Length;
            request.CookieContainer = cookieContainer;

            using (var dataStream = request.GetRequestStream())
            {
                dataStream.Write(Encoding.UTF8.GetBytes(dataJson), 0, dataJson.Length);
            }

            return request;
        }

        private string AppendApiKey(string requestUrl)
        {
            if (apiKey != null)
            {
                requestUrl += "?_user=" + HttpUtility.UrlEncode(userGuid.ToString()) + "&_apikey="
                              + HttpUtility.UrlEncode(apiKey);
            }

            return requestUrl;
        }

        private void Poll()
        {
            while (isConnected)
            {
                Request("/meta/connect", null, "connect", false);
            }
        }

        private void PollQueue()
        {
            while (isConnected)
            {
                ProcessMessage(messageQueue.Take());
            }
        }

        private void ProcessMessage(Dictionary<string, object> data)
        {
            var requestId = Guid.Parse((string)data["requestId"]);
            var query = queries[requestId];

            query.OnMessage(data);
            if (query.IsFinished)
            {
                queries.Remove(requestId);
            }
        }
    }
}
