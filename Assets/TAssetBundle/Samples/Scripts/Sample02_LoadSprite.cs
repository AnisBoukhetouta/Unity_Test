using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TAssetBundle.Samples
{
    public class Sample02_LoadSprite : MonoBehaviour
    {
        public Image image;

        [AssetType(typeof(Sprite))]
        public AssetRef sprite;


        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);

            if (!sprite.IsValid)
                yield break;

            var loadAssetAsync = AssetManager.LoadAssetAsync<Sprite>(sprite);

            yield return loadAssetAsync;

            var assetHandle = loadAssetAsync.Result;

            image.sprite = assetHandle.Get();
            image.SetNativeSize();

            yield return new WaitForSeconds(3f);

            AssetManager.UnloadAsset(assetHandle);
        }
    }
}

