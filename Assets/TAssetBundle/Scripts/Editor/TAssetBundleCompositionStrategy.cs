using System;
using UnityEngine;

namespace TAssetBundle.Editor
{

    public abstract class TAssetBundleCompositionStrategy : ScriptableObject
    {
        public class Data
        {
        }

        public abstract void Run(TAssetBundleManifest manifest, Data data);

        public virtual Type GetDataType()
        {
            return typeof(Data);
        }

        public Data CreateData()
        {
            return Activator.CreateInstance(GetDataType()) as Data;
        }

        public bool IsUseData()
        {
            return GetDataType() != typeof(Data);
        }
    }

    public abstract class TAssetBundleCompositionStrategy<T> : TAssetBundleCompositionStrategy
        where T : TAssetBundleCompositionStrategy.Data
    {
        public override Type GetDataType()
        {
            return typeof(T);
        }
    }


    public enum EAssetBundleBuildName
    {
        FirstObject,
        Number,
    }

    public class CompositionStrategyBuildData : TAssetBundleCompositionStrategy.Data
    {
        public EAssetBundleBuildName assetBundleBuildName;
    }


}
