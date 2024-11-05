using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.U2D.Sprites;

#if UNITY_EDITOR

namespace SpritesheetChanger.Setter
{
    [System.Serializable]
    public class SpriteSheetData
    {
        public Texture2D spriteSheet;
        public Vector2 customPivot = new Vector2(0.5f, 0.5f);
    }

    public class SpritesheetPivotSetterEditor : EditorWindow
    {
        private List<SpriteSheetData> spriteSheets = new List<SpriteSheetData>(10);
        private Vector2 scrollPosition;
        private static Texture2D Icon;

        private bool individualMode = true;
        private Vector2 bulkPivot = new Vector2(0.5f, 0.5f);

        public static void ShowWindow()
        {
            SpritesheetPivotSetterEditor window = EditorWindow.GetWindow<SpritesheetPivotSetterEditor>();
            window.InitializeDefaultSpriteSheet(); // Initialize with default settings
            window.minSize = new Vector2(400, 500);
            window.maxSize = window.minSize;
            window.spriteSheets.Clear(); // Clear the list to ensure it starts empty
        }

        private void OnEnable()
        {
            Icon = EditorGUIUtility.Load("Assets/SpritesheetChanger/Editor/Media/PivotSetterIcon.png") as Texture2D;
            InitializeDefaultSpriteSheet(); // Initialize sprite sheets each time the window opens
        }

        private void InitializeDefaultSpriteSheet()
        {
            // Reset sprite sheets each time the window opens
            spriteSheets.Clear();
            spriteSheets.Add(new SpriteSheetData());
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(Icon, GUILayout.Width(position.width * 1f), GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            Color originalButtonColor = GUI.backgroundColor;

            if (individualMode)
            {
                GUI.backgroundColor = Color.clear;
            }
            if (GUILayout.Button("Individual Mode", GUILayout.ExpandWidth(true)))
            {
                individualMode = true;
            }
            GUI.backgroundColor = originalButtonColor;

            if (!individualMode)
            {
                GUI.backgroundColor = Color.clear;
            }
            if (GUILayout.Button("General Mod", GUILayout.ExpandWidth(true)))
            {
                individualMode = false;
            }
            GUI.backgroundColor = originalButtonColor;

            EditorGUILayout.EndHorizontal();

            if (spriteSheets.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprite sheets found. Drag and drop your sprite sheets here.", MessageType.Info);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                List<SpriteSheetData> copyOfSpriteSheets = new List<SpriteSheetData>(spriteSheets);

                foreach (var spriteSheetData in copyOfSpriteSheets)
                {
                    DrawSpriteSheetUI(spriteSheetData);
                    GUILayout.Space(10);
                }

                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // Drag and drop area
            DrawDragAndDropArea();

            EditorGUILayout.BeginHorizontal();

            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("[X] Delete All", GUILayout.MinHeight(50)))
            {
                if (EditorUtility.DisplayDialog("Delete All Sprite Sheets", "Are you sure you want to delete all Sprite Sheets?", "Yes", "No"))
                {
                    DeleteAllSpriteSheets();
                }
            }

            GUI.backgroundColor = Color.yellow;
            // Button to add new sprite sheet
            if (GUILayout.Button("[+] Add New", GUILayout.MinHeight(50)))
            {
                spriteSheets.Add(new SpriteSheetData());
            }


            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (!individualMode)
            {
                GUI.backgroundColor = Color.white;
                GUILayout.Label("Set General Pivot", EditorStyles.boldLabel);
                bulkPivot.x = Mathf.Clamp01(EditorGUILayout.FloatField("Pivot X", bulkPivot.x));
                bulkPivot.y = Mathf.Clamp01(EditorGUILayout.FloatField("Pivot Y", bulkPivot.y));

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Apply", GUILayout.MinHeight(50)))
                {
                    SetBulkPivot();
                }
            }


            if (individualMode)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Apply All Pivot", GUILayout.MinHeight(50)))
                {
                    ApplyAllPivots();
                }
            }
        }

        private void ApplyAllPivots()
        {
            foreach (var spriteSheetData in spriteSheets)
            {
                if (spriteSheetData.spriteSheet != null)
                {
                    ModifyPivot(spriteSheetData);
                }
            }
            Debug.Log("All individual pivots applied successfully.");
        }

