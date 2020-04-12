using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Refsa.CustomWorld.Editor
{   
    internal class WorldTypeData
    {
        public string name;
        public bool classExists;
        public string className;
    }

    internal class SystemData
    {
        public string name;
        public string world;
        public Type type;
        public int selectedWorldIndex;
    }

    internal class CustomWorldWindowData
    {
        public string wantedWorldType;
        public string addNewWorldTypeEnum;
        public string newWorldTemplate;
        public string projectPath;
        public string packagePath;
        public Type bootstrapType;

        public List<WorldTypeData> worldTypeData;
        public List<SystemData> systemDatas;
        public List<string> worldTypeEnums;

        public Queue<SystemData> systemsToChange;
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

        /// <summary>
        /// Sets up required data from a fresh window
        /// </summary>
        void SetupData()
        {
            data = new CustomWorldWindowData();
            data.systemsToChange = new Queue<SystemData>();
            
            OnSelectionChanged();
            SetupNewWorldTemplatePath();

            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);
            if (isBaseSetup)
            {
                SetupWorldTypeEnums();
                SetupWorldTypeData();
                SetupSystemTypeData();
            }
        }

        /// <summary>
        /// Sets up the data required to display information about existing worlds
        /// </summary>
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

        void SetupSystemTypeData()
        {
            data.systemDatas = new List<SystemData>();

            Type enumType = data.bootstrapType.BaseType.GetGenericArguments()[0];
            Type attributeType = data.bootstrapType.BaseType.GetGenericArguments()[1];
            var getAllSystems = typeof(CustomWorldHelpers).GetMethod("GetAllSystemsDirect").MakeGenericMethod(enumType, attributeType);

            int index = 0;
            foreach (Enum enumValue in System.Enum.GetValues(enumType))
            {
                var relatedSystems = (IReadOnlyList<Type>) getAllSystems.Invoke(null, new object[] {WorldSystemFilterFlags.Default, enumValue, false});
                foreach (var relatedSystem in relatedSystems)
                {
                    if (relatedSystem.AssemblyQualifiedName.Contains("Unity")) continue;

                    data.systemDatas.Add(
                        new SystemData
                        {
                            name = relatedSystem.Name,
                            world = enumValue.ToString(),
                            type = relatedSystem,
                            selectedWorldIndex = index
                        }
                    );
                }
                index++;
            }
            
        }

        void SetupWorldTypeEnums()
        {
            data.worldTypeEnums = new List<string>();

            Type enumType = data.bootstrapType.BaseType.GetGenericArguments()[0];
            foreach (Enum enumValue in System.Enum.GetValues(enumType))
            {
                data.worldTypeEnums.Add(enumValue.ToString());
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
                        // AddEnumEntry();
                        AddWorld();
                    }
                }
                GUILayout.EndVertical();

                EditorGUILayout.Space(20);

                using (new GUILayout.VerticalScope())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("World", GUILayout.Width(100));
                        EditorGUILayout.LabelField("System");
                        EditorGUILayout.LabelField("Type");
                    }
                    for (int i = 0; i < data.systemDatas.Count; i++)
                    {
                        var system = data.systemDatas[i];
                        using (new GUILayout.HorizontalScope())
                        {
                            // EditorGUILayout.LabelField($"{system.world}", GUILayout.Width(100));
                            int index = EditorGUILayout.Popup(system.selectedWorldIndex, data.worldTypeEnums.ToArray(), GUILayout.Width(100));
                            EditorGUILayout.LabelField($"{system.name}");
                            EditorGUILayout.LabelField($"{system.type.AssemblyQualifiedName}");

                            if (index != system.selectedWorldIndex)
                            {
                                system.selectedWorldIndex = index;
                                data.systemsToChange.Enqueue(system);
                            }
                        }
                    }
                }
            }
            GUILayout.EndVertical();

            if (data.systemsToChange.Count != 0)
            {
                ChangeSystems();
            }
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
            data.systemsToChange = new Queue<SystemData>();
            data.packagePath = EditorPrefs.GetString("com.refsa.customworld.packagePath");
            data.projectPath = EditorPrefs.GetString("com.refsa.customworld.projectPath");

            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);

            SetupNewWorldTemplatePath();

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            if (isBaseSetup)
            {
                SetupWorldTypeEnums();
                SetupWorldTypeData();
                SetupSystemTypeData();
            }
        }
