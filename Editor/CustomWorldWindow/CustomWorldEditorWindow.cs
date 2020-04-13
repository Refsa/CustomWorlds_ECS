using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.EditorCoroutines;
using Unity.EditorCoroutines.Editor;
using System.Collections;

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
        public Type type;
        public Enum worldEnum;
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
    }

    internal class CustomWorldWindow : EditorWindow
    {
        int index = -1;
        bool isBaseSetup = false;

        CustomWorldWindowData data;

        VisualTreeAsset worldTypeViewUxml;
        VisualTreeAsset systemInfoViewUxml;
		VisualTreeAsset toastMessageViewUxml;

        VisualElement worldTypeContainer;
        VisualElement systemInfoContainer;

		List<EditorCoroutine> activeCoroutines;

        public static void Create()
        {
            var window = GetWindow<CustomWorldWindow>();

            window.minSize = new Vector2(800, 600);
            window.maxSize = new Vector2(1600, 1000);

            window.titleContent = new GUIContent("Custom World Editor");
        }

        private void OnEnable() 
        {
			activeCoroutines = new List<EditorCoroutine>();

            SetupData();
            SetupView();

            Selection.selectionChanged += OnSelectionChanged;
        }

		private void OnDisable() 
        {
            Selection.selectionChanged -= OnSelectionChanged;

			foreach (var coroutine in activeCoroutines)
			{
				if (coroutine != null)
				{
					EditorCoroutineUtility.StopCoroutine(coroutine);
				}
			}
        }

#region VIEW
		void SetupView()
		{
			// TODO: Change path to retreive UXML/USS in package release
			var baseUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                (data.packagePath + "/CustomWorldWindow/UXML/BaseWindow.uxml");
                
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>
                (data.packagePath + "/CustomWorldWindow/USS/BaseStyle.uss");

            baseUxml.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(uss);

			toastMessageViewUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                	(data.packagePath + "/CustomWorldWindow/UXML/ToastMessageView.uxml");

			// AddNewToastMessage("Some Toast Message", ToastMessageStatus.Ok);

			if (isBaseSetup)
			{
				var mainViewUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                	(data.packagePath + "/CustomWorldWindow/UXML/MainView.uxml");

				worldTypeViewUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                	(data.packagePath + "/CustomWorldWindow/UXML/WorldTypeView.uxml");

            	systemInfoViewUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                	(data.packagePath + "/CustomWorldWindow/UXML/SystemInfoView.uxml");

				var mainViewElement = mainViewUxml.CloneTree();
				mainViewElement.AddToClassList("FlexTemplateContainer");

				rootVisualElement.Query("MainContainer").First().Add(mainViewElement);

            	worldTypeContainer = rootVisualElement.Query("WorldTypeInnerContainer").First();
            	systemInfoContainer = rootVisualElement.Query("SystemInfoContainer").First();

            	for (int i = 0; i < data.worldTypeData.Count; i++)
            	{
            	    AddNewWorldTypeView(data.worldTypeData[i]);
            	}

            	for (int i = 0; i < data.systemDatas.Count; i++)
            	{
            	    var system = data.systemDatas[i];
            	    AddNewSystemInfoView(system);
            	}

				var addWorldTextInput = rootVisualElement.Query("AddWorldNameInput").First() as TextField;
				(rootVisualElement.Query("AddWorldSubmit").First() as Button).clicked +=
					() => {
						data.addNewWorldTypeEnum = addWorldTextInput.value;
						AddWorld();
					};
			}
			else
			{
				var setupBootstrapView = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>
                	(data.packagePath + "/CustomWorldWindow/UXML/SetupBootstrapView.uxml").CloneTree();

				setupBootstrapView.AddToClassList("FlexTemplateContainer");
				
				(setupBootstrapView.Query("SetupBootstrap").First() as Button).clicked +=
					() =>
					{
						data.bootstrapType = CustomWorldsEditorHelpers.SetupBaseBootstrap();
					};

				rootVisualElement.Query("MainContainer").First().Add(setupBootstrapView);
			}
		}

        void AddNewWorldTypeView(WorldTypeData data)
        {
            var newElement = worldTypeViewUxml.CloneTree();

            (newElement.Query(null, "WorldName").First() as Label).text = data.name;
            (newElement.Query(null, "WorldClass").First() as Label).text = data.className;

            (newElement.Query(null, "WorldRemove").First() as Button).clicked += 
                () => {
                    this.data.wantedWorldType = data.name;
                    RemoveWorld();
                };

            worldTypeContainer.Add(newElement);
        }

        void AddNewSystemInfoView(SystemData systemData)
        {
            var newElement = systemInfoViewUxml.CloneTree();

            (newElement.Query(null, "SystemName").First() as Label).text = systemData.name;

            var enumField = (newElement.Query(null, "WorldTypeEnum").First() as EnumField);
            enumField.value = systemData.worldEnum;
            enumField.RegisterValueChangedCallback(e => {
                ChangeSystem(systemData, e.previousValue, e.newValue);
            });

            systemInfoContainer.Add(newElement);
        }

		enum ToastMessageStatus {
			Ok,
			Error
		}
		float toastInAnimationTime;
		void AddNewToastMessage(string message, ToastMessageStatus status)
		{
			var toastMessageElement = toastMessageViewUxml.CloneTree();
			toastMessageElement.AddToClassList("ToastTemplateContainer");

			(toastMessageElement.Query("ToastMessageText").First() as Label).text = message;

			rootVisualElement.Add(toastMessageElement);

			var toastMessageContainer = toastMessageElement.Query(null, "ToastMessageContainer").First();
			var toastMessageText = toastMessageElement.Query("ToastMessageText").First();

			float easeInTime = 1f;

			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseIn(toastMessageContainer, easeInTime, UIElementsAnimation.BackgroundColorEase))
			);
			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseIn(toastMessageContainer, easeInTime, UIElementsAnimation.BorderColorEase))
			);
			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseIn(toastMessageText, easeInTime, UIElementsAnimation.ColorEase)
				)
			);

			activeCoroutines.Add(
				this.StartCoroutine(RunActionAfter(toastMessageElement, 5f, ToastEaseOutAndDestroy))
			);
		}

		void ToastEaseOutAndDestroy(VisualElement element)
		{
			var toastMessageContainer = element.Query(null, "ToastMessageContainer").First();
			var toastMessageText = element.Query("ToastMessageText").First();
			float easeOutTime = 1f;

			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseOut(toastMessageContainer, easeOutTime, UIElementsAnimation.BackgroundColorEase))
			);
			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseOut(toastMessageContainer, easeOutTime, UIElementsAnimation.BorderColorEase))
			);
			activeCoroutines.Add(
				this.StartCoroutine(
					UIElementsAnimation.AnimationEaseOut(toastMessageText, easeOutTime, UIElementsAnimation.ColorEase)
				)
			);

			activeCoroutines.Add(
				this.StartCoroutine(RemoveElementAfter(element, easeOutTime * 1.1f))
			);
		}

		IEnumerator RunActionAfter(VisualElement element, float time, Action<VisualElement> action)
		{
			double startTime = EditorApplication.timeSinceStartup;
			float animTime = UIElementsAnimation.AnimationTime(startTime, time);

			while (animTime < 1f)
			{
				animTime = UIElementsAnimation.AnimationTime(startTime, time);
				yield return null;
			}

			// rootVisualElement.Remove(element);
			action?.Invoke(element);

			yield break;
		}

		IEnumerator RemoveElementAfter(VisualElement element, float time)
		{
			double startTime = EditorApplication.timeSinceStartup;
			float animTime = UIElementsAnimation.AnimationTime(startTime, time);

			while (animTime < 1f)
			{
				animTime = UIElementsAnimation.AnimationTime(startTime, time);
				yield return null;
			}

			rootVisualElement.Remove(element);
			yield break;
		}
		
