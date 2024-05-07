
namespace TAssetBundle.Editor
{
    internal class PreprocessBuild : UnityEditor.Build.IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            AssetBundleBuilder.PreprocessBuild(report.summary.platform);
        }
    }

    internal class PostprocessBuild : UnityEditor.Build.IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            AssetBundleBuilder.PostprocessBuild(report.summary.platform);
        }
    }
}
