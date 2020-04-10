using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Refsa.CustomWorld.Editor
{   
    internal struct WorldTypeData
    {
        public string name;
        public bool classExists;
        public string className;
    }

    internal class CustomWorldWindowData
    {
        public string wantedWorldType;
        public string addNewWorldTypeEnum;
        public string newWorldTemplate;
        public List<string> availableWorldTypes;
        public string projectPath;
        public string packagePath;
        public Type bootstrapType;

        public List<WorldTypeData> worldTypeData;
    }

    internal class CustomWorldWindow : ModalWindow<CustomWorldWindowData>
    {
        int index = -1;
        bool isBaseSetup = false;

#region SETUP
        public static CustomWorldWindow Create()
        {

            var window = CustomWorldWindow.CreateInstance<CustomWorldWindow>();
            window.minSize = new Vector2(400, 150);
            window.maxSize = new Vector2(400, 150);
            window.titleContent = new GUIContent("Custom World Editor");

            window.showOKButton = false;
            window.cancelText = "Close";

            window.SetupData();

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

        void SetupData()
        {
            data = new CustomWorldWindowData();
            
            OnSelectionChanged();
            SetupNewWorldTemplatePath();

            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);
            if (isBaseSetup)
            {
                SetupWorldTypeData();
                FindAvailableWorldTypes(data.bootstrapType);
            }
        }

        void SetupWorldTypeData()
        {
            data.worldTypeData = new List<WorldTypeData>();

            foreach (string name in System.Enum.GetNames(data.bootstrapType.BaseType.GetGenericArguments()[0]))
            {   
                bool exists = CustomWorldsEditorHelpers.ClassAlreadyExists(name + "World");

                data.worldTypeData.Add(
                    new WorldTypeData {
                        name = name,
                        classExists = exists,
                        className = name + "Class"
                    }
                );
            }
        }
#endregion

#region DRAW
        protected override void Draw()
        {
            if (!isBaseSetup)
            {
                DrawSetupBootstrap();
            }
            else
            {
                DrawAddWorlds();
            }
        }

        void DrawSetupBootstrap()
        {
            EditorGUILayout.BeginVertical();

            if (GUILayout.Button("Setup Bootstrap"))
            {
                data.bootstrapType = CustomWorldsEditorHelpers.SetupBaseBootstrap();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawAddWorlds()
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
#endregion

#region SCRIPT_RELOAD
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
            data = new CustomWorldWindowData();
            data.packagePath = EditorPrefs.GetString("packagePath");
            data.projectPath = EditorPrefs.GetString("projectPath");

            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);

            SetupNewWorldTemplatePath();

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            if (isBaseSetup)
            {
                FindAvailableWorldTypes(data.bootstrapType);
                SetupWorldTypeData();
            }
        }
#endregion

        void OnSelectionChanged()
        {
            data.projectPath = CustomWorldsEditorHelpers.GetProjectDirectoryPath();
            data.packagePath = CustomWorldsEditorHelpers.GetPackageDirectoryPath();

            if (data.projectPath != "")
                EditorPrefs.SetString("projectPath", data.projectPath);
            if (data.packagePath != "")
                EditorPrefs.SetString("packagePath", data.packagePath);
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

            string worldTypeEnumPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (worldTypeEnumPath == null) return;

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

            /* string projectPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (projectPath == null) return;
            data.projectPath = Path.GetDirectoryName(projectPath); */
            string projectPath = data.projectPath;

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