using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TAssetBundle
{
    public abstract class WebRequestCommand
    {
        protected readonly string _url;
        public int retryCount;
        public bool useRetry;
        internal InnerOperations innerOperations;

        public string Url => _url;
        internal virtual bool IgnoreError => false;

        public Action<UnityWebRequest> onComplete;
        public Action<UnityWebRequest> onProgress;

        internal WebRequestCommand(InnerOperations innerOperations, string url)
        {
            _url = url;
            this.innerOperations = innerOperations;
        }

        internal abstract UnityWebRequest GetRequest();
        
        
    }


    public class WebRequestCommandGet : WebRequestCommand
    {
        internal WebRequestCommandGet(InnerOperations innerOperations, string url)
            : base(innerOperations, url)
        {
        }

        internal override UnityWebRequest GetRequest()
        {
            return UnityWebRequest.Get(_url);
        }
    }

    public class WebRequestCommandAssetBundle : WebRequestCommand
    {
        private readonly Hash128 _hash;

        internal WebRequestCommandAssetBundle(InnerOperations innerOperations, string url, Hash128 hash)
            : base(innerOperations, url)
        {
            _hash = hash;
        }

        internal override UnityWebRequest GetRequest()
        {
            return UnityWebRequestAssetBundle.GetAssetBundle(_url, _hash);
        }
    }

    public class WebRequestCommandHead : WebRequestCommand
    {
        internal override bool IgnoreError => true;

        internal WebRequestCommandHead(InnerOperations innerOperations, string url)
            : base(innerOperations, url)
        {
        }

        internal override UnityWebRequest GetRequest()
        {
            return UnityWebRequest.Head(_url);
        }
    }



    public class WebRequestAsync : IEnumerator
    {
        public WebRequestCommand Command { get; private set; }
        public UnityWebRequest Request { get; private set; }
        public bool IsDone { get; private set; }
        public object Current => null;

        public event Action<WebRequestAsync> OnComplete;

        public WebRequestAsync(WebRequestCommand command, Action<WebRequestCommand> action)
        {
            Command = command;
            Command.onComplete += OnCompleteRequest;
            action(command);
        }

        private void OnCompleteRequest(UnityWebRequest request)
        {
            IsDone = true;
            Request = request;
            OnComplete?.Invoke(this);
        }

        public bool MoveNext()
        {
            return !IsDone;
        }

        public void Reset()
        {
        }
    }

    public delegate void WebRequestCallback(UnityWebRequest webRequest, WebRequestCommand command);

    internal class WebRequest
    {
        public struct Option
        {
            public int maxConcurrentRequestCount;
            public int maxRetryRequestCount;
            public float retryRequestWaitDuration;
            public bool enableDebuggingLog;
        }

        private Option _option;
        private int _currentRequestCount = 0;
        private readonly Queue<WebRequestCommand> _requestQueue = new Queue<WebRequestCommand>();

        public bool EnableDebuggingLog => _option.enableDebuggingLog;

        public event WebRequestCallback OnBeforeSend;
        public event WebRequestCallback OnComplete;
        public event WebRequestCallback OnError;

        public WebRequest(Option option)
        {
            _option = option;
        }

        public WebRequestAsync GetAsync(InnerOperations innerOperations, string url, Action<UnityWebRequest> onProgress = null, bool useRetry = true)
        {
            return new WebRequestAsync(new WebRequestCommandGet(innerOperations, url)
            {
                onProgress = onProgress,
                useRetry = useRetry

            }, Request);
        }

        public WebRequestAsync GetAssetBundleAsync(InnerOperations innerOperations, string url, Hash128 hash, Action<UnityWebRequest> onProgress = null)
        {
            return new WebRequestAsync(new WebRequestCommandAssetBundle(innerOperations, url, hash)
            {
                onProgress = onProgress,
                useRetry = true

            }, Request);
        }

        private void RetryRequest(WebRequestCommand command)
        {
            command.retryCount += 1;

            Request(command);
        }

        private void Request(WebRequestCommand command)
        {
            if (_currentRequestCount >= _option.maxConcurrentRequestCount)
            {
                _requestQueue.Enqueue(command);

                if (EnableDebuggingLog)
                {
                    Logger.Log("reserved request count - " + _requestQueue.Count);
                }
            }
            else
            {
                InternalRequest(command);
            }
        }

        private void InternalRequest(WebRequestCommand command)
        {
            CoroutineHandler.Instance.StartCoroutine(InternalRequestCoroutine(command));
        }

        private IEnumerator InternalRequestCoroutine(WebRequestCommand command)
        {
            ++_currentRequestCount;

            var request = command.GetRequest();

            OnBeforeSend?.Invoke(request, command);

            if (command.retryCount > 0)
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("retry request - url:{0}, retryCount:{1}, requestCount:{2}",
                        request.url, command.retryCount, _currentRequestCount));
                }
            }
            else
            {
                if (EnableDebuggingLog)
                {
                    Logger.Log(string.Format("request - url:{0}, requestCount:{1}", request.url, _currentRequestCount));
                }
            }

            var requestAsyncOperation = request.SendWebRequest();

            command.innerOperations?.Add(requestAsyncOperation);

            while (!requestAsyncOperation.isDone)
            {
                command.onProgress?.Invoke(request);
                yield return null;
            }

            command.onProgress?.Invoke(request);

            try
            {
                if (request.IsSuccess())
                {
                    if (EnableDebuggingLog)
                    {
                        Logger.Log(string.Format("response success - url:{0}", request.url));
                    }

                    command.onComplete?.Invoke(request);
                    OnComplete?.Invoke(request, command);
                }
                else if(command.IgnoreError)
                {
                    if (EnableDebuggingLog)
                    {
                        Logger.Log(string.Format("ignored response error - url:{0}, responseCode:{1}, error:{2}",
                            request.url, request.responseCode, request.error));
                    }

                    command.onComplete?.Invoke(request);
                }
                else
                {
                    Logger.Warning(string.Format("response error - url:{0}, responseCode:{1}, error:{2}",
                        request.url, request.responseCode, request.error));

                    if (command.useRetry && command.retryCount < _option.maxRetryRequestCount)
                    {
                        yield return new WaitForSeconds(_option.retryRequestWaitDuration);

                        RetryRequest(command);
                    }
                    else
                    {
                        command.onComplete?.Invoke(request);
                        OnError?.Invoke(request, command);
                    }
                }

                --_currentRequestCount;

                if (_requestQueue.Count > 0)
                {
                    var reservedCommand = _requestQueue.Dequeue();

                    if (EnableDebuggingLog)
                    {
                        Logger.Log("reserved request count - " + _requestQueue.Count);
                    }

                    Request(reservedCommand);
                }

                yield return new WaitForSeconds(1f);
            }
            finally
            {
                request.Dispose();
            }
        }
    }
}