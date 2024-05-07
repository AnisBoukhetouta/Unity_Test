using System;

namespace TAssetBundle.Samples
{
    public static class FileSizeFormmater
    {
        static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        const string FormatTemplate = "{0:0.#}{1}";

        public static string FormatSize(long size)
        {
            if (size == 0)
            {
                return string.Format(FormatTemplate, 0, SizeSuffixes[0]);
            }

            var absSize = Math.Abs(size);
            var fpPower = Math.Log(absSize, 1024);
            var intPower = (int)fpPower;
            var iUnit = intPower >= SizeSuffixes.Length ? SizeSuffixes.Length - 1 : intPower;
            var normSize = Math.Round(absSize / Math.Pow(1024, iUnit));

            return string.Format(FormatTemplate, Math.Sign(size) * normSize, SizeSuffixes[iUnit]);
        }
    }

}