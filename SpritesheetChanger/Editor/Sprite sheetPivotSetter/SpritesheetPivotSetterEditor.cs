using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

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

        public static void ShowWindow()
        {
            SpritesheetPivotSetterEditor window = EditorWindow.GetWindow<SpritesheetPivotSetterEditor>();
            window.InitializeDefaultSpriteSheet(); 
            window.minSize = new Vector2(400, 500);
            window.maxSize = window.minSize;
        }

        private void InitializeDefaultSpriteSheet()
        {
            spriteSheets.Add(new SpriteSheetData());
        }

        private void OnEnable()
        {
            Icon = EditorGUIUtility.Load("Assets/SpritesheetChanger/Editor/Media/PivotSetterIcon.png") as Texture2D;
        }

        private Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D backgroundTexture = new Texture2D(width, height);

            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();

            return backgroundTexture;
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Box(Icon, GUILayout.Width(position.width * 1f), GUILayout.Height(100));
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            List<SpriteSheetData> copyOfSpriteSheets = new List<SpriteSheetData>(spriteSheets);

            foreach (var spriteSheetData in copyOfSpriteSheets)
            {
                DrawSpriteSheetUI(spriteSheetData);
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

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
            if (GUILayout.Button("[+] Add new", GUILayout.MinHeight(50)))
            {
                spriteSheets.Add(new SpriteSheetData());
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Modify Pivot for All", GUILayout.MinHeight(50)))
            {
                ModifyPivotForAll();
            }

            GUI.backgroundColor = originalBackgroundColor;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
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

            EditorGUILayout.BeginVertical(GUILayout.MinWidth(0), GUILayout.MaxWidth(20));

            GUILayout.Label("Custom Pivot", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("X", GUILayout.Width(12));
            spriteSheetData.customPivot.x = Mathf.Clamp01(EditorGUILayout.FloatField(spriteSheetData.customPivot.x));

            GUILayout.Label("Y", GUILayout.Width(12));
            spriteSheetData.customPivot.y = Mathf.Clamp01(EditorGUILayout.FloatField(spriteSheetData.customPivot.y));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUIStyle boldButtonStyle = new GUIStyle(GUI.skin.button);
            boldButtonStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Modify Pivot", boldButtonStyle, GUILayout.MinHeight(30), GUILayout.MinWidth(30)))
            {
                ModifyPivot(spriteSheetData);
            }

            if (GUILayout.Button("Open Sprite Editor", boldButtonStyle, GUILayout.MinHeight(30), GUILayout.MinWidth(30)))
            {
                OpenSpriteRenderer(spriteSheetData);
            }

            if (GUILayout.Button("REMOVE", boldButtonStyle, GUILayout.MinHeight(50), GUILayout.MinWidth(50)))
            {
                spriteSheets.Remove(spriteSheetData);
            }
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
                updatedSpriteSheet[i].pivot = spriteSheetData.customPivot;
                updatedSpriteSheet[i].alignment = (int)SpriteAlignment.Custom;
            }

            importer.spritesheet = updatedSpriteSheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            AssetDatabase.Refresh();
            Debug.Log("Sprite pivots modified successfully for Sprite Sheet: " + spriteSheetData.spriteSheet.name);
        }

        private void OpenSpriteRenderer(SpriteSheetData spriteSheetData)
        {
            if (spriteSheetData.spriteSheet == null)
            {
                Debug.LogError("Please assign the Sprite Sheet!");
                return;
            }

            string path = AssetDatabase.GetAssetPath(spriteSheetData.spriteSheet);
            Object spriteRendererObject = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (spriteRendererObject != null)
            {
                Selection.activeObject = spriteRendererObject;
                EditorApplication.ExecuteMenuItem("Window/2D/Sprite Editor");
            }
            else
            {
                Debug.LogError("Failed to load Sprite Renderer for the Sprite Sheet!");
            }
        }

        private void ModifyPivotForAll()
        {
            bool allSheetsComplete = true;

            foreach (var spriteSheetData in spriteSheets.ToList())
            {
                if (spriteSheetData.spriteSheet == null || spriteSheetData.customPivot == null)
                {
                    Debug.LogError("Please fill in all Sprite Sheets before modifying pivots.");
                    allSheetsComplete = false;
                    break;
                }
            }

            if (allSheetsComplete)
            {
                foreach (var spriteSheetData in spriteSheets.ToList())
                {
                    string path = AssetDatabase.GetAssetPath(spriteSheetData.spriteSheet);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

                    if (importer == null)
                    {
                        Debug.LogError("Failed to get TextureImporter for the Sprite Sheet!");
                        continue;
                    }

                    if (importer.spriteImportMode != SpriteImportMode.Multiple)
                    {
                        Debug.LogError("Sprite Mode must be set to Multiple. Please change it in the Texture Import Settings.");
                        return;
                    }

                    ModifyPivot(spriteSheetData);
                }

                Debug.Log("Sprite pivots modified successfully for all Sprite Sheets!");
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