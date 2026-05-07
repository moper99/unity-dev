using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FairyGUIEditor
{ 
    public class UIGenerateWindow : EditorWindow
    {
        private static readonly string ProjectCodePath =
            Path.Combine("Assets", "GameScripts", "HotFix", "GameLogic", "UI");

        private FileDetailInfo[] _fileDetailInfos;
        private int _index;
        private int _newIndex;
        private string[] _modeLs;
        private string _modName;

        private string _path = "";
        private Vector2 _scrollPos = new Vector2(100, 100);
        private static string _riderFolder = "";

        private bool _forceRefresh = true;
        private bool _hasCompiled = false;
        
#if UNITY_EDITOR_OSX
        /// <summary>
        /// 工程绝对路径，同时把"Assets"去掉
        /// </summary>
        private static string _projectPath = string.Empty;

        private static string ProjectPath
        {
            get
            {
                if (string.IsNullOrEmpty(_projectPath))
                {
                    _projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
                }

                return _projectPath;
            }
        }
#endif

        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                _hasCompiled = true;
                // return;
            }

            if (_hasCompiled && !EditorApplication.isCompiling)
            {
                _hasCompiled = false;
                _forceRefresh = true;
            }
            
            if (_path == "" || _modeLs.Length <= 0) return;

            if (Application.isPlaying) Close();

            var normalStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 50, normal = { textColor = Color.green } };
            var highlightStyle = new GUIStyle(GUI.skin.button)
                { fixedWidth = 50, normal = { textColor = Color.yellow } };
            var modifiedStyle = new GUIStyle(GUI.skin.button) { fixedWidth = 50, normal = { textColor = Color.red } };
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label( "请选择要生成代码的模块", EditorStyles.boldLabel, GUILayout.Width(200f));
            _newIndex = EditorGUILayout.Popup(_index, _modeLs, GUILayout.Width(200f));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (_index != _newIndex || _forceRefresh)
            {
                // EditorPrefs.SetInt("UIGenerateWindow_index", _index);
                _modName = _modeLs[_newIndex];
                var p = Path.Combine(_path, _modName);
                _fileDetailInfos = GetCodeFiles(p);
                _index = _newIndex;
                _forceRefresh = false;
            }

            if (_fileDetailInfos == null)
            {
                _forceRefresh = true;
                return;
            }
            
            // GUILayout.BeginVertical("GroupBox");
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(450), GUILayout.Height(400));
            foreach (var fInfo in _fileDetailInfos)
            {
                GUILayout.BeginHorizontal();
                if (!fInfo.ExistInProject)
                {
                    if (GUILayout.Button("新建", highlightStyle))
                    {   
                        _forceRefresh = true;
                        CopyToDst(fInfo);
                    }
                }
                else
                {
                    if (GUILayout.Button("更新", fInfo.IsDifferent ? modifiedStyle : normalStyle))
                    {
                        CompareTwoFiles(fInfo.LongName, fInfo.DestFileName);
                    }

                    GUI.enabled = false;
                }

                fInfo.Selected = GUILayout.Toggle(fInfo.Selected, fInfo.ShortName);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            // GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("一键拷贝/diff所有",GUILayout.Width(200)))
            {
                foreach (var fInfo in _fileDetailInfos)
                {
                    if (!fInfo.ExistInProject)
                    {
                        CopyToDst(fInfo);
                    }
                    else
                    {
                        var srcFileName = fInfo.LongName;
                        var dstFileName = CalcDstPathName(fInfo.ShortName);
                        CompareTwoFiles(srcFileName, dstFileName);
                    }
                }
                Close();
            }

            if (GUILayout.Button("拷贝选中",GUILayout.Width(100)))
            {
                foreach (var fInfo in _fileDetailInfos)
                {
                    if (!fInfo.ExistInProject && fInfo.Selected)
                    {
                        CopyToDst(fInfo);
                    }
                }

                _forceRefresh = true;
            }


            if (GUILayout.Button("关闭",GUILayout.Width(100))) Close();

            GUILayout.EndHorizontal();
        }

        [MenuItem("FairyGUI/GenCode", true)]
        public static bool ShowGenCodeButton()
        {
            return !Application.isPlaying;
        }

        [MenuItem("FairyGUI/GenCode")]
        public static void ShowWindow()
        {
            const string projectPathText = "Library/FGUIProject.txt";
            if (!File.Exists(projectPathText))
            {
                var projectFile = EditorUtility.OpenFilePanel("选择FairyGUI工程文件(FGUI.fairy)",
                    "", "fairy");
                if (!projectFile.EndsWith(".fairy"))
                {
                    EditorUtility.DisplayDialog("错误", "未选择FairyGUI工程", "ok");
                    return;
                }

#if UNITY_EDITOR_WIN
                var riderPath = EditorUtility.OpenFilePanel("选择 rider64.exe 文件",
                    "", "exe");
                if (!riderPath.EndsWith(".exe"))
                {
                    EditorUtility.DisplayDialog("错误", "未选择 rider64.exe 文件", "ok");
                    return;
                }
#elif UNITY_EDITOR_OSX
                var riderPath = "/Applications/Rider.app";
                if (!Directory.Exists(riderPath))
                {
                    riderPath = EditorUtility.OpenFolderPanel("选择 Rider.app 文件", "", "Rider.app");
                    if (!riderPath.EndsWith(".app"))
                    {
                        EditorUtility.DisplayDialog("错误", "未选择 Rider.app 文件", "ok");
                        return;
                    }
                }
#endif

                var projectPath = Path.GetDirectoryName(projectFile);
                var codePath = projectPath + Path.DirectorySeparatorChar + "tempCodes" + "\n" + riderPath;
                File.WriteAllText(projectPathText, codePath);
            }

            var fileContent = File.ReadAllText(projectPathText);
            var path = fileContent.Split("\n")[0];
            _riderFolder = fileContent.Split("\n")[1];
            Debug.Log($"code path: {path}");
            Debug.Log($"rider path: {_riderFolder}");
            var win = GetWindow<UIGenerateWindow>();
            win.Init(path);
            win.minSize = new Vector2(450, 500);
            win.maxSize = new Vector2(450, 500);

            // System.Diagnostics.Process.Start("rider.bat");
        }

        private static bool IsSrcDstDifferent(string srcFileName, string dstFileName)
        {
            var srcText = File.ReadAllText(srcFileName);
            var dstText = File.ReadAllText(dstFileName);
            var srcText2 = srcText.Replace("\r\n", "\n").Replace(" ", "").Trim('\n', ' ');
            var dstText2 = dstText.Replace("\r\n", "\n").Replace(" ", "").Trim('\n', ' ');
            var isDiff = !srcText2.Equals(dstText2);
            return isDiff;
            // if (dstFileName.EndsWith("Panel.cs"))
            // {
            //     var isDiff = IsAutoTextDifferent(srcText, dstText, "protected override void OnInit()",
            //         "#region user data init");
            //     return isDiff;
            // }
            // else if (dstFileName.EndsWith("Binder.cs"))
            // {
            //     var srcText2 = srcText.Replace("\r\n", "\n").Replace(" ", "");
            //     var dstText2 = dstText.Replace("\r\n", "\n").Replace(" ", "");
            //     var isDiff = !srcText2.Equals(dstText2);
            //     return isDiff;
            // }
            // else
            // {
            //     var isDiff = IsAutoTextDifferent(srcText, dstText, "public override void ConstructFromXML(XML xml)",
            //         "#region user data init");
            //     return isDiff;
            // }
        }

        private static bool IsAutoTextDifferent(string srcText, string dstText, string startSentry, string endSentry)
        {
            startSentry = startSentry.Replace(" ", "");
            endSentry = endSentry.Replace(" ", "");
            var srcText2 = srcText.Replace("\r\n", "\n").Replace(" ", "");
            var dstText2 = dstText.Replace("\r\n", "\n").Replace(" ", "");
            var srcStartIdx = srcText2.IndexOf(startSentry, StringComparison.Ordinal);
            var srcEndIdx = srcText2.IndexOf(endSentry, StringComparison.Ordinal);
            var dstStartIdx = dstText2.IndexOf(startSentry, StringComparison.Ordinal);
            var dstEndIdx = dstText2.IndexOf(endSentry, StringComparison.Ordinal);
            if (srcStartIdx == -1 || srcEndIdx == -1 || dstStartIdx == -1 || dstEndIdx == -1)
            {
                return true;
            }

            if (srcEndIdx - srcStartIdx != dstEndIdx - dstStartIdx)
            {
                return true;
            }

            var srcAutoText = srcText2.Substring(srcStartIdx, srcEndIdx - srcStartIdx);
            var dstAutoText = dstText2.Substring(dstStartIdx, dstEndIdx - dstStartIdx);
            return !srcAutoText.Equals(dstAutoText);
        }


        private void Init(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                EditorUtility.DisplayDialog("错误", $"路径{path}下 未生成代码", "ok");
                return;
            }

            _path = path;
            var ls = new List<string>();
            foreach (var subPath in Directory.GetFileSystemEntries(_path))
                if (File.GetAttributes(subPath).HasFlag(FileAttributes.Directory))
                {
                    var p = Path.GetFileNameWithoutExtension(subPath);
                    if (p == "Boot")
                    {
                        continue;
                    }
                    ls.Add(p);
                }

            if (ls.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", $"路径{path}下 未生成代码", "ok");
                return;
            }

            _modeLs = ls.ToArray();
            _index = _index != 0 ? _index : 0;
            _index = _index < _modeLs.Length ? _index : 0;
        }

        private void CopyToDst(FileDetailInfo fInfo)
        {
            var srcFileName = fInfo.LongName;
            var dstFileName = CalcDstPathName(fInfo.ShortName);
            var pp = Path.GetDirectoryName(dstFileName);
            Directory.CreateDirectory(pp ?? throw new Exception("pp is null"));
            File.Copy(srcFileName, dstFileName);
            fInfo.ExistInProject = true;

//             var designerFileName = Path.GetFileName(fInfo.ShortName) ?? "";
//             var className = designerFileName.Replace(".Designer.cs", "");
//             var logicFileName = dstFileName.Replace(designerFileName, className + ".cs");
//
//             if (!File.Exists(logicFileName))
//             {
//                 if (string.IsNullOrEmpty(className))
//                 {
//                     Debug.LogError($"生成逻辑类失败 dstFileName:{dstFileName}, shortName: {fInfo.ShortName}");
//                     return;
//                 }
//                 File.WriteAllText(logicFileName, $@"
//
//
// namespace GameRuntime
// {{
//     public partial class {className}
//     {{
//     }}
// }}
// ");
//             }
        }
        
        private static void CompareTwoFiles(string srcFileName, string dstFileName)
        {
            if (string.IsNullOrEmpty(_riderFolder) || !File.Exists(_riderFolder))
            {
                Debug.LogWarning("Rider路径未配置或无效，无法打开diff。\n当前路径: " + (_riderFolder ?? "空"));
                return;
            }

            try
            {
#if UNITY_EDITOR_OSX
                var command = "open";
                var args = $"-na \"/Applications/Rider.app\" --args diff \"{srcFileName}\" \"{Path.Combine(ProjectPath, dstFileName)}\"";
#else
                var command = _riderFolder;
                var args = $"diff \"{srcFileName}\" \"{dstFileName}\"";
#endif
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        FileName = command,
                        Arguments = args
                    }
                };
                process.OutputDataReceived += StdOutHandler;
                process.ErrorDataReceived  += StdErrHandler;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception e)
            {
                Debug.LogError($"启动Rider失败: {e.Message}\nRider路径: {_riderFolder}\n请重新运行 FairyGUI/GenCode 配置正确路径");
            }
        }

        static void StdOutHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data))
            {
                return;
            }
            //* Do your stuff with the output (write to console/log/StringBuilder)
            Debug.Log($"rider output: {outLine.Data}");
        }

        static void StdErrHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (string.IsNullOrEmpty(outLine.Data))
            {
                return;
            }
            //* Do your stuff with the output (write to console/log/StringBuilder)
            Debug.LogError($"rider error: {outLine.Data}");
        }

        private FileDetailInfo[] GetCodeFiles(string p)
        {
            var allFiles = Directory.GetFiles(p, "*.cs", SearchOption.AllDirectories);
            var fileDetailInfos = new List<FileDetailInfo>();
            foreach (var fileName in allFiles)
            {
                var shortName = GetRelativePath(p, fileName);
                Debug.Log($"shortName:{shortName}");
                var dstFileName = CalcDstPathName(shortName);
                var exist = File.Exists(dstFileName);
                bool diff = exist && IsSrcDstDifferent(fileName, dstFileName);
                fileDetailInfos.Add(new FileDetailInfo
                {
                    LongName = fileName,
                    ShortName = shortName,
                    Selected = false,
                    ExistInProject = exist,
                    IsDifferent = diff,
                    DestFileName = dstFileName
                });
            }

            return fileDetailInfos.ToArray();
        }

        private string CalcDstPathName(string shortName)
        {
            return Path.Combine(ProjectCodePath, _modName, "View", shortName);
        }

        public string GetRelativePath(string absPath, string relTo)
        {
            var sep = Path.DirectorySeparatorChar;
            var absDirs = absPath.Split(sep);
            var relDirs = relTo.Split(sep);

            // Get the shortest of the two paths
            var len = absDirs.Length < relDirs.Length ? absDirs.Length : relDirs.Length;

            // Use to determine where in the loop we exited
            var lastCommonRoot = -1;
            int index;

            // Find common root
            for (index = 0; index < len; index++)
                if (absDirs[index] == relDirs[index]) lastCommonRoot = index;
                else break;

            // If we didn't find a common prefix then throw
            if (lastCommonRoot == -1) throw new ArgumentException("Paths do not have a common base");

            // Build up the relative path
            var relativePath = new StringBuilder();

            // Add on the ..
            for (index = lastCommonRoot + 1; index < absDirs.Length; index++)
                if (absDirs[index].Length > 0)
                    relativePath.Append($"..{sep}");

            // Add on the folders
            for (index = lastCommonRoot + 1; index < relDirs.Length - 1; index++)
                relativePath.Append($"{relDirs[index]}{sep}");
            relativePath.Append(relDirs[relDirs.Length - 1]);

            return relativePath.ToString();
        }

        private class FileDetailInfo
        {
            public bool ExistInProject;
            public string LongName;
            public bool Selected;
            public string ShortName;
            public bool IsDifferent;
            public string DestFileName;
        }
    }
}