#endregion

#region SETUP
        /// <summary>
        /// Sets up required data from a fresh window
        /// </summary>
        void SetupData()
        {
            data = new CustomWorldWindowData();
            
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

		/// <summary>
		/// Retreives classes derived from ComponentSystemBase
		/// </summary>
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
                            type = relatedSystem,
                            worldEnum = enumValue
                        }
                    );
                }
                index++;
            }
            
        }

		/// <summary>
		/// Retreives the enum names from CustomWorldType enum
		/// </summary>
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
            data.packagePath = EditorPrefs.GetString("com.refsa.customworld.packagePath");
            data.projectPath = EditorPrefs.GetString("com.refsa.customworld.projectPath");

            isBaseSetup = CustomWorldsEditorHelpers.IsBaseSetup(out data.bootstrapType);

            SetupNewWorldTemplatePath();

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
        bool AddEnumEntry(out string feedbackMessage)
        {
            if (data.addNewWorldTypeEnum == null || data.addNewWorldTypeEnum == "") 
            {
				feedbackMessage = "New World Type name is empty";
                return false;
            }

            foreach (string name in System.Enum.GetNames(data.bootstrapType.BaseType.GetGenericArguments()[0]))
            {   
                if (data.addNewWorldTypeEnum == name)
                {
                    data.addNewWorldTypeEnum = "";
					feedbackMessage = $"World Type of name {data.addNewWorldTypeEnum} already exists";
                    return false;
                }
            }

            string worldTypeEnumPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (worldTypeEnumPath == null) 
			{
				feedbackMessage = "Could not find CustomWorldType.cs in project";
				return false;
			}

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

			feedbackMessage = "New World Type enum entry was made";
            return true;
        }

        /// <summary>
        /// Removes an entry from the CustomWorldType enum
        /// </summary>
        /// <param name="enumEntryName"></param>
        void RemoveEnumEntry(string enumEntryName)
        {
            string worldTypeEnumPath = CustomWorldsEditorHelpers.FindFileInProject("CustomWorldType.cs");
            if (worldTypeEnumPath == null)
            {
                UnityEngine.Debug.LogError($"Couldn't find world type enum file (CustomWorldType.cs)");
                return;
            }

            string enumFileContents = "";
            using (var enumFile = File.OpenText(worldTypeEnumPath))
            {
                enumFileContents = 
                    enumFile.ReadToEnd()
                    .Replace("    " + enumEntryName + "\n", "")
                    .Replace("    " + enumEntryName + ",\n", "")
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
            if (AddEnumEntry(out string feedbackMessage))
            {
				AddNewToastMessage(feedbackMessage, ToastMessageStatus.Ok);
                AddNewWorld(out feedbackMessage);
				AddNewToastMessage(feedbackMessage, ToastMessageStatus.Ok);
            }
            else
            {
				AddNewToastMessage(feedbackMessage, ToastMessageStatus.Error);
                data.wantedWorldType = "";
            }
        }

        /// <summary>
        /// Adds a new world from the enum name in data.wantedWorldType
        /// </summary>
        void AddNewWorld(out string feedbackMessage)
        {
            if (data.wantedWorldType == null || data.wantedWorldType == "")
            {
				feedbackMessage = "Wanted world type was empty";
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
			feedbackMessage = "New World class was added";

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

            if (savePath != null) 
            {
                File.Delete(savePath);
				AddNewToastMessage($"Removed World class file: {className}", ToastMessageStatus.Ok);
            }

            RemoveEnumEntry(data.wantedWorldType);
			AddNewToastMessage($"Removed World Type enum entry of name: {data.wantedWorldType}", ToastMessageStatus.Ok);

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
            }

            data.newWorldTemplate = newWorldContents;
        }

        void ChangeSystem(SystemData data, Enum oldValue, Enum newValue)
        {
            string filePath = CustomWorldsEditorHelpers.FindFileInProject(data.name + ".cs");
            string fileContent = "";
            using (var file = File.OpenText(filePath))
            {
                fileContent = file.ReadToEnd();
            }

            if (oldValue.ToString() == "Default")
            {
                if (fileContent.IndexOf($"[CustomWorldType(CustomWorldType.{newValue.ToString()})]") != -1) return;

                int insertIndex = fileContent.IndexOf($"public class {data.name}");
                string newContent = $"[CustomWorldType(CustomWorldType.{newValue.ToString()})]\n";
                fileContent = fileContent.Insert(insertIndex, newContent);
            }
            else
            {
                if (newValue.ToString() == "Default")
                {
                    fileContent = fileContent.Replace($"\n[CustomWorldType(CustomWorldType.{oldValue.ToString()})]", "");
                }
                else
                {

                }
            }

            using (var file = File.CreateText(filePath))
            {
                file.Write(fileContent);
            }

			AddNewToastMessage($"Changed World Type on system {data.name} from {oldValue} to {newValue}", ToastMessageStatus.Ok);

            data.worldEnum = newValue;

            AssetDatabase.Refresh();
        }
    }
}