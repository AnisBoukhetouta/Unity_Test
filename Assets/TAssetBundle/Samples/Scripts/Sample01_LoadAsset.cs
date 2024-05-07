using UnityEngine;

namespace TAssetBundle.Samples
{
    public class Sample01_LoadAsset : MonoBehaviour
    {
        [AssetType(typeof(GameObject))]
        public AssetRef[] spherePrefabs;

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            foreach (var assetRef in spherePrefabs)
            {
                if (GUILayout.Button("Load " + assetRef.FileName))
                {
                    var loadAssetAsync = AssetManager.LoadAssetAsync<GameObject>(assetRef);

                    loadAssetAsync.OnComplete += (assetHandle) =>
                    {
                        Instantiate(assetHandle.Get());
                    };
                }

                if (GUILayout.Button("Unload " + assetRef.FileName))
                {
                    AssetManager.UnloadAsset(assetRef);
                }
            }

            if (GUILayout.Button("Unload All"))
            {
                AssetManager.UnloadAll();
            }

            GUILayout.EndVertical();
        }
    }

}