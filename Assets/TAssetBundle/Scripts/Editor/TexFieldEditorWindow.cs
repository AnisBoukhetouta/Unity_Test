using System;
using UnityEditor;
using UnityEngine;

namespace TAssetBundle.Editor
{

    public class TextFieldEditorWindow : EditorWindow
    {
        private const string ControlName = "TextField";
        public string text;
        public string buttonTitle;
        public Predicate<string> validator;
        public Action<string> onChanged;        

        private void OnGUI()
        {
            GUI.SetNextControlName(ControlName);
            text = EditorGUILayout.TextField(text);
            EditorGUI.FocusTextInControl(ControlName);


            bool enabled = true;

            if(validator != null && !validator(text))
            {
                enabled = false;
            }

            var originEnabled = GUI.enabled;
            GUI.enabled = enabled;

            if (GUILayout.Button(buttonTitle))
            {
                Apply();
            }

            GUI.enabled = originEnabled;
        }


        private void Apply()
        {
            onChanged?.Invoke(text);
            Close();
        }

        public static TextFieldEditorWindow Show(Rect position, string title, string text, string buttonTitle, Action<string> onChanged, Predicate<string> validator = null)
        {
            var window = GetWindow<TextFieldEditorWindow>(true, title, true);            
            window.position = position;
            window.text = text;
            window.buttonTitle = buttonTitle;
            window.onChanged = onChanged;
            window.validator = validator;
            window.minSize = new Vector2(300, 50);
            window.maxSize = window.minSize;
            window.ShowModalUtility();
            return window;
        }
    }

}