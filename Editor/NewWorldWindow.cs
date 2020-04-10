using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Refsa.CustomWorld.Editor
{
    internal struct NewWorldWindowData
    {
        public string wantedWorldType;
        public string newWorldTemplate;
        public List<string> availableWorldTypes;
        public string path;
    }

    internal class NewWorldWindow : ModalWindow<NewWorldWindowData>
    {
        public static NewWorldWindow Create(Type enumType)
        {
            var window = NewWorldWindow.CreateInstance<NewWorldWindow>();

            window.title = "New Custom World";
            window.titleContent = new GUIContent("New Custom World");
            window.data = new NewWorldWindowData();
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);

            window.data.path = CustomWorldsEditorHelpers.GetProjectDirectoryPath();
            string templatePath = CustomWorldsEditorHelpers.GetPackageDirectoryPath() + "/ScriptTemplates/CustomWorld.cs.txt";
            string newWorldContents = "";

            using (var newWorldTemplate = File.OpenText(templatePath))
            {
                newWorldContents = newWorldTemplate.ReadToEnd();
            }
            if (newWorldContents == "")
            {
                UnityEngine.Debug.LogError($"Template file for new Custom Worlds not found");
                window.Cancel();
            }

            window.data.newWorldTemplate = newWorldContents;
            window.data.availableWorldTypes = new List<string>();

            foreach (string name in System.Enum.GetNames(enumType.BaseType.GetGenericArguments()[0]))
            {   
                if (!CustomWorldsEditorHelpers.ClassAlreadyExists(name + "World") && name != "Default")
                {
                    window.data.availableWorldTypes.Add(name);
                }
            }

            window.ShowModal();
            return window;
        }

        protected override void Cancel()
        {
            Close();
        }

        protected override void OK()
        {
            if (data.wantedWorldType != null && data.wantedWorldType != "")
            {
                string className = $"{data.wantedWorldType}World";

                string savePath = data.path + $"/{className}.cs";
                data.newWorldTemplate = data.newWorldTemplate.Replace("#WORLDTYPE#", data.wantedWorldType);

                using (var newWorldFile = File.CreateText(savePath))
                {
                    newWorldFile.Write(data.newWorldTemplate);
                }

                AssetDatabase.Refresh();
            }

            Close();
        }

        int index = -1;
        protected override void Draw()
        {
            index = EditorGUILayout.Popup(index, data.availableWorldTypes.ToArray());

            if (index != -1)
            {
                data.wantedWorldType = data.availableWorldTypes[index];
            }
        }
    }
}