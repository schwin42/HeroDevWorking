using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace VoxelMax
{
    [@CustomEditor(typeof(VoxelContainer))]
    public class VoxelEditor : Editor
    {
        private VoxelContainer voxelContainer;
        private GameObject exportBaseObject = null;
        private float spaceBetween = 0.05f;
        private float voxelSize = 1f;
        private bool addPhysics = true;
        private bool hideChildObject = true;

        //Main Toolbar icons
        private bool iconsLoaded = false;
        private Texture iconCursor;
        private Texture iconSelect;
        private Texture iconFloodSelect;
        private Texture iconExtrude;
        private Texture iconDraw;
        private Texture iconErase;
        private Texture iconPaint;
        private Texture iconFillPaint;
        private Texture iconMove;
        //Brush toolbar icons
        private Texture iconBrushSquare;
        private Texture iconBrushCircle;
        private Texture iconBrushHalfSphere;
        private Texture iconBrushPolyhedron;

        public VoxelEditor()
        {            
        }

        private void LoadButtonPictures()
        {
            if (iconsLoaded)
                return;
            try
            {
                //Main ToolbarIcons                
                this.iconCursor = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Cursor.png", typeof(Texture));
                this.iconSelect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Select.png", typeof(Texture));
                this.iconFloodSelect = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/FloodSelect.png", typeof(Texture));
                this.iconExtrude = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Extrude.png", typeof(Texture));
                this.iconDraw = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Draw.png", typeof(Texture));
                this.iconErase = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Erase.png", typeof(Texture));
                this.iconPaint = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Paint.png", typeof(Texture));
                this.iconFillPaint = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/FillPaint.png", typeof(Texture));
                this.iconMove = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/Move.png", typeof(Texture));

                //Brush toolbar icons
                this.iconBrushSquare = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/BrushSquare.png", typeof(Texture));
                this.iconBrushCircle = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/BrushCircle.png", typeof(Texture));
                this.iconBrushHalfSphere = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/BrushHalfSphere.png", typeof(Texture));
                this.iconBrushPolyhedron = (Texture)AssetDatabase.LoadAssetAtPath("Assets/" + StaticValues.voxelizerRootFolder + "/Source/Editor/Icons/BrushPolyhedron.png", typeof(Texture));

                iconsLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Could not load button images from the assets " + "Assets/" + StaticValues.voxelizerRootFolder);
                Debug.LogError(e.Message);
            }
        }

        private bool exploderVisible = false;
        private bool optimizerVisible = true;
        private string[] selectorModes=new string[]{"Surface select", "InDepth select", "Global select"};
        private float colorTolerance = 0f;

        private string GetModeString()
        {
            string CurModeString = "";
            switch (this.voxelContainer.editMode)
            {
                case EditModes.CursorMode:
                    CurModeString = "Cursor Mode (F5)";
                    break;
                case EditModes.SelectMode:
                    CurModeString = "Select Mode (F6)";
                    break;
                case EditModes.FloodSelectMode:
                    CurModeString = "Flood Select Mode (F7)";
                    break;
                case EditModes.ExtrudeMode:
                    CurModeString = "Extrude Mode (F8)";
                    break;
                case EditModes.DrawMode:
                    CurModeString = "Draw Mode (F9)";
                    break;
                case EditModes.EraseMode:
                    CurModeString = "Erase Mode (F10)";
                    break;
                case EditModes.PaintMode:
                    CurModeString = "Paint Mode (F11)";
                    break;
                case EditModes.FloodPaintMode:
                    CurModeString = "Flood Paint Mode (F12)";
                    break;
                case EditModes.MoveMode:
                    CurModeString = "Move Mode";
                    break;
            }
            return CurModeString;
        }
        
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                //Mode label with hotkey string
                EditorGUILayout.BeginVertical("Box");
                EditorGUILayout.LabelField("Voxel editor is not available during gameplay!");
                EditorGUILayout.EndVertical();
                return;
            }
            
            this.voxelContainer = (VoxelContainer)this.target;

            //Handle Hotkeys
            Event currentEvent = Event.current;
            if (currentEvent != null)
                this.HandleHotKeys(currentEvent.keyCode);

            //Editor Buttons
            EditorGUILayout.BeginHorizontal("Box");

            EditModes prevEditMode = this.voxelContainer.editMode;         

            //CursorMode
            bool isCursorMode = (this.voxelContainer.editMode == EditModes.CursorMode);
            isCursorMode = GUILayout.Toggle(isCursorMode, new GUIContent(this.iconCursor, "Cursor Mode (F5)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isCursorMode)
                this.voxelContainer.editMode = EditModes.CursorMode;

            //SelectMode
            bool isSelectMode = (this.voxelContainer.editMode == EditModes.SelectMode);
            isSelectMode = GUILayout.Toggle(isSelectMode, new GUIContent(this.iconSelect, "Select Voxels (F6)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isSelectMode)
                this.voxelContainer.editMode = EditModes.SelectMode;

            //FloodSelectMode
            bool isFloodSelectMode = (this.voxelContainer.editMode == EditModes.FloodSelectMode);
            isFloodSelectMode = GUILayout.Toggle(isFloodSelectMode, new GUIContent(this.iconFloodSelect, "Select Voxels by color (F7)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isFloodSelectMode)
                this.voxelContainer.editMode = EditModes.FloodSelectMode;

            //ExtrudeMode
            bool isExtrudeMode = (this.voxelContainer.editMode == EditModes.ExtrudeMode);
            isExtrudeMode = GUILayout.Toggle(isExtrudeMode, new GUIContent(this.iconExtrude, "Extrude Selected Voxels (F8)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isExtrudeMode)
                this.voxelContainer.editMode = EditModes.ExtrudeMode;

            //DrawMode
            bool isDrawMode = (this.voxelContainer.editMode == EditModes.DrawMode);
            isDrawMode = GUILayout.Toggle(isDrawMode, new GUIContent(this.iconDraw, "Draw Voxels (F9)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isDrawMode)
                this.voxelContainer.editMode = EditModes.DrawMode;

            //EraseMode
            bool isEraseMode = (this.voxelContainer.editMode == EditModes.EraseMode);
            isEraseMode = GUILayout.Toggle(isEraseMode, new GUIContent(this.iconErase, "Erase Voxels (F10)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isEraseMode)
                this.voxelContainer.editMode = EditModes.EraseMode;

            //PaintMode
            bool isPaintMode = (this.voxelContainer.editMode == EditModes.PaintMode);
            isPaintMode = GUILayout.Toggle(isPaintMode, new GUIContent(this.iconPaint, "Paint Voxels (F11)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isPaintMode)
                this.voxelContainer.editMode = EditModes.PaintMode;

            //FloodPaintMode
            bool isFloodPaintMode = (this.voxelContainer.editMode == EditModes.FloodPaintMode);
            isFloodPaintMode = GUILayout.Toggle(isFloodPaintMode, new GUIContent(this.iconFillPaint, "Paint Selected Voxels (F12)"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isFloodPaintMode)
                this.voxelContainer.editMode = EditModes.FloodPaintMode;

            //MoveMode
            bool isMoveMode = (this.voxelContainer.editMode == EditModes.MoveMode);
            isMoveMode = GUILayout.Toggle(isMoveMode, new GUIContent(this.iconMove, "Move Selected Voxels"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
            if (isMoveMode)
                this.voxelContainer.editMode = EditModes.MoveMode;
            
            //If editmode change
            if (prevEditMode != this.voxelContainer.editMode)
            {
                Tools.current = Tool.Move;
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.EndHorizontal();

            //Mode label with hotkey string
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField(this.GetModeString());
            EditorGUILayout.EndVertical();

            //Brush color
            if ((this.voxelContainer.editMode == EditModes.PaintMode) || (this.voxelContainer.editMode == EditModes.FloodPaintMode))
            {
                if (this.voxelContainer.editMode == EditModes.PaintMode)
                {
                    GUILayout.BeginVertical("Box");
                    this.voxelContainer.brushSize = EditorGUILayout.IntSlider("Brush Size", this.voxelContainer.brushSize, 1, StaticValues.maxBrushSize);
                    GUILayout.EndVertical();
                }
                GUILayout.BeginVertical("Box");                
                this.voxelContainer.brushColor = EditorGUILayout.ColorField("Brush Color", this.voxelContainer.brushColor);                
                OnGuiColorToolBar();
                GUILayout.EndVertical();
            }

            //Select mode
            if (this.voxelContainer.editMode == EditModes.SelectMode) 
            {
                EditorGUILayout.BeginVertical("box");  
                bool isSurfaceSelect = EditorGUILayout.Toggle("Surface select", this.voxelContainer.selectMode == SelectModes.SurfaceMode);
                if (isSurfaceSelect)
                    this.voxelContainer.selectMode = SelectModes.SurfaceMode;
                else
                    this.voxelContainer.selectMode = SelectModes.DepthMode;
                EditorGUILayout.EndHorizontal();
            }

            //Flood select mode
            if (this.voxelContainer.editMode == EditModes.FloodSelectMode)
            {
                EditorGUILayout.BeginVertical("box");  
                this.voxelContainer.selectMode  =(SelectModes) EditorGUILayout.Popup("Select mode", (int)this.voxelContainer.selectMode, this.selectorModes);
                this.colorTolerance             =EditorGUILayout.Slider("Color tolerance", this.colorTolerance, 0, 100);
                EditorGUILayout.EndVertical();           
            }

            //Paint and Erase Options
            if ((this.voxelContainer.editMode == EditModes.DrawMode) || (this.voxelContainer.editMode == EditModes.EraseMode))
            {
                GUILayout.BeginHorizontal("Box");
                bool isSquareBrush = (this.voxelContainer.brushMode == BrushModes.SquareBrush);
                isSquareBrush = GUILayout.Toggle(isSquareBrush, new GUIContent(this.iconBrushSquare, "Square Brush"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
                if (isSquareBrush)
                    this.voxelContainer.brushMode = BrushModes.SquareBrush;

                bool isCircleBrush = (this.voxelContainer.brushMode == BrushModes.CircleBrush);
                isCircleBrush = GUILayout.Toggle(isCircleBrush, new GUIContent(this.iconBrushCircle, "Circle Brush"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
                if (isCircleBrush)
                    this.voxelContainer.brushMode = BrushModes.CircleBrush;

                bool isSphereBrush = (this.voxelContainer.brushMode == BrushModes.SphereBrush);
                isSphereBrush = GUILayout.Toggle(isSphereBrush, new GUIContent(this.iconBrushHalfSphere, "Circle Brush"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
                if (isSphereBrush)
                    this.voxelContainer.brushMode = BrushModes.SphereBrush;

                bool isPolyhedronBrush = (this.voxelContainer.brushMode == BrushModes.PolyhedronBrush);
                isPolyhedronBrush = GUILayout.Toggle(isPolyhedronBrush, new GUIContent(this.iconBrushPolyhedron, "Circle Brush"), GUI.skin.button, GUILayout.Width(StaticValues.speedButtonHeight), GUILayout.Height(StaticValues.speedButtonHeight));
                if (isPolyhedronBrush)
                    this.voxelContainer.brushMode = BrushModes.PolyhedronBrush;

                GUILayout.EndHorizontal();                

                GUILayout.BeginVertical("Box");
                this.voxelContainer.brushSize = EditorGUILayout.IntSlider("Brush Size",this.voxelContainer.brushSize, 1, StaticValues.maxBrushSize);
                GUILayout.EndVertical();

                if (this.voxelContainer.editMode == EditModes.DrawMode)
                {
                    GUILayout.BeginVertical("Box");
                    this.voxelContainer.brushColorFromBackground = EditorGUILayout.Toggle("Brush Color From Background", this.voxelContainer.brushColorFromBackground);
                    this.voxelContainer.brushColor = EditorGUILayout.ColorField("Brush Color", this.voxelContainer.brushColor);
                    OnGuiColorToolBar();
                    GUILayout.EndVertical();
                }
            }

            //Cursor Mode
            if (this.voxelContainer.editMode == EditModes.CursorMode)
            {                           
                EditorGUILayout.BeginVertical("box");                 
                this.OnGuiOptmizeMesh();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical("box");                
                this.OnGuiExplodeIntoGameObjects();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical("box");                  
                                
                this.OnGuiSeparateSelection();  
                this.OnHollowObject();
                this.OnFillUpObject();            
                this.OnGuiDuplicateResolution();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
                                
                this.OnGuiFroceRebuild();
                this.OnGuiPrepareBoxColliders();
                this.OnGuiClearBoxColliders();
                this.OnGuiContactDeveloper();                
                EditorGUILayout.EndVertical();
            }

            //Move Mode
            if (this.voxelContainer.editMode == EditModes.MoveMode)
            {
                //Don't need to do anything yet here
            }
        }

        private void OnGuiClearBoxColliders()
        {
            if (GUILayout.Button("Clear Box Colliders"))
            {
                if (this.target != null)
                {
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;
                    Undo.RegisterCompleteObjectUndo(this.target, "Box Colliders");
                    voxelContainer.ClearBoxColliders();
                }
            }
        }

        private void OnGuiPrepareBoxColliders()
        {
            if (GUILayout.Button("Prepare Box Colliders"))
            {
                if (this.target != null)
                {
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;
                    Undo.RegisterCompleteObjectUndo(this.target, "Box Colliders");
                    voxelContainer.CreateBoxColliders();
                }
            }
        }

        private void OnHollowObject()
        {
            if (GUILayout.Button("Hollow Object"))
            {
                if (this.target != null)
                {
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;
                    Undo.RegisterCompleteObjectUndo(this.target, "Hollow");
                    voxelContainer.HollowObject();                    
                }
            }
        }

        private void OnFillUpObject()
        {
            if (GUILayout.Button("Fillup Object"))
            {
                if (this.target != null)
                {
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;
                    Undo.RegisterCompleteObjectUndo(this.target, "Fillup");
                    voxelContainer.FillUpObject();
                }
            }
        }

        private void OnGuiSeparateSelection()
        {
            if (GUILayout.Button("New Container from Selection"))
            {
                if (this.target != null)
                {
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;
                    Undo.RegisterCompleteObjectUndo(this.target, "Separate");
                    voxelContainer.SeparateSelectedVoxels();
                }
            }
        }

        private void OnGuiDuplicateResolution()
        {
            if (GUILayout.Button("Duplicate resolution"))
            {
                if (this.target != null)
                {                    
                    this.DuplicateResolution();
                }
            }
        }

        private void OnGuiFroceRebuild()
        {
            if (GUILayout.Button("Force rebuild mesh(Debug)"))
            {
                if (this.target != null)
                {
                    long rebuilStartTime = DateTime.Now.Ticks;
                    VoxelContainer voxelContainer = (VoxelContainer)this.target;                    
                    voxelContainer.SetAllAreaChanged();
                    voxelContainer.BuildMesh(true, true, true, true);
                    Debug.Log("VoxelMax " + voxelContainer.name + " Rebuild time: " + ((float)(DateTime.Now.Ticks - rebuilStartTime) / (float)TimeSpan.TicksPerSecond) + " sec");
                }
            }
        }

        private void OnGuiContactDeveloper()
        {
            if (GUILayout.Button("Contact Developer"))
            {
                System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                try
                {
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.FileName = "https://www.facebook.com/NanoidGames";
                    myProcess.Start();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("Could not open webpage: " + e.Message);
                }
            }
        }

        private void OnGuiExplodeIntoGameObjects()
        {
            EditorGUI.indentLevel++;
            this.exploderVisible = EditorGUILayout.Foldout(this.exploderVisible, "Explode into Objects");
            if (this.exploderVisible)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                this.exportBaseObject = (GameObject)EditorGUILayout.ObjectField("Base object", this.exportBaseObject, typeof(GameObject), false);
                EditorGUILayout.HelpBox("By default it is a cube.", MessageType.None);
                EditorGUILayout.EndHorizontal();

                this.voxelSize = EditorGUILayout.FloatField("Voxel Size", this.voxelSize);
                this.spaceBetween = EditorGUILayout.FloatField("Space Between", this.spaceBetween);
                this.addPhysics = EditorGUILayout.Toggle("Add physics", this.addPhysics);
                this.hideChildObject = EditorGUILayout.Toggle("Hide objects", this.hideChildObject);

                EditorGUILayout.Space();
                this.explodeRandomFactors = EditorGUILayout.Foldout(explodeRandomFactors, "Random Factors");
                if (this.explodeRandomFactors)
                {
                    this.explodeRandomFactor.MinPosition = EditorGUILayout.Vector3Field("Min position offset", this.explodeRandomFactor.MinPosition);
                    this.explodeRandomFactor.MaxPosition = EditorGUILayout.Vector3Field("Max position offset", this.explodeRandomFactor.MaxPosition);
                    this.explodeRandomFactor.MinRotation = EditorGUILayout.Vector3Field("Min rotation offset", this.explodeRandomFactor.MinRotation);
                    this.explodeRandomFactor.MaxRotation = EditorGUILayout.Vector3Field("Max rotation offset", this.explodeRandomFactor.MaxRotation);
                    this.explodeRandomFactor.MinScale = EditorGUILayout.Vector3Field("Min scale offset", this.explodeRandomFactor.MinScale);
                    this.explodeRandomFactor.MaxScale = EditorGUILayout.Vector3Field("Max scale offset", this.explodeRandomFactor.MaxScale);
                }

                //Finishing buttons
                if (GUILayout.Button("Explode into GameObjects"))
                {
                    if (this.target != null)
                    {
                        VoxelContainer voxelContainer = (VoxelContainer)this.target;
                        Renderer renderer = voxelContainer.gameObject.GetComponent<Renderer>();

                        if ((renderer != null) && (voxelContainer != null))
                        {
                            Texture2D texture = (Texture2D)renderer.sharedMaterial.mainTexture;
                            this.ExplodeIntoObjects(exportBaseObject, voxelContainer, texture, this.voxelSize, this.spaceBetween, this.addPhysics, this.hideChildObject, this.explodeRandomFactor);
                        }
                    }
                }
                if (GUILayout.Button("Tutorial"))
                {
                    System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                    try
                    {
                        myProcess.StartInfo.UseShellExecute = true;
                        myProcess.StartInfo.FileName = "https://www.youtube.com/watch?v=11ckLazjsOY";
                        myProcess.Start();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("Could not open webpage: " + e.Message);
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        private void OnGuiOptmizeMesh()
        {
            EditorGUI.indentLevel++;
            this.optimizerVisible = EditorGUILayout.Foldout(this.optimizerVisible, "Optimize Mesh");
            if (this.optimizerVisible)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical();
                if (this.target != null)
                {
                    ((VoxelContainer)this.target).optmalisationMinMergableVoxels = EditorGUILayout.IntField("Merge Factor", ((VoxelContainer)this.target).optmalisationMinMergableVoxels);
                    EditorGUILayout.HelpBox("If you set this value to 0, it will try to generate the best possible mesh. But it can takes hours to get a perfect mesh for a complex object.", MessageType.Info, true);
                }
                if (GUILayout.Button("Build optimized mesh"))
                {
                    if (this.target != null)
                    {                       
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
                        VoxelContainer voxelContainer = (VoxelContainer)this.target;
                        voxelContainer.BuildOptimizedMesh(true);
                        sw.Stop();
                        Debug.Log("VoxelMax " + this.target.name + " Optimization time: " + sw.Elapsed.TotalSeconds + " sec");                       
                        sw = null;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        //Random factors for the explode into GameObjects feature
        private bool explodeRandomFactors = false;
        public class ExplodeRandomFactors
        {
            public Vector3 MinPosition = Vector3.zero;
            public Vector3 MaxPosition = Vector3.zero;
            public Vector3 MinRotation = Vector3.zero;
            public Vector3 MaxRotation = Vector3.zero;
            public Vector3 MinScale = Vector3.one;
            public Vector3 MaxScale = Vector3.one;            
        }
        ExplodeRandomFactors explodeRandomFactor = new ExplodeRandomFactors();

        private void DuplicateResolution()
        {
            List<Vector3> vectorList = new List<Vector3>();
            vectorList.Add(Vector3.zero);
            vectorList.Add(Vector3.up);
            vectorList.Add(Vector3.right);
            vectorList.Add(Vector3.forward);
            vectorList.Add(Vector3.up + Vector3.right);
            vectorList.Add(Vector3.up + Vector3.forward);            
            vectorList.Add(Vector3.up + Vector3.right + Vector3.forward);
            vectorList.Add(Vector3.right + Vector3.forward);

            VoxelContainer sourceContainer = (VoxelContainer)this.target;
            GameObject newGameObject = new GameObject();
            newGameObject.name = sourceContainer.name + "x2";
            newGameObject.transform.localScale = sourceContainer.gameObject.transform.localScale / 2f;
            VoxelContainer newContainer=(VoxelContainer)newGameObject.AddComponent<VoxelContainer>();
            foreach (Voxel curVoxel in sourceContainer.voxels.Values)
            {
                foreach (Vector3 curShiftVector in vectorList)
                {
                    Voxel newVoxel = new Voxel();
                    newVoxel.position = curVoxel.position * 2f + curShiftVector;
                    newVoxel.color = curVoxel.color;
                    newContainer.AddVoxel(newVoxel);
                }                
            }

            newContainer.BuildMesh(true, true, true, true);
        }

        private List<Color> colorPalette = null;        
        private void ColorPaletteToGrid(List<Color> aColors)
        {
            List<Voxel> selectedVoxels = voxelContainer.GetSelectedVoxels();
            GUILayout.BeginVertical();
            try
            {
                int mod = 0;
                if (aColors.Count > 0)
                {
                    foreach (Color curColor in aColors)
                    {
                        if (aColors.IndexOf(curColor) >= 40) break;
                        mod = aColors.IndexOf(curColor) % 10;
                        if (mod == 0)
                        {
                            if (aColors.IndexOf(curColor) != 0)
                                GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                        }

                        float buttonSize = 20f;
                        foreach (Voxel curVoxel in selectedVoxels)
                        {
                            if (curVoxel.color == curColor)
                            {
                                buttonSize = 25f;
                                break;
                            }
                        }
                        Color savedColor = GUI.color;
                        GUI.color = curColor;
                        if (GUILayout.Toggle(this.voxelContainer.brushColor == curColor, "", GUI.skin.button, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                        {
                            this.voxelContainer.brushColor = curColor;
                        }
                        GUI.color = savedColor;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            finally
            {
                GUILayout.EndVertical();
            }
        }

        private void OnGuiColorToolBar()
        {
            Color originalColor = GUI.color;
            try
            {
                if ((this.voxelContainer.IsChanged()) || (colorPalette == null))
                {
                    colorPalette = voxelContainer.GetOrderedColorPalette();
                }
                EditorGUILayout.LabelField("Used colors");
                EditorGUI.indentLevel++;           
                this.ColorPaletteToGrid(colorPalette);
                EditorGUI.indentLevel--;


                EditorGUILayout.BeginVertical("Box");
                if (this.voxelContainer.colorPaletteIndex > ColorPalettes.Instance.palettes.Count)
                    this.voxelContainer.colorPaletteIndex = 0;

                if (ColorPalettes.Instance.palettes.Count==0)
                {
                    //create a new palette
                    ColorPalette newPalette = new ColorPalette();
                    newPalette.paletteName = "NewPalette";
                    ColorPalettes.Instance.palettes.Add(newPalette);
                }

                this.voxelContainer.colorPaletteIndex = EditorGUILayout.IntPopup("Color Palette", this.voxelContainer.colorPaletteIndex, ColorPalettes.Instance.GetColorPaletteNames(), null);
                ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].paletteName=EditorGUILayout.TextField("Palette name", ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].paletteName);
                this.ColorPaletteToGrid(ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].colors);

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("  Add Color "))
                {
                    if (ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].colors.IndexOf(this.voxelContainer.brushColor)<0)
                        ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].colors.Add(this.voxelContainer.brushColor);
                }
                if (GUILayout.Button("Remove Color"))
                {
                    ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].colors.Remove(this.voxelContainer.brushColor);
                }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Save"))
                {
                    string filename = ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].paletteName+".txt";
                    ColorPalettes.Instance.palettes[this.voxelContainer.colorPaletteIndex].SaveFile(ColorPalettes.Instance.GetColorPalettesPath() + filename);
                    ColorPalettes.Instance.LoadPalettes();
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Reload Palettes"))
                {                    
                    ColorPalettes.Instance.LoadPalettes();
                }
                EditorGUILayout.EndVertical();
            }
            finally
            {
                GUI.color = originalColor;
            }
        }

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            this.LoadButtonPictures();

            if ((this.target != null) && (!Application.isPlaying))
            {
                VoxelContainer curContainer = ((VoxelContainer)this.target);
                curContainer.WaitForSerialization();
                //if (!curContainer.optimized)
       //             curContainer.BuildMesh(false, true, true, true);                

                MeshFilter curMeshFilter = curContainer.GetComponent<MeshFilter>();                
                if ((curMeshFilter != null) && (curMeshFilter.sharedMesh == null))
                {
#if UNITY_5_0
					curMeshFilter.mesh = (Mesh)AssetDatabase.LoadAssetAtPath(curContainer.GetModelFileName(), typeof(Mesh));
#else
                    curMeshFilter.mesh = AssetDatabase.LoadAssetAtPath<Mesh>(curContainer.GetModelFileName());
#endif
                }

                if (!StaticValues.editorBuildFlag)
                    curContainer.editMode = EditModes.CursorMode;
                else
                {
                    //on rebuild events (like new color)
                    //the editor lost it's focus but we want to keep the edit mode in this case
                    StaticValues.editorBuildFlag = false;
                }
                Tools.current = Tool.Move;
            }
        }

        void OnDisable()
        {
            /*if (this.target != null)
            {
                VoxelContainer curContainer = ((VoxelContainer)this.target);
                if (!curContainer.optimized) {
                    curContainer.SaveMeshAndUpdateCollider();
                }
            }*/
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        void OnSceneGUI()
        {            
            //Prepare event and current object
            Event currentEvent = Event.current;
            this.voxelContainer = (VoxelContainer)this.target;
           

            //Set some unity behaviour
            EditorUtility.SetSelectedWireframeHidden(this.voxelContainer.GetComponent<Renderer>(), true);
            if (this.voxelContainer.editMode != EditModes.CursorMode)
            {
                Tools.current = Tool.None;
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }

            //Mouse events
            if (selectorDrag)
            {
                Vector3 curPos = currentEvent.mousePosition;
                Rect selectionBox = this.GetSelectorRectangle(this.selectorDragStart, curPos);
                this.DrawSelectionBox(selectionBox);
                SceneView.RepaintAll();                
            }

            if ((!voxelContainer.meshAssetUpdatedInLastBuild) &&
            ((currentEvent.type == EventType.KeyUp) || ((currentEvent.type == EventType.MouseUp))))
            {
                StaticValues.editorBuildFlag = true;
                this.voxelContainer.BuildMesh(false, true, true, true);
            }

            //Handle modes
            switch (this.voxelContainer.editMode)
            {
                case EditModes.CursorMode:
                    this.isCursorMode(currentEvent);
                    break;
                case EditModes.SelectMode:
                    this.InSelectMode(currentEvent);
                    break;
                case EditModes.ExtrudeMode:
                    this.InExtrudeMode(currentEvent);
                    break;
                case EditModes.FloodSelectMode:
                    this.InFloodSelectMode(currentEvent);
                    break;
                case EditModes.DrawMode:
                    this.InDrawMode(currentEvent);
                    break;
                case EditModes.EraseMode:
                    this.InEraseMode(currentEvent);
                    break;
                case EditModes.PaintMode:
                    this.InPaintMode(currentEvent);
                    break;
                case EditModes.FloodPaintMode:
                    this.InFloodPaintMode(currentEvent);
                    break;
                case EditModes.MoveMode:
                    this.InMoveMode(currentEvent);
                    break;
            }

            //End of Modes

            //Keyboard Events
            if (currentEvent.type == EventType.KeyDown)
            {
                //Delete Voxels
                if (this.voxelContainer.editMode != EditModes.CursorMode)
                {
                    if ((currentEvent.keyCode == KeyCode.Delete) || (currentEvent.keyCode == KeyCode.Backspace))
                    {
                        this.voxelContainer.DeleteSelectedVoxels();
                        this.voxelContainer.BuildMesh(false, false, false, true);
                        currentEvent.Use();
                    }
                }

                this.HandleHotKeys(currentEvent.keyCode);
            }
      
            //Draw Selection
            foreach (Voxel curVoxel in voxelContainer.voxels.Values)
            {
                curVoxel.DrawSelection();
            }
        }

        private void HandleHotKeys(KeyCode keyCode)
        {
            if (this.voxelContainer != null)
            {
                //Hotkey
                switch (keyCode)
                {
                    case KeyCode.F5:
                        this.voxelContainer.editMode = EditModes.CursorMode;                        
                        break;
                    case KeyCode.F6:
                        this.voxelContainer.editMode = EditModes.SelectMode;                        
                        break;
                    case KeyCode.F7:
                        this.voxelContainer.editMode = EditModes.FloodSelectMode;                       
                        break;
                    case KeyCode.F8:
                        this.voxelContainer.editMode = EditModes.ExtrudeMode;                       
                        break;
                    case KeyCode.F9:
                        this.voxelContainer.editMode = EditModes.DrawMode;                      
                        break;
                    case KeyCode.F10:
                        this.voxelContainer.editMode = EditModes.EraseMode;                        
                        break;
                    case KeyCode.F11:
                        this.voxelContainer.editMode = EditModes.PaintMode;                       
                        break;
                    case KeyCode.F12:
                        this.voxelContainer.editMode = EditModes.FloodPaintMode;                       
                        break;
                }
            }
        }

        private void InMoveMode(Event currentEvent)
        {
            Handles.color = Color.blue;
            Handles.CubeCap(0, this.voxelContainer.gameObject.transform.position, this.voxelContainer.gameObject.transform.rotation, 0.5f);

            List<Voxel> selectedVoxels = this.voxelContainer.GetSelectedVoxels();
            if (selectedVoxels.Count == 0)
                return;
            Vector3 sumVector = this.GetAvgVector(selectedVoxels);
            sumVector = this.voxelContainer.gameObject.transform.TransformPoint(sumVector);
            Vector3 newVector = Handles.PositionHandle(sumVector, this.voxelContainer.gameObject.transform.rotation)-sumVector;
            newVector = this.voxelContainer.transform.InverseTransformDirection(newVector);
            newVector.x = (Mathf.Abs(Mathf.Round(newVector.x)) > 0f) ? 1f * Mathf.Sign(newVector.x) : 0f;
            newVector.y = (Mathf.Abs(Mathf.Round(newVector.y)) > 0f) ? 1f * Mathf.Sign(newVector.y) : 0f;
            newVector.z = (Mathf.Abs(Mathf.Round(newVector.z)) > 0f) ? 1f * Mathf.Sign(newVector.z) : 0f;
            if (newVector.magnitude >= 1f)
            {
                for (int i = 0; i < selectedVoxels.Count; i++)
                {
                    this.voxelContainer.RemoveVoxel(selectedVoxels[i], false);
                    selectedVoxels[i].position += newVector;
                }
                for (int i = 0; i < selectedVoxels.Count; i++)
                {
                    if (!this.voxelContainer.voxels.ContainsKey(selectedVoxels[i].position))
                        this.voxelContainer.AddVoxel(selectedVoxels[i], false);
                }
                this.voxelContainer.UpdateStructure();
                this.voxelContainer.BuildMesh(false, false, false, true);
            }            
        }

        #region EditingModes
        private void isCursorMode(Event currentEvent)
        {
        }

        private void InSelectMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {
                //Handle DoubleClick
                bool doubleClicked = false;
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (milliseconds - lastMouseUp < 300)
                {
                    doubleClicked = true;
                    selectorDrag = false;
                    if (this.voxelContainer.HasSelectedVoxels())
                    {
                        Undo.RegisterCompleteObjectUndo(this.target, "Selection changed");
                        this.voxelContainer.DeselectAllVoxels();
                    }
                    else
                    {
                        Undo.RegisterCompleteObjectUndo(this.target, "Selection changed");
                        this.voxelContainer.SelectAllVoxels();
                    }
                }
                this.lastMouseUp = milliseconds;
                if (!doubleClicked)
                {
                    if (!aEvent.control)
                        this.voxelContainer.DeselectAllVoxels();
                    //Single Click
                    float moveDistance = Vector2.Distance(this.selectorDragStart, new Vector2(aEvent.mousePosition.x, aEvent.mousePosition.y));
                    if ((!this.selectorDrag) || (moveDistance < 5f))
                    {
                        selectorDrag = false;
                        Ray currentRay = HandleUtility.GUIPointToWorldRay(aEvent.mousePosition);
                        RaycastHit hitInfo;
                        if (Physics.Raycast(currentRay, out hitInfo))
                        {
                            Voxel selectedVoxel = voxelContainer.GetVoxelByCoordinate(hitInfo.point, 0.5f);
                            if (selectedVoxel != null)
                            {
                                Undo.RegisterCompleteObjectUndo(this.target, "Selection changed");
                                selectedVoxel.selected = !selectedVoxel.selected;
                                SceneView.RepaintAll();                                
                            }
                        }
                    }
                    else
                    {
                        
                        //Cursor Drag 
                        this.selectorDrag = false;
                        Vector3 curPos = aEvent.mousePosition;
                        Rect selectionBox = this.GetSelectorRectangle(this.selectorDragStart, curPos);
                        foreach (Voxel curVoxel in this.voxelContainer.voxels.Values)
                        {                            
                            Vector2 voxelScreenPos = HandleUtility.WorldToGUIPoint(this.voxelContainer.transform.TransformPoint(curVoxel.position));
                            if ((selectionBox.Contains(voxelScreenPos)) && (this.IsVisibleVoxel(curVoxel) || this.voxelContainer.selectMode == SelectModes.DepthMode))
                            {
                                curVoxel.selected = true;
                            }
                        }
                        SceneView.RepaintAll();
                        Undo.RegisterCompleteObjectUndo(this.target, "Selection changed");
                    }
                }
            }
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseDown))
            {
                if (!selectorDrag)
                {
                    selectorDrag = true;
                    this.selectorDragStart = aEvent.mousePosition;
                }
                aEvent.Use();
            }
        }

        private void InExtrudeMode(Event aEvent)
        {            
            //Right
            Handles.color = Color.green;
            Vector3 extrudePos = this.GetExtrudeVectorPos(Vector3.right);
            extrudeVector = Handles.Slider(extrudeVector + extrudePos, this.voxelContainer.transform.TransformDirection(Vector3.right)) - extrudePos;
            extrudeVector = this.voxelContainer.transform.InverseTransformDirection(extrudeVector);
            if (Mathf.Abs(this.extrudeVector.x) >= 1f)
            {
                extrudeVector.x = 1f * Mathf.Sign(extrudeVector.x);
                extrudeVector.y = 0f;
                extrudeVector.z = 0f;

                this.Extrude(this.extrudeVector);
                this.extrudeVector = Vector3.zero;
                EditorUtility.SetDirty(target);
            }

            //Up
            Handles.color = Color.red;
            extrudePos = this.GetExtrudeVectorPos(Vector3.up);
            extrudeVector = Handles.Slider(extrudeVector + extrudePos, this.voxelContainer.transform.TransformDirection(Vector3.up)) - extrudePos;
            extrudeVector = this.voxelContainer.transform.InverseTransformDirection(extrudeVector);
            if (Mathf.Abs(extrudeVector.y) >= 1f)
            {
                extrudeVector.y = 1f * Mathf.Sign(extrudeVector.y);
                extrudeVector.x = 0f;
                extrudeVector.z = 0f;

                this.Extrude(this.extrudeVector);
                this.extrudeVector = Vector3.zero;
                EditorUtility.SetDirty(target);
            }

            //Forward
            Handles.color = Color.blue;            
            extrudePos = this.GetExtrudeVectorPos(Vector3.forward);
            extrudeVector = Handles.Slider(extrudeVector + extrudePos, this.voxelContainer.transform.TransformDirection(Vector3.forward)) - extrudePos;
            extrudeVector = this.voxelContainer.transform.InverseTransformDirection(extrudeVector);
            if (Mathf.Abs(this.extrudeVector.z) >= 1f)
            {
                extrudeVector.z = 1f * Mathf.Sign(extrudeVector.z);
                extrudeVector.x = 0f;
                extrudeVector.y = 0f;

                this.Extrude(this.extrudeVector);
                this.extrudeVector = Vector3.zero;
                EditorUtility.SetDirty(target);
            }
        }

        private void InFloodSelectMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {
                if (!aEvent.control)
                    this.voxelContainer.DeselectAllVoxels();

                Ray currentRay = HandleUtility.GUIPointToWorldRay(aEvent.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(currentRay, out hitInfo))
                {
                    Voxel selectedVoxel = voxelContainer.GetVoxelByCoordinate(hitInfo.point, 0.5f);
                    if (selectedVoxel != null)
                    {
                        Undo.RegisterCompleteObjectUndo(this.target, "Selection changed");
                        List<Voxel> floodelectedVoxels = null;
                        floodelectedVoxels = this.voxelContainer.FloodSelect(selectedVoxel, this.colorTolerance/100f);              
                        if (floodelectedVoxels != null)
                        {
                            foreach (Voxel curVoxel in floodelectedVoxels)
                            {
                                if (this.IsVisibleVoxel(curVoxel) || (this.voxelContainer.selectMode == SelectModes.DepthMode) || (this.voxelContainer.selectMode == SelectModes.GlobalMode))
                                {
                                    curVoxel.selected = true;
                                }
                                else
                                {
                                    curVoxel.selected = false;
                                }
                            }
                        }
                    }
                }
                SceneView.RepaintAll();                
            }
        }

        private Color GetColorForNewVoxel(Vector3 aPosition, Vector3 aDirectionVector)
        {
            Color resultColor = this.voxelContainer.brushColor;
            if (this.voxelContainer.brushColorFromBackground)
            {
                for (int i = 0; i <= 20; i++) { 
                    Voxel backgroundVoxel = this.voxelContainer.GetVoxelByCoordinate(aPosition - (aDirectionVector * ((float)i)));
                    if (backgroundVoxel != null)
                    {
                        resultColor = backgroundVoxel.color;
                        break;
                    }
                }
            }
            return resultColor;
        }
        private void FillUpBackground(Vector3 aPosition, Vector3 aDirectionVector, Color aColor)
        {
            bool hasBackground = false;
            for (int i = 1; i <= 20; i++)
            {
                Voxel backgroundVoxel = this.voxelContainer.GetVoxelByCoordinate(aPosition - (aDirectionVector * ((float)i)));
                if (backgroundVoxel != null)
                {
                    hasBackground = true;
                    break;
                }
            }
            if (hasBackground)
            {
                for (int i = 1; i <= 20; i++)
                {
                    Vector3 curPosition = aPosition - (aDirectionVector * ((float)i));
                    Voxel backgroundVoxel = this.voxelContainer.GetVoxelByCoordinate(curPosition);
                    if (backgroundVoxel == null)
                    {
                        Voxel newVoxel = new Voxel();
                        newVoxel.position = curPosition;
                        newVoxel.color = aColor;
                        this.voxelContainer.AddVoxel(newVoxel);                                                
                    }
                    else
                        break;
                }
            }
        }
        private Vector3 drawDirectionVector = Vector3.zero;
        private List<Vector3> addedVoxels = null;
        private void InDrawMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseDown))
                this.inActionMouseDown = true;
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {                
                this.inActionMouseDown = false;
                drawDirectionVector = Vector3.zero;
                addedVoxels = null;
            }

            if (inActionMouseDown)
            {
                if (addedVoxels == null)
                    addedVoxels = new List<Vector3>();
                                                   
                Ray currentRay = HandleUtility.GUIPointToWorldRay(aEvent.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(currentRay, out hitInfo))
                {
                    Voxel curVoxel = this.voxelContainer.GetVoxelByCoordinate(hitInfo.point, 0.5f);
                    if ((curVoxel != null) && (!addedVoxels.Contains(curVoxel.position)))
                    {
                        bool undoActionAdded = false;
                        if (drawDirectionVector == Vector3.zero) {
                            drawDirectionVector = this.voxelContainer.transform.TransformPoint(curVoxel.position);
                            drawDirectionVector = hitInfo.point - drawDirectionVector;
                            drawDirectionVector = this.GetDirectionVector(drawDirectionVector);                            
                        }

                        Vector3 vectorX = new Vector3();
                        Vector3 vectorY = new Vector3();
                        this.GetPlaneVectors(drawDirectionVector, ref vectorX, ref vectorY);
                        int minX = this.voxelContainer.brushSize/2;

                        Vector3 origoPosition = curVoxel.position + drawDirectionVector;

                        switch (this.voxelContainer.brushMode)
                        {
                            case BrushModes.SquareBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {                                
                                        Vector3 newPosition = curVoxel.position + drawDirectionVector + (vectorX * i) + (vectorY * j);
                                        if (!this.voxelContainer.voxels.ContainsKey(newPosition)) 
                                        {
                                            //We only would like to create one undo action
                                            if (!undoActionAdded)
                                            {
                                                Undo.RegisterCompleteObjectUndo(this.target, "Voxel added");
                                                undoActionAdded = true;
                                            }
                                            Voxel newVoxel      = new Voxel();
                                            newVoxel.position   = newPosition;
                                            newVoxel.color      = this.GetColorForNewVoxel(newPosition, drawDirectionVector);
                                            this.voxelContainer.AddVoxel(newVoxel);
                                            addedVoxels.Add(newPosition);
                                            this.FillUpBackground(newPosition, drawDirectionVector, newVoxel.color);
                                        }                                        
                                    }
                                }
                                break;

                            case BrushModes.CircleBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {                                        
                                        Vector3 newPosition = curVoxel.position + drawDirectionVector + (vectorX * i) + (vectorY * j);
                                        if ((!this.voxelContainer.voxels.ContainsKey(newPosition)) &&
                                            (Vector3.Distance(origoPosition, newPosition) <= (minX)))
                                        {
                                            //We only would like to create one undo action
                                            if (!undoActionAdded)
                                            {
                                                Undo.RegisterCompleteObjectUndo(this.target, "Voxel added");
                                                undoActionAdded = true;
                                            }
                                            Voxel newVoxel      = new Voxel();
                                            newVoxel.position   = newPosition;
                                            newVoxel.color      = this.GetColorForNewVoxel(newPosition, drawDirectionVector);
                                            this.voxelContainer.AddVoxel(newVoxel);
                                            addedVoxels.Add(newPosition);
                                            this.FillUpBackground(newPosition, drawDirectionVector, newVoxel.color);
                                        }                                        
                                    }
                                }
                                break;

                            case BrushModes.SphereBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {
                                        for (int k = 1; k <= minX; k++)
                                        {
                                            Vector3 newPosition = curVoxel.position + drawDirectionVector * k + (vectorX * i) + (vectorY * j);
                                            if ((!this.voxelContainer.voxels.ContainsKey(newPosition)) &&
                                                (Vector3.Distance(origoPosition, newPosition) <= (minX)))
                                            {
                                                //We only would like to create one undo action
                                                if (!undoActionAdded)
                                                {
                                                    Undo.RegisterCompleteObjectUndo(this.target, "Voxel added");
                                                    undoActionAdded = true;
                                                }
                                                Voxel newVoxel       = new Voxel();
                                                newVoxel.position    = newPosition;
                                                newVoxel.color       = this.GetColorForNewVoxel(newPosition, drawDirectionVector);
                                                this.voxelContainer.AddVoxel(newVoxel);
                                                addedVoxels.Add(newPosition);
                                                this.FillUpBackground(newPosition, drawDirectionVector, newVoxel.color);
                                            }
                                        }
                                    }
                                }
                                break;

                            case BrushModes.PolyhedronBrush:
                                for (int k = 0; k <= minX; k++)
                                for (int i = -minX+k; i <= minX-k; i++)
                                {
                                    for (int j = -minX+k; j <= minX-k; j++)
                                    {
                                        Vector3 newPosition = curVoxel.position + (drawDirectionVector * (k + 1)) + (vectorX * i) + (vectorY * j);
                                        if (!this.voxelContainer.voxels.ContainsKey(newPosition))
                                        {
                                            //We only would like to create one undo action
                                            if (!undoActionAdded)
                                            {
                                                Undo.RegisterCompleteObjectUndo(this.target, "Voxel added");
                                                undoActionAdded = true;
                                            }
                                            Voxel newVoxel = new Voxel();
                                            newVoxel.position = newPosition;
                                            newVoxel.color = this.GetColorForNewVoxel(newPosition, drawDirectionVector);
                                            this.voxelContainer.AddVoxel(newVoxel);
                                            addedVoxels.Add(newPosition);
                                            this.FillUpBackground(newPosition, drawDirectionVector, newVoxel.color);
                                        }
                                    }
                                }
                                break;
                        }

                      
                        if (undoActionAdded) { 
                            this.voxelContainer.BuildMesh(false, false, false, false);
                            //if (this != null)
                             //   EditorUtility.SetDirty(this);
                        }
                    }
                }
            }
        }

        private bool inActionMouseDown = false;
        Vector3 eraseDirectionVector = Vector3.zero;
        private void InEraseMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseDown))
            {
                if (!this.inActionMouseDown)
                    Undo.RegisterCompleteObjectUndo(this.target, "Voxel removed");
                this.inActionMouseDown = true;
            }
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {
                this.inActionMouseDown = false;
                this.eraseDirectionVector = Vector3.zero;
            }

            if (inActionMouseDown)
            {
                Ray currentRay = HandleUtility.GUIPointToWorldRay(aEvent.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(currentRay, out hitInfo))
                {
                    Voxel curVoxel = this.voxelContainer.GetVoxelByCoordinate(hitInfo.point, 0.5f);
                    if (curVoxel != null)
                    {
                        bool undoActionAdded = false;
                        if (eraseDirectionVector == Vector3.zero)
                        {
                            eraseDirectionVector = this.voxelContainer.transform.TransformPoint(curVoxel.position);
                            eraseDirectionVector = hitInfo.point - eraseDirectionVector;
                            eraseDirectionVector = this.GetDirectionVector(eraseDirectionVector);
                        }

                        Vector3 vectorX = new Vector3();
                        Vector3 vectorY = new Vector3();
                        this.GetPlaneVectors(eraseDirectionVector, ref vectorX, ref vectorY);
                        int minX = this.voxelContainer.brushSize / 2;

                        Vector3 origoPosition = curVoxel.position + eraseDirectionVector;

                        switch (this.voxelContainer.brushMode)
                        {
                            case BrushModes.SquareBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {
                                        Vector3 newPosition = curVoxel.position + (vectorX * i) + (vectorY * j);
                                        if (this.voxelContainer.voxels.ContainsKey(newPosition))
                                        {
                                            //We only would like to create one undo action
                                            if (!undoActionAdded)
                                            {
                                                Undo.RegisterCompleteObjectUndo(this.target, "Voxel removed");
                                                undoActionAdded = true;
                                            }
                                            Voxel voxelToRemove=this.voxelContainer.GetVoxelByCoordinate(newPosition);                                            
                                            this.voxelContainer.RemoveVoxel(voxelToRemove);
                                        }
                                    }
                                }
                                break;

                            case BrushModes.CircleBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {
                                        Vector3 newPosition = curVoxel.position + (vectorX * i) + (vectorY * j);
                                        if ((this.voxelContainer.voxels.ContainsKey(newPosition)) &&
                                            (Vector3.Distance(origoPosition, newPosition) <= (minX)))
                                        {
                                            //We only would like to create one undo action
                                            if (!undoActionAdded)
                                            {
                                                Undo.RegisterCompleteObjectUndo(this.target, "Voxel removed");
                                                undoActionAdded = true;
                                            }
                                            Voxel voxelToRemove = this.voxelContainer.GetVoxelByCoordinate(newPosition);
                                            this.voxelContainer.RemoveVoxel(voxelToRemove);
                                        }
                                    }
                                }
                                break;

                            case BrushModes.SphereBrush:
                                for (int i = -minX; i <= minX; i++)
                                {
                                    for (int j = -minX; j <= minX; j++)
                                    {
                                        for (int k = 0; k <= minX; k++)
                                        {
                                            Vector3 newPosition = curVoxel.position + (-eraseDirectionVector * k) + (vectorX * i) + (vectorY * j);
                                            if ((this.voxelContainer.voxels.ContainsKey(newPosition)) &&
                                                (Vector3.Distance(origoPosition, newPosition) <= (minX)))
                                            {
                                                //We only would like to create one undo action
                                                if (!undoActionAdded)
                                                {
                                                    Undo.RegisterCompleteObjectUndo(this.target, "Voxel removed");
                                                    undoActionAdded = true;
                                                }
                                                Voxel voxelToRemove = this.voxelContainer.GetVoxelByCoordinate(newPosition);
                                                this.voxelContainer.RemoveVoxel(voxelToRemove);
                                            }
                                        }
                                    }
                                }
                                break;

                            case BrushModes.PolyhedronBrush:
                                for (int k = 0; k <= minX; k++)
                                    for (int i = -minX + k; i <= minX - k; i++)
                                    {
                                        for (int j = -minX + k; j <= minX - k; j++)
                                        {
                                            Vector3 newPosition = curVoxel.position + (-eraseDirectionVector * k) + (vectorX * i) + (vectorY * j);
                                            if (this.voxelContainer.voxels.ContainsKey(newPosition))
                                            {
                                                //We only would like to create one undo action
                                                if (!undoActionAdded)
                                                {
                                                    Undo.RegisterCompleteObjectUndo(this.target, "Voxel removed");
                                                    undoActionAdded = true;
                                                }
                                                Voxel voxelToRemove = this.voxelContainer.GetVoxelByCoordinate(newPosition);
                                                this.voxelContainer.RemoveVoxel(voxelToRemove);
                                            }
                                        }
                                    }
                                break;
                        }


                        if (undoActionAdded)
                        {
                            this.voxelContainer.BuildMesh(false, false, false, false);
                            if (this != null)
                                EditorUtility.SetDirty(this);
                        }
                    }
                }
            }
        }

        private void InPaintMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseDown))
                this.inActionMouseDown = true;
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {              
                this.inActionMouseDown = false;
            }

            if (inActionMouseDown)
            {
                Ray currentRay = HandleUtility.GUIPointToWorldRay(aEvent.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(currentRay, out hitInfo))
                {
                    Voxel curVoxel = this.voxelContainer.GetVoxelByCoordinate(hitInfo.point, 0.5f);
                    if (curVoxel != null)
                    {
                        Vector3 directionVector = this.voxelContainer.transform.TransformPoint(curVoxel.position);
                        directionVector = hitInfo.point - directionVector;
                        directionVector = this.GetDirectionVector(directionVector);

                        Vector3 vectorX = new Vector3();
                        Vector3 vectorY = new Vector3();
                        this.GetPlaneVectors(directionVector, ref vectorX, ref vectorY);
                        int minX = this.voxelContainer.brushSize / 2;

                        bool undoActionAdded = false;
                        for (int i = -minX; i <= minX; i++)
                        {
                            for (int j = -minX; j <= minX; j++)
                            {
                                Vector3 newPosition = curVoxel.position + (vectorX * i) + (vectorY * j);
                                if (this.voxelContainer.voxels.ContainsKey(newPosition))
                                {
                                    //We only would like to create one undo action
                                    if (!undoActionAdded)
                                    {
                                        Undo.RegisterCompleteObjectUndo(this.target, "Voxel Painted");
                                        undoActionAdded = true;
                                    }
                                    Voxel voxelToColor = this.voxelContainer.GetVoxelByCoordinate(newPosition);
                                    voxelToColor.color = this.voxelContainer.brushColor;
                                }
                            }
                        }                              

                        this.voxelContainer.BuildMesh(false, false, false, false);

                        if (this != null)
                            EditorUtility.SetDirty(this);
                    }
                }
            }
        }

        private void InFloodPaintMode(Event aEvent)
        {
            if ((aEvent.button == 0) && (aEvent.type == EventType.MouseUp))
            {
                Undo.RegisterCompleteObjectUndo(this.target, "Color changed");
                foreach (Voxel curVoxel in this.voxelContainer.voxels.Values)
                {
                    if (curVoxel.selected)
                        curVoxel.color = this.voxelContainer.brushColor;
                }
                StaticValues.editorBuildFlag = true;
                this.voxelContainer.BuildMesh(false, true, false, true); 

                this.inActionMouseDown = false;
            }
        }
        #endregion


        #region EditingFunctions
        private void GetPlaneVectors(Vector3 aNormalVector, ref Vector3 rVectorA, ref Vector3 rVectorB)
        {
            if ((aNormalVector==Vector3.up)  || (aNormalVector==Vector3.down))
            {
                rVectorA = Vector3.right;
                rVectorB = Vector3.forward;
            }

            if ((aNormalVector == Vector3.right) || (aNormalVector == Vector3.left))
            {
                rVectorA = Vector3.up;
                rVectorB = Vector3.forward;
            }

            if ((aNormalVector == Vector3.forward) || (aNormalVector == Vector3.back))
            {
                rVectorA = Vector3.up;
                rVectorB = Vector3.right;
            }
        }
        private Vector3 GetDirectionVector(Vector3 aVector)
        {
            aVector.Normalize();
            Vector3 result = Vector3.zero;
            float curDistance = 1f;
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.up), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.up), aVector);
                result = Vector3.up;
            }
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.down), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.down), aVector);
                result = Vector3.down;
            }
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.left), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.left), aVector);
                result = Vector3.left;
            }
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.right), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.right), aVector);
                result = Vector3.right;
            }
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.forward), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.forward), aVector);
                result = Vector3.forward;
            }
            if (Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.back), aVector) < curDistance)
            {
                curDistance = Vector3.Distance(this.voxelContainer.transform.TransformDirection(Vector3.back), aVector);
                result = Vector3.back;
            }
            return result;
        }

        private bool IsVisibleVoxel(Voxel aVoxel)
        {            
            Vector3 possibleScreenPos = Camera.current.WorldToScreenPoint(this.voxelContainer.transform.TransformPoint(aVoxel.position));
            Ray currentRay = Camera.current.ScreenPointToRay(possibleScreenPos);
            RaycastHit hitInfo;
            if (Physics.Raycast(currentRay, out hitInfo))
            {
                if (Vector3.Distance(hitInfo.point, this.voxelContainer.transform.TransformPoint(aVoxel.position)) < (0.5f * this.voxelContainer.transform.lossyScale.magnitude)) return true;
            }
            return false;
        }

        private void DrawSelectionBox(Rect aRect)
        {
            
            Vector3 lowerLeft = HandleUtility.GUIPointToWorldRay(new Vector2(aRect.xMin, aRect.yMin)).GetPoint(0.5f);
            Vector3 upperLeft = HandleUtility.GUIPointToWorldRay(new Vector2(aRect.xMin, aRect.yMin + aRect.height)).GetPoint(0.5f);
            Vector3 upperRight = HandleUtility.GUIPointToWorldRay(new Vector2(aRect.xMin + aRect.width, aRect.yMin + aRect.height)).GetPoint(0.5f);
            Vector3 lowerRight = HandleUtility.GUIPointToWorldRay(new Vector2(aRect.xMin + aRect.width, aRect.yMin)).GetPoint(0.5f);
            Handles.color = Color.blue;
            Handles.DrawLine(lowerLeft, upperLeft);
            Handles.DrawLine(upperLeft, upperRight);
            Handles.DrawLine(upperRight, lowerRight);
            Handles.DrawLine(lowerRight, lowerLeft);
        }


        private bool selectorDrag = false;
        private Vector2 selectorDragStart = Vector2.zero;
        private long lastMouseUp = 0;

        private Rect GetSelectorRectangle(Vector2 startPos, Vector2 endPos)
        {
            startPos = HandleUtility.WorldToGUIPoint(HandleUtility.GUIPointToWorldRay(startPos).origin);
            endPos = HandleUtility.WorldToGUIPoint(HandleUtility.GUIPointToWorldRay(endPos).origin);
            //endPos.y = Camera.current.pixelHeight - endPos.y;
            //            startPos.y = Camera.current.pixelHeight - startPos.y;
            Rect selectionBox = new Rect(Mathf.Min(startPos.x, endPos.x), Mathf.Min(startPos.y, endPos.y),
                                         Mathf.Abs(startPos.x - endPos.x),
                                         Mathf.Abs(startPos.y - endPos.y));

            return selectionBox;
        }

        private const float handlerLength = 1f;

        private Vector3 GetAvgVector(List<Voxel> aSelectedVoxels)
        {
            Vector3 sumVector = new Vector3();
            foreach (Voxel curVoxel in aSelectedVoxels)
            {
                sumVector += curVoxel.position;
            }
            return sumVector / aSelectedVoxels.Count;
        }

        private Vector3 GetMaxVector(List<Voxel> aSelectedVoxels)
        {
            Vector3 maxVector = new Vector3();
            if (aSelectedVoxels.Count > 0)
                maxVector = aSelectedVoxels[0].position;
            foreach (Voxel curVoxel in aSelectedVoxels)
            {
                if (maxVector.x < curVoxel.position.x)
                {
                    maxVector.x = curVoxel.position.x;
                }
                if (maxVector.y < curVoxel.position.y)
                {
                    maxVector.y = curVoxel.position.y;
                }
                if (maxVector.z < curVoxel.position.z)
                {
                    maxVector.z = curVoxel.position.z;
                }
            }
            return maxVector;
        }

        private Vector3 extrudeVector = Vector3.zero;
         
        private Vector3 GetExtrudeVectorPos(Vector3 direction)
        {
            List<Voxel> selectedVoxels = this.voxelContainer.GetSelectedVoxels();
            if (selectedVoxels.Count == 0)
                return Vector3.zero;
            Vector3 sumVector = this.GetAvgVector(selectedVoxels);
            Vector3 maxVector = this.GetMaxVector(selectedVoxels);

            if (direction == Vector3.forward)
            {
                sumVector.z = maxVector.z;
            }
            if (direction == Vector3.right)
            {
                sumVector.x = maxVector.x;
            }
            if (direction == Vector3.up)
            {
                sumVector.y = maxVector.y;
            }
            direction = direction * 0.1f;
            //  Handles.color = Color.blue;

            return this.voxelContainer.transform.TransformPoint(sumVector) + direction;
        }

        public void Extrude(Vector3 aDirection)
        {
            Undo.RegisterCompleteObjectUndo(this.target, "Extrude");

            List<Voxel> voxelList = this.voxelContainer.GetSelectedVoxels();
            this.voxelContainer.DeselectAllVoxels();
            foreach (Voxel curVoxel in voxelList)
            {
                Vector3 newPosition = curVoxel.position + aDirection;
                if (!voxelContainer.voxels.ContainsKey(newPosition))
                {
                    Voxel newVoxel = new Voxel();
                    newVoxel.color = curVoxel.color;
                    newVoxel.position = newPosition;
                    newVoxel.selected = true;
                    voxelContainer.AddVoxel(newVoxel);
                }
                else
                {
                    Vector3 prevPosition = curVoxel.position - aDirection;
                    if (!voxelContainer.voxels.ContainsKey(prevPosition))
                    {
                        voxelContainer.voxels[newPosition].color = curVoxel.color;
                        voxelContainer.RemoveVoxel(curVoxel);                        
                        voxelContainer.voxels[newPosition].selected = true;
                    }
                    else
                    {
                        curVoxel.selected = false;
                    }
                }
            }
            voxelContainer.BuildMesh(false, false, false, true);
        }

        private Vector3 RandomVector(Vector3 aMinVector, Vector3 aMaxVector)
        {                        
            Vector3 resultVector = Vector3.zero;
            resultVector.x = UnityEngine.Random.Range(aMinVector.x, aMaxVector.x);
            resultVector.y = UnityEngine.Random.Range(aMinVector.y, aMaxVector.y);
            resultVector.z = UnityEngine.Random.Range(aMinVector.z, aMaxVector.z);

            return resultVector;
                
        }

        public void ExplodeIntoObjects(GameObject aBaseGameObject, VoxelContainer aVoxelContainer, Texture2D aTexture, float aVoxelSize, float aSpaceBetween, bool aAddPhysics, bool aHideChildObject, ExplodeRandomFactors aRandomFactors)
        {
            if (aVoxelSize <= 0f)
            {
                EditorUtility.DisplayDialog("Error", "Voxel size has to be greater then zero.", "Ok");
                return;
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayCancelableProgressBar("Export separeted voxels", "Exporting", 0);

            List<Color> usedColors = aVoxelContainer.GetOrderedColorPalette();
            Dictionary<Color, Material> materialList = new Dictionary<Color, Material>();            
            try
            {
                string materialFolder = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.explodedModels + "/" + this.voxelContainer.name;
                StaticValues.CheckAndCreateFolders();
                if (!System.IO.Directory.Exists(materialFolder))
                    AssetDatabase.CreateFolder("Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.explodedModels, this.voxelContainer.name);

                for (int i = 0; i < usedColors.Count; i++)
                {
                    Color curColor = usedColors[i];
                                                  
                    Material newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.color = curColor;
                    materialList.Add(curColor, newMaterial);
                    
                    AssetDatabase.CreateAsset(newMaterial,  materialFolder + "/material_" + i + ".mat");                    
                }

                GameObject parentGameObject = new GameObject();
                parentGameObject.name  = aVoxelContainer.gameObject.name + "_Separeted";

                //Material newMaterial = new Material("Standard");
                int stepCounter = 0;
                foreach (Voxel curVoxel in aVoxelContainer.voxels.Values)
                {
                    GameObject voxelObject;
                    if (aBaseGameObject == null)
                        voxelObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    else
                        voxelObject = Instantiate(aBaseGameObject);

                    voxelObject.name = parentGameObject.name + "_Voxel";                    
                    voxelObject.transform.parent = parentGameObject.transform;
                    voxelObject.transform.position = curVoxel.position+(curVoxel.position * aSpaceBetween) + RandomVector(aRandomFactors.MinPosition, aRandomFactors.MaxPosition);
                    voxelObject.transform.localScale = RandomVector(aRandomFactors.MinScale, aRandomFactors.MaxScale);
                    voxelObject.transform.eulerAngles = RandomVector(aRandomFactors.MinRotation, aRandomFactors.MaxRotation);
                    voxelObject.GetComponent<Renderer>().sharedMaterial.SetColor(0, curVoxel.color);

                    if (aHideChildObject)
                        voxelObject.hideFlags = HideFlags.HideInHierarchy;                   
                    voxelObject.GetComponent<Renderer>().sharedMaterial = materialList[curVoxel.color];
                    if (aAddPhysics)
                    {                     
                        if (voxelObject.GetComponent<Collider>() == null)
                            voxelObject.AddComponent<BoxCollider>();
                        if (voxelObject.GetComponent<Rigidbody>() == null)
                            voxelObject.AddComponent<Rigidbody>();
                    }
                                        
                    stepCounter++;
                    if (EditorUtility.DisplayCancelableProgressBar("Export separeted voxels", "Exporting", aVoxelContainer.voxels.Count / stepCounter))
                        return;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private GameObject BuildVoxelCube(Voxel aVoxel, Texture2D aTexture)
        {
            GameObject result = new GameObject();
            result.AddComponent<MeshRenderer>();

            MeshFilter curMeshFilter = result.GetComponent<MeshFilter>();
            if (curMeshFilter == null)
                curMeshFilter = result.AddComponent<MeshFilter>();
            Mesh curMesh = new Mesh();
            curMeshFilter.mesh = curMesh;

            List<Vector3> vertices;
            List<int> triangles;
            List<Vector2> uvs;
            if (curMesh.vertexCount > 0)
            {
                vertices = new List<Vector3>(curMesh.vertices);
                triangles = new List<int>(curMesh.triangles);
                uvs = new List<Vector2>(curMesh.uv);
            }else
            {
                vertices = new List<Vector3>();
                triangles = new List<int>();
                uvs = new List<Vector2>();
            }

            VoxelContainer tempContainer = new VoxelContainer();//:(
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.down / 2f), Vector3.left, Vector3.back);
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.up / 2f), Vector3.right, Vector3.back);
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.left / 2f), Vector3.up, Vector3.back);
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.right / 2f), Vector3.down, Vector3.back);
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.forward / 2f), Vector3.right, Vector3.up);
            tempContainer.BuildPolygonsForFace(aTexture, aVoxel.color, vertices, triangles, uvs, aVoxel.position + (Vector3.back / 2f), Vector3.right, Vector3.down);
            tempContainer = null;

            curMesh.vertices = vertices.ToArray();
            curMesh.triangles = triangles.ToArray();
            curMesh.uv = uvs.ToArray();
            
            curMesh.RecalculateNormals();
            return result;
        }

        private void OnUndoRedoPerformed()
        {
            if (this.voxelContainer != null)
            {                
                this.voxelContainer.WaitForSerialization();
                this.voxelContainer.ClearUVDictionary();
                this.voxelContainer.SetAllAreaChanged();
                this.voxelContainer.UpdateAllNeighborList();
                this.voxelContainer.BuildMesh(false, true, true, true);
                EditorUtility.SetDirty(target);
            }
        }
        #endregion
    }
}