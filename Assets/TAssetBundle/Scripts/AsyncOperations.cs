using System;
using System.Collections;

namespace TAssetBundle
{
    public class TAsyncOperationBase : IEnumerator
    {
        internal InnerOperations InnerOperations { get; private set; } = new InnerOperations();

        public bool IsDone { get; protected set; }
        public object Current => null;

        public float Progress => IsDone ? 1f : InnerOperations.Progress;

        public event Action<TAsyncOperationBase> OnComplete;

        public TAsyncOperationBase()
        {
            IsDone = false;
        }

        public bool MoveNext()
        {
            return !IsDone;
        }

        public void Reset()
        {
        }

        internal void Complete()
        {
            IsDone = true;
            OnComplete?.Invoke(this);
        }
    }

    public class TAsyncOperation : TAsyncOperationBase
    {
        public new event Action OnComplete;

        internal new void Complete()
        {
            base.Complete();
            OnComplete?.Invoke();
        }
    }

    public class TAsyncOperation<ResultType> : TAsyncOperationBase
    {
        public new event Action<ResultType> OnComplete;
        public ResultType Result { get; private set; }


        internal void Complete(ResultType result)
        {
            Result = result;
            Complete();
            OnComplete?.Invoke(Result);
        }
    }

    public class TAsyncOperationProgress<ResultType> : TAsyncOperationBase
    {
        public new event Action<ResultType> OnComplete;
        public event Action<ResultType> OnProgress;

        public ResultType Result { get; private set; }

        internal void Complete(ResultType result)
        {
            Result = result;
            Complete();
            OnComplete?.Invoke(result);
        }

        internal void Update(ResultType result)
        {
            Result = result;
            OnProgress?.Invoke(result);
        }
    }


}