using System.Threading.Tasks;
using UnityEngine;

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace TAssetBundle
{
    public static class TAsyncOperationExtensions
    {
        /// <summary>
        /// Return TAsyncOperation as a Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncOperation"></param>
        /// <returns></returns>
        public static async Task<T> ToTask<T>(this TAsyncOperationProgress<T> asyncOperation)
        {
            await ToTask((TAsyncOperationBase)asyncOperation);

            return asyncOperation.Result;
        }

        /// <summary>
        /// Return TAsyncOperation as a Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncOperation"></param>
        /// <returns></returns>
        public static async Task<T> ToTask<T>(this TAsyncOperation<T> asyncOperation)
        {
            await ToTask((TAsyncOperationBase)asyncOperation);

            return asyncOperation.Result;
        }

        /// <summary>
        /// Return TAsyncOperation as a Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncOperation"></param>
        /// <returns></returns>
        public static async Task ToTask(this TAsyncOperationBase asyncOperation)
        {
            while (!asyncOperation.IsDone)
            {
                await Task.Yield();
            }
        }

#if USE_UNITASK
        public static UniTask ToUniTask(this TAsyncOperationBase asyncOperation)
        {
            return UniTask.WaitUntil(() => asyncOperation.IsDone);
        }

        public static UniTask<T> ToUniTask<T>(this TAsyncOperation<T> asyncOperation)
        {
            return UniTask.WaitUntil(() => asyncOperation.IsDone).ContinueWith(() => asyncOperation.Result);
        }
        
        public static UniTask.Awaiter GetAwaiter(this TAsyncOperation asyncOperation)
        {
            return asyncOperation.ToUniTask().GetAwaiter();
        }

        public static UniTask<T>.Awaiter GetAwaiter<T>(this TAsyncOperation<T> asyncOperation)
        {
            return asyncOperation.ToUniTask().GetAwaiter();
        }
#endif
    }

    public static class AsyncOperationExtensions
    {
        /// <summary>
        /// Returns an AsyncOperation as a Task
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="asyncOperation"></param>
        /// <returns></returns>
        public static async Task<T> ToTask<T>(this T asyncOperation) where T : AsyncOperation
        {
            while (!asyncOperation.isDone)
            {
                await Task.Yield();
            }

            return asyncOperation;
        }

#if USE_UNITASK
        public static UniTask ToUniTask(this AsyncOperation asyncOperation)
        {
            return UniTask.WaitUntil(() => asyncOperation.IsDone);
        }

        public static UniTask<T> ToUniTask<T>(this T asyncOperation) where T : AsyncOperation
        {
            return UniTask.WaitUntil(() => asyncOperation.IsDone).ContinueWith(() => asyncOperation.Result);
        }

        public static UniTask.Awaiter GetAwaiter(this AsyncOperation asyncOperation)
        {
            return asyncOperation.ToUniTask().GetAwaiter();
        }

        public static UniTask<T>.Awaiter GetAwaiter<T>(this T asyncOperation) where T : AsyncOperation
        {
            return asyncOperation.ToUniTask().GetAwaiter();
        }
#endif
    }

}