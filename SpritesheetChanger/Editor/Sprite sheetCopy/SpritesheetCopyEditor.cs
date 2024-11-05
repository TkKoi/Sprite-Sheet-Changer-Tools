using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
namespace SpritesheetChanger.Copy
{
    public class SpritesheetCopyEditor : EditorWindow
    {
        Object copyFromSptireSheet;
        List<Object> copyToSptireSheetList = new List<Object>();
        Vector2 scrollPosition = Vector2.zero;
        private static Texture2D Icon;

        public static void ShowWindow()
        {
            SpritesheetCopyEditor window = (SpritesheetCopyEditor)EditorWindow.GetWindow(typeof(SpritesheetCopyEditor));
            window.minSize = new Vector2(400, 450);
            window.maxSize = new Vector2(400, 450);
            window.Show();
        }

        private void OnEnable()
        {
            Icon = EditorGUIUtility.Load("Assets/SpritesheetChanger/Editor/Media/CopyFrom_CopyTo.png") as Texture2D;
        }

        void OnGUI()
        {
            GUILayout.Space(5);

            // Display the icon
            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(Icon, GUILayout.Width(position.width), GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();

            // Field for copyFromSptireSheet and buttons
            GUILayout.BeginHorizontal();
            copyFromSptireSheet = EditorGUILayout.ObjectField(GUIContent.none, copyFromSptireSheet, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
            GUILayout.Space(1f);

            // Copy button
            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("COPY", GUILayout.ExpandWidth(true), GUILayout.Height(100)))
            {
                CopyData();
            }
            GUILayout.Space(1f);

            GUILayout.EndHorizontal();

            GUI.backgroundColor = Color.black;
            // Sprite Editor button
            if (GUILayout.Button("Sprite Editor", GUILayout.Width(100), GUILayout.Height(25)))
            {
                OpenSpriteEditor(copyFromSptireSheet);
            }

            GUI.backgroundColor = Color.white;
            // Drop area for spritesheets
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "");

            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            GUI.Label(dropArea, "Drag and drop spritesheets here", centeredStyle);

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is Texture2D)
                            {
                                copyToSptireSheetList.Add(draggedObject);
                            }
                        }
                        evt.Use();
                    }
                }
            }

            // Check for duplicates
            var duplicateIndices = FindDuplicateIndices();
            if (duplicateIndices.Count > 0)
            {
                EditorGUILayout.HelpBox("Warning: There are duplicate sprites in the list!", MessageType.Warning);
            }

            // Scroll view for copyToSptireSheetList
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            for (int i = 0; i < copyToSptireSheetList.Count; i++)
            {
                if (copyToSptireSheetList[i] != null) // Ensure the object is not null before accessing it
                {
                    GUI.backgroundColor = duplicateIndices.Contains(i) ? Color.red : Color.white;

                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();

                    // Object field for copyTo
                    copyToSptireSheetList[i] = EditorGUILayout.ObjectField(GUIContent.none, copyToSptireSheetList[i], typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));

                    // Remove button
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        RemoveCopyToWindow(i);
                        // Since we are modifying the list, we need to break out of the loop
                        break;
                    }
                    GUI.backgroundColor = originalBackgroundColor;

                    // Open Sprite Editor button
                    if (GUILayout.Button("Open Sprite Editor", GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                    {
                        OpenSpriteEditor(copyToSptireSheetList[i]);
                    }

                    GUILayout.EndHorizontal();

                    // Display the sprite name below buttons
                    string spritePath = AssetDatabase.GetAssetPath(copyToSptireSheetList[i]);
                    string spriteName = System.IO.Path.GetFileNameWithoutExtension(spritePath);
                    EditorGUILayout.LabelField(spriteName, GUILayout.ExpandWidth(true));

                    GUILayout.EndVertical();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(25f);
        }

        List<int> FindDuplicateIndices()
        {
            var duplicateIndices = new List<int>();
            var seenSprites = new HashSet<string>();

            for (int i = 0; i < copyToSptireSheetList.Count; i++)
            {
                if (copyToSptireSheetList[i] != null)
                {
                    string spritePath = AssetDatabase.GetAssetPath(copyToSptireSheetList[i]);
                    if (!seenSprites.Add(spritePath))
                    {
                        duplicateIndices.Add(i);
                    }
                }
            }
            return duplicateIndices;
        }

        void CopyData()
        {
            if (copyFromSptireSheet == null)
            {
                Debug.LogError("The copyFrom field is empty. Please add the sprite sheet from which you want to copy the parameters!");
                return;
            }

            foreach (Object copyToObject in copyToSptireSheetList)
            {
                if (copyToObject == null)
                {
                    Debug.LogError("One of the copyTo fields is empty. Add a sprite sheet for which the parameters will be copied!");
                    continue;
                }

                if (!(copyFromSptireSheet is Texture2D) || !(copyToObject is Texture2D))
                {
                    Debug.LogError("Needs two Texture2D objects!");
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

            targetImporter.spriteImportMode = SpriteImportMode.Single;
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
