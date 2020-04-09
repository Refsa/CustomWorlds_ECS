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
        [MenuItem("Assets/Create/Custom World/Setup", false, ContextMenuSorting)]
        public static void SetupCustomWorldBootstrap()
        {
            if (IsBaseSetup(out Type current))
            {
                UnityEngine.Debug.LogError($"Bootstrap for Custom World already exists: {current.Name}");
                return;
            }

            string path = GetProjectDirectoryPath();
            string currentPath = GetDirectoryPath();

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
        }

        [MenuItem("Assets/Create/Custom World/New Custom World", false, ContextMenuSorting + 11)]
        public static void CreateNewCustomWorld()
        {
            if (!IsBaseSetup(out Type current))
            {
                UnityEngine.Debug.LogError($"Base Bootstrap for Custom World is not setup");
                return;
            }

            NewWorldWindow.Create(current);
        }

        internal static IReadOnlyList<string> GetUnassignedWorldTypes(Type current)
        {
            Type attributeType = current.BaseType.GetGenericArguments()[1];
            Type enumType = current.BaseType.GetGenericArguments()[0];

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

        internal static string GetProjectDirectoryPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return path;
        }

        internal static string GetDirectoryPath()
        {
            string currentPath = Directory.GetCurrentDirectory().Replace("/Assets", "") + "/Library/PackageCache/";
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
                currentPath = Directory.GetCurrentDirectory() + "/Assets/Scripts/ECS/CustomWorld/Editor";
            }

            return currentPath;
        }
    }
}