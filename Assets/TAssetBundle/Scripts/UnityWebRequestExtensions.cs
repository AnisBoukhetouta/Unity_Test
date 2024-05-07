using UnityEngine.Networking;

namespace TAssetBundle
{
    public struct UnityWebRequestError
    {
        public long responseCode;
        public string message;
        public bool isNetworkError;
        public bool isHttpError;
    }

    public static class UnityWebRequestExtensions
    {
        public static UnityWebRequestError GetError(this UnityWebRequest request)
        {
            return new UnityWebRequestError
            {
                responseCode = request.responseCode,
                message = request.error,
                isHttpError = request.IsHttpError(),
                isNetworkError = request.IsNetworkError()
            };
        }

        public static bool IsSuccess(this UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result == UnityWebRequest.Result.Success;
#else            
            return !request.isNetworkError && !request.isHttpError && (request.responseCode == 200 || request.responseCode == 0);
#endif
        }

        public static bool IsNetworkError(this UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError;
#else
            return request.isNetworkError;
#endif
        }

        public static bool IsHttpError(this UnityWebRequest request)
        {
#if UNITY_2020_1_OR_NEWER
            return request.result == UnityWebRequest.Result.ProtocolError;
#else
            return request.isHttpError;
#endif
        }
    }
}

