using UnityEngine;

namespace TAssetBundle.Samples
{
    public class SceneLoader : MonoBehaviour
    {
        public void LoadScene(string sceneName)
        {
            AssetManager.LoadSceneAsync(sceneName);
        }
    }
}

