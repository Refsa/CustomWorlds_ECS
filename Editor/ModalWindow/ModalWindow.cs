using UnityEditor;
using UnityEngine;

namespace Refsa.CustomWorld.Editor
{
    internal enum ModalResult
    {
        None,
        OK,
        Cancel
    }

    internal interface IModal<T> where T : struct
    {
        void ModalClosed(T data);
    }

    internal abstract class ModalWindow<T> : EditorWindow where T : struct
    {
        protected T data;
        protected IModal<T> owner;
        protected string title = "MODALWINDOW";
        protected ModalResult result;

        public ModalResult Result => result;

        protected virtual void Cancel()
        {
            result = ModalResult.Cancel;

            owner?.ModalClosed(data);

            Close();
        }

        protected virtual void OK()
        {
            result = ModalResult.OK;

            owner?.ModalClosed(data);

            Close();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(title);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                Draw();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("OK"))
                {
                    OK();
                }
                if (GUILayout.Button("Cancel"))
                {
                    Cancel();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        protected abstract void Draw();
    }
}