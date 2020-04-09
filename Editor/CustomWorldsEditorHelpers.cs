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
        [MenuItem("Assets/Create/Custom World/Setup")]
        public static void SetupCustomWorldBootstrap()
        {
            if (IsBaseSetup(out Type current))
            {
                UnityEngine.Debug.LogError($"Bootstrap for Custom World already exists: {current.Name}");
            }

            string path = GetProjectDirectoryPath();
            string currentPath = GetDirectoryPath();

            using (var enumTemplate = File.OpenText(currentPath + "/ScriptTemplates/CustomWorldTypeEnum.cs.txt"))
            {
                string enumFileContents = enumTemplate.ReadToEnd();
                using (var enumFile = File.CreateText(path + "/CustomWorldType.cs"))
                {
                    enumFile.Write(enumFileContents);
                }
            }

            using (var attributeTemplate = File.OpenText(currentPath + "/ScriptTemplates/CustomWorldTypeAttribute.cs.txt"))
            {
                string enumFileContents = attributeTemplate.ReadToEnd();
                using (var enumFile = File.CreateText(path + "/CustomWorldTypeAttribute.cs"))
                {
                    enumFile.Write(enumFileContents);
                }
            }

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

        [MenuItem("Assets/Create/Custom World/New Custom World")]
        public static void CreateNewCustomWorld()
        {
            if (!IsBaseSetup(out Type current))
            {
                UnityEngine.Debug.LogError($"Base Bootstrap for Custom World is not setup");
                return;
            }

            string path = GetProjectDirectoryPath();
            string templatePath = GetDirectoryPath() + "/ScriptTemplates/CustomWorld.cs.txt";
            string newWorldContents = "";

            using (var newWorldTemplate = File.OpenText(templatePath))
            {
                newWorldContents = newWorldTemplate.ReadToEnd();
            }
            if (newWorldContents == "")
            {
                UnityEngine.Debug.LogWarning($"Template file for new Custom Worlds not found");
            }

            // setup WorldType selector
            GenericMenu worldTypeSelector = new GenericMenu();
            string wantedWorldType = "";
            foreach (string name in System.Enum.GetNames(current.BaseType.GetGenericArguments()[0]))
            {
                worldTypeSelector.AddItem(new GUIContent(name), false,
                    (o) => {
                        wantedWorldType = (string)o;
                        if (wantedWorldType != "")
                        {
                            string className = $"{wantedWorldType}World";
                            if (ClassAlreadyExists(className))
                            {
                                UnityEngine.Debug.LogError($"Class with name [{className}] already exists");
                                return;
                            }

                            string savePath = path + $"/{className}.cs";
                            newWorldContents = newWorldContents.Replace("#WORLDTYPE#", wantedWorldType);

                            using (var newWorldFile = File.CreateText(savePath))
                            {
                                newWorldFile.Write(newWorldContents);
                            }

                            AssetDatabase.Refresh();
                        }
                    }, name);
            }

            // Display WorldType selector
            Rect rect = new Rect(Vector2.zero, new Vector2(100, 200));
            worldTypeSelector.DropDown(rect);
        }

        static string[] GetUnassignedWorldTypes(Type current)
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
            
            return enumNames.ToArray();
        }

        static bool ClassAlreadyExists(string name)
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

        static bool IsBaseSetup(out Type current)
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

        static string GetProjectDirectoryPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (Path.HasExtension(path))
            {
                path = Path.GetDirectoryName(path);
            }
            return path;
        }

        static string GetDirectoryPath()
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