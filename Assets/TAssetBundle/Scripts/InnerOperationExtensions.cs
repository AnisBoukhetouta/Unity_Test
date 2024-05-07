using UnityEngine;

namespace TAssetBundle
{
    internal class AsyncOperationInner : IInnerOperation
    {
        private readonly AsyncOperation _asyncOperation;

        public AsyncOperation Operation => _asyncOperation;

        public AsyncOperationInner(AsyncOperation asyncOperation)
        {
            _asyncOperation = asyncOperation;
        }

        public float Progress => _asyncOperation.progress;
    }



    internal static class InnerOperationExtensions
    {
        internal static void Add(this InnerOperations innerOperations, AsyncOperation asyncOperation)
        {
            innerOperations.Add(new AsyncOperationInner(asyncOperation));
        }
    }

}