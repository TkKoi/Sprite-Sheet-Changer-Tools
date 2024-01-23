using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace SpritesheetChanger.Copy
{
    public class SpritesheetCopyEditor : EditorWindow
    {
        Object copyFromSptireSheet;
        List<Object> copyToSptireSheetList = new List<Object>(); // Use a list to store multiple copyTo objects
        Vector2 scrollPosition = Vector2.zero;
        private static Texture2D Icon;

        public static void ShowWindow()
        {
            SpritesheetCopyEditor window = (SpritesheetCopyEditor)EditorWindow.GetWindow(typeof(SpritesheetCopyEditor));
            window.minSize = new Vector2(400, 450); // Increased height
            window.maxSize = window.minSize;
            window.Show();
        }

        private void OnEnable()
        {
            Icon = EditorGUIUtility.Load("Assets/SpritesheetChanger/Editor/Media/CopyFrom_CopyTo.png") as Texture2D;
        }

        void OnGUI()
        {
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(Icon, GUILayout.Width(position.width * 1f), GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            copyFromSptireSheet = EditorGUILayout.ObjectField(GUIContent.none, copyFromSptireSheet, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.Space(1f);

            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("COPY", GUILayout.ExpandWidth(true), GUILayout.Height(100)))
            {
                CopyData();
            }

            GUILayout.Space(1f);

            GUI.backgroundColor = Color.yellow;
            // Display the "+" button to add a new CopyTo window
            if (GUILayout.Button("+", GUILayout.Width(100), GUILayout.Height(100)))
            {
                AddCopyToWindow();
            }

            GUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
            // Display CopyTo windows with scrolling
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(206)); // Increased height

            for (int i = 0; i < copyToSptireSheetList.Count; i++)
            {
                GUI.backgroundColor = Color.white;
                GUILayout.BeginHorizontal();
                copyToSptireSheetList[i] = EditorGUILayout.ObjectField(GUIContent.none, copyToSptireSheetList[i], typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));

                GUI.backgroundColor = Color.red;
                // Display the "X" button to remove the corresponding CopyTo window
                if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(30)))
                {
                    RemoveCopyToWindow(i);
                }
                GUI.backgroundColor = originalBackgroundColor;
                // Display the "Open Sprite Editor" button for each CopyTo
                if (GUILayout.Button("Open Sprite Editor", GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                {
                    OpenSpriteEditor(copyToSptireSheetList[i]);
                }

                GUILayout.Space(1f);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            // Display the "Open Sprite Editor for CopyFrom" button
            GUILayout.BeginHorizontal();
            GUILayout.Space(3f);

            if (GUILayout.Button("Open Sprite Editor for CopyFrom", GUILayout.ExpandWidth(true), GUILayout.Height(30)))
            {
                OpenSpriteEditor(copyFromSptireSheet);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(25f);
        }

        void CopyData()
        {
            if (copyFromSptireSheet == null)
            {
                Debug.LogError("The copyFrom field is empty. Please add the sprite sheet from which you want to copy the parameters!");
                return;
            }

            // Validate CopyTo objects
            foreach (Object copyToObject in copyToSptireSheetList)
            {
                if (copyToObject == null)
                {
                    Debug.LogError("One of the copyTo fields is empty. Add a sprite sheet for which the parameters will be copied!");
                    continue;
                }

                if (!(copyFromSptireSheet is Texture2D) || !(copyToObject is Texture2D))
                {
                    Debug.LogError($"Needs two Texture2D objects!");
                    continue;
                }

                string copyFromPath = AssetDatabase.GetAssetPath(copyFromSptireSheet);
                string copyToPath = AssetDatabase.GetAssetPath(copyToObject);

                CopyTextureParameters(copyFromPath, copyToPath);
            }
        }

        void CopyTextureParameters(string sourcePath, string targetPath)
        {
            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;

            if (sourceImporter == null || targetImporter == null)
            {
                Debug.LogError("Failed to get Texture Importers for copyFrom or copyTo textures!");
                return;
            }

            sourceImporter.isReadable = true;
            targetImporter.isReadable = true;

            targetImporter.spriteImportMode = SpriteImportMode.Multiple;

            CopySpritesheetData(sourceImporter, targetImporter, targetPath);
        }

        void CopySpritesheetData(TextureImporter sourceImporter, TextureImporter targetImporter, string targetPath)
        {
            if (sourceImporter.spritesheet == null || sourceImporter.spritesheet.Length == 0)
            {
                Debug.LogError("No sprite data found in the source texture!");
                return;
            }

            List<SpriteMetaData> newData = new List<SpriteMetaData>();

            foreach (SpriteMetaData spriteData in sourceImporter.spritesheet)
            {
                newData.Add(spriteData);
            }

            targetImporter.spritesheet = newData.ToArray();

            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"CopyData complete! Amount of slices found: {sourceImporter.spritesheet.Length}");
        }

        void OpenSpriteEditor(Object targetObject)
        {
            if (targetObject != null)
            {
                string copyToPath = AssetDatabase.GetAssetPath(targetObject);
                Object spriteRendererObject = AssetDatabase.LoadAssetAtPath<Object>(copyToPath);

                if (spriteRendererObject != null)
                {
                    Selection.activeObject = spriteRendererObject;
                    EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor");
                }
                else
                {
                    Debug.LogError("Failed to load Sprite Editor for the specified object!");
                }
            }
            else
            {
                Debug.LogError("Cannot open Sprite Editor for a null object!");
            }
        }

        void AddCopyToWindow()
        {
            copyToSptireSheetList.Add(null);
        }

        void RemoveCopyToWindow(int index)
        {
            if (index >= 0 && index < copyToSptireSheetList.Count)
            {
                copyToSptireSheetList.RemoveAt(index);
            }
        }
    }
}
#endif