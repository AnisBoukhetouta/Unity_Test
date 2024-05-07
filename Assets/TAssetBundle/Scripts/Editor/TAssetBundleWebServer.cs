using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace TAssetBundle.Editor
{

    [InitializeOnLoad]
    internal static class TAssetBundleWebServer
    {
        private static readonly object _lockObject = new object();
        private static int _port;
        private static bool _active;
        private static string _rootFolder;
        private static bool _running;
        private static string _rootPath;

        static TAssetBundleWebServer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssetBundleBuilder.OnBuildCompleted += OnBuildCompleted;
            SetActive(EditorPrefs.GetBool("TAssetBundleWebServer.Active", false));
            SetPort(EditorPrefs.GetInt("TAssetBundleWebServer.Port", 20080));
            SetRootFolder(EditorPrefs.GetString("TAssetBundleWebServer.RootFolder", "/ServerData"));
        }

        public static bool IsActive()
        {
            return _active;
        }

        public static void SetActive(bool active)
        {
            if (_active != active)
            {
                _active = active;
                EditorPrefs.SetBool("TAssetBundleWebServer.Active", active);
            }
        }

        public static int GetPort()
        {
            return _port;
        }

        public static void SetPort(int port)
        {
            if (_port != port)
            {
                _port = port;
                EditorPrefs.SetInt("TAssetBundleWebServer.Port", port);
            }
        }

        public static string GetRootFolder()
        {
            return _rootFolder;
        }

        public static void SetRootFolder(string rootFolder)
        {
            if (_rootFolder != rootFolder)
            {
                _rootFolder = rootFolder;
                _rootPath = EditorUtil.GetProjectPath() + _rootFolder;
                EditorPrefs.SetString("TAssetBundleWebServer.RootFolder", rootFolder);
            }
        }

        public static string GetRootPath()
        {
            return _rootPath;
        }

        private static void Start()
        {
            _running = true;

            var rootpath = GetRootPath();
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(GetPrefix());
            httpListener.Start();

            Logger.Log("start web server - port:" + _port);
            Task.Run(() => Listen(httpListener, rootpath));
        }

        private static void Stop()
        {
            lock (_lockObject)
            {
                if (!_running)
                    return;

                _running = false;
                Logger.Log("stop web server - port:" + _port);
            }
        }

        public static string GetPrefix()
        {
            return $"http://localhost:{_port}/";
        }

        public static bool IsRunning()
        {
            lock (_lockObject)
            {
                return _running;
            }
        }

        private static void Listen(HttpListener httpListener, string rootPath)
        {
            while (httpListener.IsListening && IsRunning())
            {
                var context = httpListener.GetContext();

                HttpListenerRequest req = context.Request;

                var assetPath = rootPath + req.RawUrl;

                Logger.Log("web server request - " + req.RawUrl);

                HttpListenerResponse resp = context.Response;

                byte[] buffer;

                if (File.Exists(assetPath))
                {
                    resp.StatusCode = (int)HttpStatusCode.OK;
                    buffer = File.ReadAllBytes(assetPath);
                }
                else
                {
                    Logger.Warning("web server file not found - " + assetPath);
                    resp.StatusCode = (int)HttpStatusCode.NotFound;
                    buffer = Encoding.UTF8.GetBytes("404 - NOT FOUND");
                }

                resp.ContentLength64 = buffer.Length;
                var ros = resp.OutputStream;
                ros.Write(buffer, 0, buffer.Length);
                ros.Close();
            }

            httpListener.Stop();
            httpListener.Close();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (AssetBundleBuilder.Settings.editorPlayMode == EEditorPlayMode.EditorAsset)
                return;

            if (!_active)
                return;

            if (playModeState == PlayModeStateChange.EnteredPlayMode)
            {
                Start();
            }
            else if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                Stop();
            }
        }

        private static void OnBuildCompleted(BuildTarget buildTarget)
        {
            if (!_active)
                return;

            if (!Directory.Exists(AssetBundleBuilder.GetOutputPath(buildTarget)))
                return;

            var serverDataPath = Path.Combine(GetRootPath(), buildTarget.ToString());
            EditorUtil.DeleteDirectory(serverDataPath);
            EditorUtil.CreateDirectory(GetRootPath());
            FileUtil.CopyFileOrDirectory(AssetBundleBuilder.GetOutputPath(buildTarget), serverDataPath);
            Logger.Log("copy assets to server data folder - " + serverDataPath);
        }
    }
}

