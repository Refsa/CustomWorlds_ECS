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
        protected ModalResult result;

        protected string cancelText = "Cancel";
        protected string OKText = "OK";
        protected bool showOKButton = true;

        protected bool requestClose = false;

        public ModalResult Result => result;

        protected virtual void Cancel()
        {
            result = ModalResult.Cancel;

            owner?.ModalClosed(data);

            requestClose = true;
        }

        protected virtual void OK()
        {
            result = ModalResult.OK;

            owner?.ModalClosed(data);

            requestClose = true;
        }

        private void OnGUI()
        {
            if (requestClose) Close();

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
                if (showOKButton)
                {
                    if (GUILayout.Button(OKText))
                    {
                        OK();
                    }
                }
                if (GUILayout.Button(cancelText))
                {
                    Cancel();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        protected abstract void Draw();
    }
}