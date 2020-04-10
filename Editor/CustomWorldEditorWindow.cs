using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Refsa.CustomWorld.Editor
{
    internal struct CustomWorldWindowData
    {
        public string wantedWorldType;
        public string addNewWorldTypeEnum;
        public string newWorldTemplate;
        public List<string> availableWorldTypes;
        public string projectPath;
        public string packagePath;
        public Type bootstrapType;
    }

    internal class CustomWorldWindow : ModalWindow<CustomWorldWindowData>
    {
        public static CustomWorldWindow Create()
        {
            var window = CustomWorldWindow.CreateInstance<CustomWorldWindow>();

            window.titleContent = new GUIContent("Custom World Editor");
            window.data = new CustomWorldWindowData();
            window.minSize = new Vector2(400, 150);
            window.maxSize = new Vector2(400, 150);

            window.showOKButton = false;
            window.cancelText = "Close";
            window.isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out window.data.bootstrapType);

            window.data.projectPath = CustomWorldsEditorHelpers.GetProjectDirectoryPath();
            window.data.packagePath = CustomWorldsEditorHelpers.GetPackageDirectoryPath();

            window.SetupNewWorldTemplatePath();

            if (window.isBaseSetup)
                window.FindAvailableWorldTypes(window.data.bootstrapType);

            window.Show();
            return window;
        }

        protected override void Cancel()
        {
            requestClose = true;
        }

        protected override void OK()
        {
            requestClose = true;
        }

        int index = -1;
        bool isBaseSetup = false;
        protected override void Draw()
        {
            if (!isBaseSetup)
            {
                EditorGUILayout.BeginVertical();

                if (GUILayout.Button("Setup Bootstrap"))
                {
                    data.bootstrapType = CustomWorldsEditorHelpers.SetupBaseBootstrap();
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical();

                if (data.availableWorldTypes != null && data.availableWorldTypes.Count != 0)
                {
                    index = EditorGUILayout.Popup(index, data.availableWorldTypes.ToArray());

                    if (index != -1)
                    {
                        data.wantedWorldType = data.availableWorldTypes[index];
                    }

                    if (GUILayout.Button("Add World"))
                    {
                        AddNewWorld();
                    }
                }
                else
                {
                    data.addNewWorldTypeEnum = EditorGUILayout.TextField("New World Type Enum Entry", data.addNewWorldTypeEnum);
                    if (GUILayout.Button("Add World Type Enum"))
                    {
                        AddNewEnumEntry();
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            if (EditorWindow.HasOpenInstances<CustomWorldWindow>())
            {
                var currentWindow = (EditorWindow.GetWindow(typeof(CustomWorldWindow)) as CustomWorldWindow);
                if (currentWindow != null)
                {
                    currentWindow.ScriptsReloaded();
                }
            }
        }

        void ScriptsReloaded()
        {
            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);
            FindAvailableWorldTypes(data.bootstrapType);
        }

        void AddNewEnumEntry()
        {
            if (data.addNewWorldTypeEnum == null || data.addNewWorldTypeEnum == "") 
            {
                return;
            }

            foreach (string name in System.Enum.GetNames(data.bootstrapType.BaseType.GetGenericArguments()[0]))
            {   
                if (data.addNewWorldTypeEnum == name)
                {
                    data.addNewWorldTypeEnum = "";
                    return;
                }
            }

            var pathSearch = Directory.GetFiles(Application.dataPath, "CustomWorldType.cs", SearchOption.AllDirectories);
            if (pathSearch.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"Could not find CustomWorldType.cs");
                return;
            }

            string worldTypeEnumPath = pathSearch[0];
            List<string> enumFileContents = new List<string>();

            using (var enumFile = File.OpenText(worldTypeEnumPath))
            {
                while (!enumFile.EndOfStream)
                {
                    enumFileContents.Add(enumFile.ReadLine());
                }
            }

            enumFileContents[enumFileContents.Count - 2] += ",";
            enumFileContents.Insert(enumFileContents.Count - 1, "\t" + data.addNewWorldTypeEnum);

            using (var enumFile = File.CreateText(worldTypeEnumPath))
            {
                enumFile.Write(enumFileContents.Aggregate((r, e) => r += e + "\n"));
            }

            data.addNewWorldTypeEnum = "";

            AssetDatabase.Refresh();
        }

        void AddNewWorld()
        {
            if (data.wantedWorldType == null || data.wantedWorldType == "")
            {
                return;
            }

            data.projectPath = CustomWorldsEditorHelpers.GetProjectDirectoryPath();
            UnityEngine.Debug.Log($"{data.projectPath}");
            string className = $"{data.wantedWorldType}World";
            string savePath = data.projectPath + $"/{className}.cs";

            if (data.newWorldTemplate == null)
            {
                SetupNewWorldTemplatePath();
            }

            data.newWorldTemplate = data.newWorldTemplate.Replace("#WORLDTYPE#", data.wantedWorldType);

            using (var newWorldFile = File.CreateText(savePath))
            {
                newWorldFile.Write(data.newWorldTemplate);
            }

            AssetDatabase.Refresh();
        }

        void SetupNewWorldTemplatePath()
        {
            string templatePath = CustomWorldsEditorHelpers.GetPackageDirectoryPath() + "/ScriptTemplates/CustomWorld.cs.txt";
            string newWorldContents = "";

            using (var newWorldTemplate = File.OpenText(templatePath))
            {
                newWorldContents = newWorldTemplate.ReadToEnd();
            }
            if (newWorldContents == "")
            {
                UnityEngine.Debug.LogError($"Template file for new Custom Worlds not found");
                Cancel();
            }

            data.newWorldTemplate = newWorldContents;
        }

        void FindAvailableWorldTypes(Type bootstrapType)
        {
            data.availableWorldTypes = new List<string>();

            foreach (string name in System.Enum.GetNames(bootstrapType.BaseType.GetGenericArguments()[0]))
            {   
                if (!CustomWorldsEditorHelpers.ClassAlreadyExists(name + "World") && name != "Default")
                {
                    data.availableWorldTypes.Add(name);
                }
            }
        }
    }
}