        private void DrawDragAndDropArea()
        {
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            Rect dropArea = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            GUI.backgroundColor = Color.black;
            GUI.Box(dropArea, "Drag & Drop Sprite Sheets Here", centeredStyle);

            Event e = Event.current;
            if (dropArea.Contains(e.mousePosition))
            {
                switch (e.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (e.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (Object obj in DragAndDrop.objectReferences)
                            {
                                if (obj is Texture2D texture && !spriteSheets.Any(s => s.spriteSheet == texture))
                                {
                                    spriteSheets.Add(new SpriteSheetData { spriteSheet = texture });
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void DrawSpriteSheetUI(SpriteSheetData spriteSheetData)
        {
            GUILayout.Label("Sprite Sheet", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));
            EditorGUI.BeginChangeCheck();
            spriteSheetData.spriteSheet = (Texture2D)EditorGUILayout.ObjectField(spriteSheetData.spriteSheet, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(100));
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }

            Rect lastRect = GUILayoutUtility.GetLastRect();
            lastRect.y += 100;
            lastRect.height = 20;
            lastRect.width = 200;

            string textureInfo = spriteSheetData.spriteSheet != null ? spriteSheetData.spriteSheet.name : "Name: null";
            EditorGUI.LabelField(lastRect, textureInfo, EditorStyles.helpBox);

            GUILayout.Space(20);

            if (spriteSheetData.spriteSheet == null)
            {
                EditorGUILayout.HelpBox("Add a new sprite sheet here.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Sprite Sheet is present.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            if (individualMode)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2));
                GUILayout.Label("Custom Pivot", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Pivot X", GUILayout.Width(80));
                spriteSheetData.customPivot.x = Mathf.Clamp01(EditorGUILayout.FloatField(spriteSheetData.customPivot.x, GUILayout.Width(100))); // Управляем шириной FloatField
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Pivot Y", GUILayout.Width(80));
                spriteSheetData.customPivot.y = Mathf.Clamp01(EditorGUILayout.FloatField(spriteSheetData.customPivot.y, GUILayout.Width(100))); // Управляем шириной FloatField
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }



            EditorGUILayout.EndHorizontal();



            if (individualMode)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Modify Pivot", GUILayout.MinHeight(30), GUILayout.MinWidth(30)))
                {
                    ModifyPivot(spriteSheetData);
                }
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button("Open Sprite Editor", GUILayout.MinHeight(30), GUILayout.MinWidth(30)))
            {
                OpenSpriteEditor(spriteSheetData.spriteSheet);
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("REMOVE", GUILayout.MinHeight(50), GUILayout.MinWidth(50)))
            {
                spriteSheets.Remove(spriteSheetData);
            }
            GUI.backgroundColor = Color.white;
        }

        private void ModifyPivot(SpriteSheetData spriteSheetData)
        {
            if (spriteSheetData.spriteSheet == null)
            {
                Debug.LogError("Please assign the Sprite Sheet!");
                return;
            }

            string path = AssetDatabase.GetAssetPath(spriteSheetData.spriteSheet);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (importer == null)
            {
                Debug.LogError("Failed to get TextureImporter for the Sprite Sheet!");
                return;
            }

            if (importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                Debug.LogError("Sprite Mode must be set to Multiple. Please change it in the Texture Import Settings.");
                return;
            }

            var spriteSheet = importer.spritesheet;
            var updatedSpriteSheet = new SpriteMetaData[spriteSheet.Length];

            for (int i = 0; i < spriteSheet.Length; i++)
            {
                updatedSpriteSheet[i] = spriteSheet[i];
                updatedSpriteSheet[i].pivot = spriteSheetData.customPivot; // Используйте customPivot из spriteSheetData
                updatedSpriteSheet[i].alignment = (int)SpriteAlignment.Custom;
            }

            importer.spritesheet = updatedSpriteSheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            AssetDatabase.Refresh();
            Debug.Log("Sprite pivots modified successfully for Sprite Sheet: " + spriteSheetData.spriteSheet.name);
        }

        private void SetBulkPivot()
        {
            foreach (var spriteSheetData in spriteSheets)
            {
                spriteSheetData.customPivot = bulkPivot;
                ModifyPivot(spriteSheetData);
            }
            Debug.Log("Bulk pivot set for all sprite sheets.");
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

        private void DeleteAllSpriteSheets()
        {
            spriteSheets.Clear();
            Debug.Log("All Sprite Sheets deleted.");
        }
    }
}
#endif
