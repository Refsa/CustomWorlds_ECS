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

    internal interface IModal<T> where T : class
    {
        void ModalClosed(T data);
    }

    internal abstract class ModalWindow<T> : EditorWindow where T : class
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

            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(title);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.Width(position.width));
                {
                    Draw();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
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
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        protected abstract void Draw();
    }
}