using System;
using System.Collections;
using Unity.EditorCoroutines;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Refsa.CustomWorld.Editor
{
    public static class UIElementsAnimation
    {
        public static void BackgroundColorEase(float animTime, VisualElement element)
		{
			element.style.backgroundColor = Color.Lerp(new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 1f), animTime);
		}

		public static void BorderColorEase(float animTime, VisualElement element)
		{
			Color colorLerped = Color.Lerp(new Color(0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, 1f), animTime);
			element.style.borderTopColor = colorLerped;
			element.style.borderBottomColor = colorLerped;
			element.style.borderLeftColor = colorLerped;
			element.style.borderRightColor = colorLerped;
		}

        public static void ColorEase(float animTime, VisualElement element)
        {
            element.style.color = Color.Lerp(new Color(0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, 1f), animTime);
        }

		public static float AnimationTime(double startTime, float totalTime)
		{
			return (float)(EditorApplication.timeSinceStartup - startTime) / totalTime;
		}

		public static IEnumerator AnimationEaseIn(VisualElement element, float time, Action<float, VisualElement> action)
		{
			double startTime = EditorApplication.timeSinceStartup;
			float animTime = AnimationTime(startTime, time);

			while (animTime < 1f)
			{
				action?.Invoke(animTime, element);

				animTime = AnimationTime(startTime, time);
				yield return null;
			}

			action?.Invoke(1f, element);

			yield break;
		}

        public static IEnumerator AnimationEaseOut(VisualElement element, float time, Action<float, VisualElement> action)
		{
			double startTime = EditorApplication.timeSinceStartup;
			float animTime = AnimationTime(startTime, time);

			while (animTime < 1f)
			{
				action?.Invoke(1f - animTime, element);

				animTime = AnimationTime(startTime, time);
				yield return null;
			}

			action?.Invoke(0f, element);

			yield break;
		}
    }
}