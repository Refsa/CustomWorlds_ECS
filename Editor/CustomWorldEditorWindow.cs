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
            window.minSize = new Vector2(600, 400);
            // window.maxSize = new Vector2(400, 400);
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
                if (name == "Default") continue;

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
            GUILayout.BeginVertical();
            {
                if (GUILayout.Button("Setup Bootstrap"))
                {
                    data.bootstrapType = CustomWorldsEditorHelpers.SetupBaseBootstrap();
                }
            }
            GUILayout.EndVertical();
        }

        void DrawAddWorlds()
        {
            GUILayout.BeginVertical(GUILayout.Width(position.width));
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("Enum Name", GUILayout.Width(position.width / 3f));
                        EditorGUILayout.LabelField("Class Name", GUILayout.Width(position.width / 3f));
                        EditorGUILayout.LabelField("Action", GUILayout.Width(position.width / 3f));
                    }
                    GUILayout.EndHorizontal();

                    foreach (var worldTypeData in data.worldTypeData)
                    {
                        GUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(worldTypeData.name, GUILayout.Width(position.width / 3f));

                        if (!worldTypeData.classExists)
                        {
                            EditorGUILayout.LabelField("NULL", GUILayout.Width(position.width / 3f));
                            if (GUILayout.Button("Create Class", GUILayout.Width(position.width / 3f)))
                            {
                                data.wantedWorldType = worldTypeData.name;
                                AddNewWorld();
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(worldTypeData.className, GUILayout.Width(position.width / 3f));
                            if (GUILayout.Button("Remove", GUILayout.Width(position.width / 3f)))
                            {
                                data.wantedWorldType = worldTypeData.name;
                                RemoveWorld();
                            }
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();

                EditorGUILayout.Space(20);

                GUILayout.BeginVertical();
                {
                    data.addNewWorldTypeEnum = EditorGUILayout.TextField("New World Type", data.addNewWorldTypeEnum);
                    if (GUILayout.Button("Add World Type"))
                    {
                        AddEnumEntry();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
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

        void AddEnumEntry()
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

            if (!enumFileContents[enumFileContents.Count - 2].Contains(","))
            {
                enumFileContents[enumFileContents.Count - 2] += ",";
            }
            enumFileContents.Insert(enumFileContents.Count - 1, "\t" + data.addNewWorldTypeEnum);

            using (var enumFile = File.CreateText(worldTypeEnumPath))
            {
                enumFile.Write(enumFileContents.Aggregate("", (r, e) => r + e + "\n"));
            }

            data.addNewWorldTypeEnum = "";

            AssetDatabase.Refresh();
        }

        void RemoveEnumEntry(string enumEntryName)
        {
            string worldTypeEnumPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (worldTypeEnumPath == null) return;

            string enumFileContents = "";
            using (var enumFile = File.OpenText(worldTypeEnumPath))
            {
                enumFileContents = 
                    enumFile.ReadToEnd()
                    .Replace("\t" + enumEntryName + "\n", "")
                    .Replace("\t" + enumEntryName + ",\n", "");
            }

            using (var enumFile = File.CreateText(worldTypeEnumPath))
            {
                enumFile.Write(enumFileContents);
            }

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

            data.wantedWorldType = "";
            AssetDatabase.Refresh();
        }

        void RemoveWorld()
        {
            string projectPath = data.projectPath;
            string className = $"{data.wantedWorldType}World";
            string savePath = CustomWorldsEditorHelpers.FindFileInProject(className + ".cs");

            if (savePath == null) return;

            RemoveEnumEntry(data.wantedWorldType);
            File.Delete(savePath);

            data.wantedWorldType = "";
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