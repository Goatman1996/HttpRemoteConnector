using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace HttpRemoteConnector
{
    public class RemoteListenerManager
    {
        private static RemoteListenerManager _Instance;
        public static RemoteListenerManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new RemoteListenerManager();
                }
                return _Instance;
            }
        }

        private RemoteListenerManager()
        {
            this.listenerDic = new Dictionary<int, HttpListener>();
            var url = GetLocalIPAddress();
            this.myIp = $"http://{url}";
        }

        private string GetLocalIPAddress()
        {
            var ip4List = new List<string>();
            System.Net.IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var ip in addressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ip4List.Add(ip.ToString());
                }
            }

            return ip4List[ip4List.Count - 1];
        }

        public string myIp { get; private set; }
        private Dictionary<int, HttpListener> listenerDic;

        private bool IsListened(int port)
        {
            return this.listenerDic.ContainsKey(port);
        }

        public void StartListener(IRemoteListener remoteListener)
        {
            var port = remoteListener.port;
            var isListened = this.IsListened(port);
            if (isListened)
            {
                throw new System.Exception($"[HttpRemoteConnector] {port} is Listening already");
            }
            var url = $"{this.myIp}:{port}/";
            UnityEngine.Debug.LogError($"Listen URL {url}");
            var httpListener = new HttpListener();
            try
            {
                httpListener.Prefixes.Add(url);
                httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                httpListener.Start();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return;
            }

            this.listenerDic.Add(port, httpListener);
            this.Loop(httpListener, remoteListener);
        }

        private async void Loop(HttpListener httpListener, IRemoteListener listener)
        {
            while (true)
            {
                if (UnityEngine.Application.isPlaying == false)
                {
                    httpListener.Close();
                    return;
                }

                var context = await httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                var response = context.Response;
                response.ContentEncoding = Encoding.UTF8;
                response.ContentType = "application/json; charset = utf-8";

                var responseMsg = "";
                if (request.HttpMethod == "GET")
                {
                    var getParam = this.ConvertGetParam(request);
                    if (getParam.Length == 1 && string.IsNullOrEmpty(getParam[0]))
                    {
                        responseMsg = listener.GetType().FullName;
                    }
                    else
                    {
                        responseMsg = listener.GetHandler(getParam);
                    }
                }
                else if (request.HttpMethod == "POST")
                {
                    var postParam = this.ConvertPostParam(request);
                    responseMsg = listener.PostHandler(postParam);
                }
                else { }

                byte[] msg = Encoding.UTF8.GetBytes(responseMsg);
                var opStream = response.OutputStream;
                opStream.Write(msg, 0, msg.Length);
                opStream.Flush();
                opStream.Close();
            }
        }

        private string[] ConvertGetParam(HttpListenerRequest request)
        {
            var rawUrl = request.RawUrl;
            // 删掉第一个 '/'
            rawUrl = rawUrl.Remove(0, 1);
            return rawUrl.Split('/');
        }

        private string ConvertPostParam(HttpListenerRequest request)
        {
            var inputStream = request.InputStream;
            var encoding = Encoding.UTF8;
            var streamReader = new StreamReader(inputStream, encoding);
            var requestMsg = streamReader.ReadToEnd();
            return requestMsg;
        }
    }
}