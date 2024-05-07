using System.Collections.Generic;
using System.Linq;

namespace TAssetBundle
{
    internal interface IInnerOperation
    {
        float Progress { get; }
    }

    internal class InnerOperations : IInnerOperation
    {
        private List<IInnerOperation> _innerOperations;
        private int _count;

        public float Progress
        {
            get
            {
                if (_innerOperations != null)
                {
                    return _innerOperations.Sum(p => p.Progress) / _count;
                }
                else
                {
                    return 0f;
                }
            }
        }

        public void PushCount()
        {
            ++_count;
        }

        public void PopCount()
        {
            --_count;
        }

        public void Add(IInnerOperation innerOperation)
        {
            if(_innerOperations == null)
            {
                _innerOperations = new List<IInnerOperation>();
            }
            
            ++_count;
            _innerOperations.Add(innerOperation);
        }
    }
}