#endregion

        /// <summary>
        /// Event handler for Selection.selectionChanged event
        /// </summary>
        void OnSelectionChanged()
        {
            data.projectPath = CustomWorldsEditorHelpers.GetProjectDirectoryPath();
            data.packagePath = CustomWorldsEditorHelpers.GetPackageDirectoryPath();

            if (data.projectPath != "")
                EditorPrefs.SetString("com.refsa.customworld.projectPath", data.projectPath);
            if (data.packagePath != "")
                EditorPrefs.SetString("com.refsa.customworld.packagePath", data.packagePath);
        }
        
        /// <summary>
        /// Adds a new entry to the CustomWorldType enum
        /// </summary>
        /// <returns>true if a new entry was added</returns>
        bool AddEnumEntry()
        {
            if (data.addNewWorldTypeEnum == null || data.addNewWorldTypeEnum == "") 
            {
                return false;
            }

            foreach (string name in System.Enum.GetNames(data.bootstrapType.BaseType.GetGenericArguments()[0]))
            {   
                if (data.addNewWorldTypeEnum == name)
                {
                    data.addNewWorldTypeEnum = "";
                    return false;
                }
            }

            string worldTypeEnumPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (worldTypeEnumPath == null) return false;

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

            return true;
        }

        /// <summary>
        /// Removes an entry from the CustomWorldType enum
        /// </summary>
        /// <param name="enumEntryName"></param>
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

        /// <summary>
        /// Adds the CustomWorldType enum entry
        /// Adds class file if enum was successfully adde
        /// </summary>
        void AddWorld()
        {
            data.wantedWorldType = data.addNewWorldTypeEnum;
            if (AddEnumEntry())
            {
                AddNewWorld();
            }
            else
            {
                data.wantedWorldType = "";
            }
        }

        /// <summary>
        /// Adds a new world from the enum name in data.wantedWorldType
        /// </summary>
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

        /// <summary>
        /// Removes a world.
        /// 
        /// Deletes the class file and removes the entry from the CustomWorldType enum
        /// </summary>
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

        /// <summary>
        /// Finds the script template for CustomWorld classes
        /// </summary>
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

        void ChangeSystems()
        {
            while (data.systemsToChange.Count > 0)
            {
                var current = data.systemsToChange.Dequeue();
                string newWorldType = data.worldTypeEnums[current.selectedWorldIndex];

                string filePath = CustomWorldsEditorHelpers.FindFileInProject(current.name + ".cs");
                string fileContent = "";
                using (var file = File.OpenText(filePath))
                {
                    fileContent = file.ReadToEnd();
                }

                if (current.world == "Default")
                {
                    if (fileContent.IndexOf($"[CustomWorldType(CustomWorldType.{newWorldType})]") != -1) return;

                    int insertIndex = fileContent.IndexOf($"public class {current.name}");
                    string newContent = $"[CustomWorldType(CustomWorldType.{newWorldType})]\n";
                    fileContent = fileContent.Insert(insertIndex, newContent);
                }
                else
                {
                    if (newWorldType == "Default")
                    {
                        fileContent = fileContent.Replace($"\n[CustomWorldType(CustomWorldType.{current.world})]", "");
                    }
                    else
                    {

                    }
                }

                using (var file = File.CreateText(filePath))
                {
                    file.Write(fileContent);
                }

                current.world = newWorldType;
                AssetDatabase.Refresh();
            }
        }
    }
}