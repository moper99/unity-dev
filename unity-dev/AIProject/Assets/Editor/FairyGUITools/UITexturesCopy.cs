using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace FairyGUIEditor
{
    public class FGUITexturesCopy
    {
        private static readonly List<string> FilePathList = new();
        private static readonly List<string> FileNeedDelList = new();
        private static readonly List<string> DirNeedDelList = new();
        private static readonly List<string> FileNameList = new();
        
        private const string sourceDir = "Assets/External~/ui-project/assets/Textures/";
        private const string destDir = "Assets/AssetRaw/UIRaw/Raw/";
        
        [MenuItem("FairyGUI/拷贝Textures到Unity")]
        private static void CopyTextures()
        {
            FilePathList.Clear();
            FileNameList.Clear();
            FileNeedDelList.Clear();
            DirNeedDelList.Clear();
            CopyFolder(sourceDir, destDir);
            DeleteFilesOrDirs(FileNeedDelList);
            DeleteFilesOrDirs(DirNeedDelList);
            AssetDatabase.Refresh();
            foreach (string destFile in FilePathList)
            {
                TextureImporter importer = AssetImporter.GetAtPath(destFile) as TextureImporter;
                if (importer != null)
                {
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        AssetDatabase.ImportAsset(destFile);
                    }
                }
            }

            FilePathList.Clear();
            FileNameList.Clear();
            FileNeedDelList.Clear();
            DirNeedDelList.Clear();
            
            //拷贝Textures的package.xml文件
            var sourcePackageXml = $"{sourceDir}package.xml";
            var destPackageXml = $"{destDir}TexturePackage.xml";
            File.Copy(sourcePackageXml, destPackageXml, true);
            AssetDatabase.ImportAsset(destPackageXml);
            
            EditorUtility.DisplayDialog("Textures Copy", "Textures Copy Completed!", "OK");
        }
        

        private static void CopyFolder(string sourceFolderPath, string destinationFolderPath)
        {
            if (!Directory.Exists(destinationFolderPath))
            {
                Directory.CreateDirectory(destinationFolderPath);
            }

            string[] files = Directory.GetFiles(sourceFolderPath);
            var destFiles = Directory.GetFiles(destinationFolderPath).ToList();
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string extension = Path.GetExtension(file);
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    string destFile = Path.Combine(destinationFolderPath, fileName);
                    RecordPath(destFile, fileName);
                    File.Copy(file, destFile, true);
                    destFiles.Remove(destFile);
                }
            }

            FileNeedDelList.AddRange(destFiles);

            string[] folders = Directory.GetDirectories(sourceFolderPath);
            var destFolders = Directory.GetDirectories(destinationFolderPath).ToList();
            foreach (string folder in folders)
            {
                string folderName = Path.GetFileName(folder);
                string destFolder = Path.Combine(destinationFolderPath, folderName);

                if (!Directory.Exists(destFolder))
                {
                    Directory.CreateDirectory(destFolder);
                }

                CopyFolder(folder, destFolder);
                destFolders.Remove(destFolder);
            }

            DirNeedDelList.AddRange(destFolders);
        }
        

        private static void RecordPath(string path, string name)
        {
            if (FileNameList.Contains(name))
            {
                Debug.LogError("存在同名Texture，这是不允许的：" + name);
            }

            FileNameList.Add(name);
            FilePathList.Add(path);
        }

        private static void DeleteFilesOrDirs(List<string> paths)
        {
            foreach (var path in paths)
            {
                if (path.EndsWith(".meta")) continue;
                FileUtil.DeleteFileOrDirectory(path);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
            }
        }
    }
}
