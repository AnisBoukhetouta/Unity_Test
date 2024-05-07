using System.Collections;
using UnityEngine;

namespace TAssetBundle
{
    internal class CoroutineHandler : MonoBehaviour
    {
        private static CoroutineHandler s_instance;


        public static CoroutineHandler Instance
        {
            get
            {
                if (s_instance != null)
                {
                    return s_instance;                    
                }
                else
                {
                    s_instance = CreateInstance();
                }

                return s_instance;
            }
        }

        private static CoroutineHandler CreateInstance()
        {
            var go = new GameObject("TAssetBundle.CoroutineHandler");
            DontDestroyOnLoad(go);
            return go.AddComponent<CoroutineHandler>();
        }


        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        private void Awake()
        {
            if (s_instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }

        public void EndOfFrame(IEnumerator coroutine)
        {
            StartCoroutine(AfterEndOfFrameCoroutine(coroutine));
        }

        private IEnumerator AfterEndOfFrameCoroutine(IEnumerator coroutine)
        {
            yield return _waitForEndOfFrame;

            yield return coroutine;
        }
    }

}
