using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.PackageManager;
using System.Reflection;
using System;
using System.Linq;
using Refsa.CustomWorld;
using Unity.Entities;
using System.Collections.Generic;

namespace Refsa.CustomWorld.Editor
{
    public static class CustomWorldsEditorHelpers
    {
        const int ContextMenuSorting = 50;

        [MenuItem("Assets/Create/Custom World", false, ContextMenuSorting)]
        public static void ShowCustomWorldWindow()
        {
            CustomWorldWindow.Create();
        }

        /// <summary>
        /// Creates all the required Custom World scripts in the currently selected folder
        /// 
        /// Checks if each of them have already been created before creating them
        /// 
        /// Creates:
        ///     CustomWorldType enum
        ///     Class derived from CustomBootstrapBase
        ///     Attribute implementing ICustomWorldTypeAttribute
        /// </summary>
        /// <returns></returns>
        internal static Type SetupBaseBootstrap()
        {
            string path = GetProjectDirectoryPath();
            string currentPath = GetPackageDirectoryPath();

            if (!ClassAlreadyExists("CustomWorldType"))
                using (var enumTemplate = File.OpenText(currentPath + "/ScriptTemplates/CustomWorldTypeEnum.cs.txt"))
                {
                    string enumFileContents = enumTemplate.ReadToEnd();
                    using (var enumFile = File.CreateText(path + "/CustomWorldType.cs"))
                    {
                        enumFile.Write(enumFileContents);
                    }
                }

            if (!ClassAlreadyExists("CustomWorldTypeAttribute"))
                using (var attributeTemplate = File.OpenText(currentPath + "/ScriptTemplates/CustomWorldTypeAttribute.cs.txt"))
                {
                    string enumFileContents = attributeTemplate.ReadToEnd();
                    using (var enumFile = File.CreateText(path + "/CustomWorldTypeAttribute.cs"))
                    {
                        enumFile.Write(enumFileContents);
                    }
                }

            if (!ClassAlreadyExists("CustomBootstrap"))
                using (var bootstrapTemplate = File.OpenText(currentPath + "/ScriptTemplates/CustomBootstrap.cs.txt"))
                {
                    string enumFileContents = bootstrapTemplate.ReadToEnd();
                    using (var enumFile = File.CreateText(path + "/CustomWorldBootstrap.cs"))
                    {
                        enumFile.Write(enumFileContents);
                    }
                }

            AssetDatabase.Refresh();

            return Type.GetType("CustomWorldBootstrap");
        }

        [MenuItem("Assets/Test")]
        internal static void Test()
        {
            var baseType = Type.GetType("CustomWorldBootstrap");

            var genericTypeDefinition = baseType.GetGenericTypeDefinition();
            UnityEngine.Debug.Log($"{genericTypeDefinition}");
        }

        /// <summary>
        /// Looks through the enum and checks if a class has already been created for them
        /// </summary>
        /// <param name="current">CustomBoostrapBase class </param>
        /// <returns></returns>
        internal static IReadOnlyList<string> GetUnassignedWorldTypes(Type baseType)
        {
            Type attributeType = baseType.BaseType.GetGenericArguments()[1];
            Type enumType = baseType.BaseType.GetGenericArguments()[0];

            List<string> enumNames = System.Enum.GetNames(enumType).ToList();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (Attribute.IsDefined(t, attributeType))
                    {
                        var attributeValue = Attribute.GetCustomAttribute(t, attributeType);
                    }
                }
            }
            
            return enumNames;
        }

        /// <summary>
        /// Checks if class of name already exists in project assemblies
        /// </summary>
        /// <param name="name">Name of object to look for</param>
        /// <returns>true if it were found</returns>
        internal static bool ClassAlreadyExists(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (t.Name == name) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the base bootstrap for Custom Worlds is setup.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        internal static bool IsBaseSetup(out Type current)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in assembly.GetTypes())
                {
                    if (!t.IsAbstract && t.GetInterface("ICustomBootstrap") != null)
                    {
                        current = t;
                        return true;
                    }
                }
            }

            current = null;
            return false;
        }

        /// <summary>
        /// Gets the current directory in relation to the Asset folder
        /// 
        /// Uses current selection, same quirks applies as with CustomWorldsEditorHelpers::GetCurrentAssetDirectory
        /// </summary>
        /// <returns></returns>
        internal static string GetProjectDirectoryPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return path;
        }

        /// <summary>
        /// Gets the current directory in relation to the Asset folder
        /// 
        /// Makes use of the current selection in order to find it. This means that there
        /// needs to be an active selection for this to work. This is not the case after
        /// Unity does a script reload.
        /// </summary>
        /// <returns></returns>
        internal static string GetCurrentAssetDirectory()
        {
            foreach (var obj in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (System.IO.Directory.Exists(path))
                    return path;
                else if (System.IO.File.Exists(path))
                    return System.IO.Path.GetDirectoryName(path);
            }

            return "Assets";
        }

        /// <summary>
        /// Finds the location of the package contents.
        /// 
        /// If ran from the Assets folder it will find the path there
        /// If ran as a package it will look in /Library/PackageCache/ for the com.refsa.customworld folder
        /// </summary>
        /// <returns>Full system path of the package /Editor folder</returns>
        internal static string GetPackageDirectoryPath()
        {
            string currentPath = Application.dataPath.Replace("/Assets", "") + "/Library/PackageCache/";
            bool foundPackagePath = false;

            foreach(var dir in Directory.GetDirectories(currentPath))
            {
                if (dir.Contains("com.refsa.customworld"))
                {
                    currentPath = dir + "/Editor";
                    foundPackagePath = true;
                    break;
                }
            }

            if (!foundPackagePath)
            {
                UnityEngine.Debug.LogError($"Could not find Package path for com.refsa.customworld");
                currentPath = Application.dataPath + "/Scripts/ECS/CustomWorld/Editor";
            }

            return currentPath;
        }

        /// <summary>
        /// Looks through the project folders for the specified file name
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <returns>null if not found, full system path if found</returns>
        internal static string FindFileInProject(string fileName)
        {
            var pathSearch = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);

            if (pathSearch.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"Could not find {fileName}");
                return null;
            }

            return pathSearch[0];
        }
    }
}