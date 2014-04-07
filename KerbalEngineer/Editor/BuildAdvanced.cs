﻿// Project:	KerbalEngineer
// Author:	CYBUTEK
// License:	Attribution-NonCommercial-ShareAlike 3.0 Unported

#region Using Directives

using System.Linq;

using KerbalEngineer.Extensions;
using KerbalEngineer.Settings;
using KerbalEngineer.Simulation;

using UnityEngine;

#endregion

namespace KerbalEngineer.Editor
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class BuildAdvanced : MonoBehaviour
    {
        #region Instance

        /// <summary>
        ///     Gets the current instance if started or returns null.
        /// </summary>
        public static BuildAdvanced Instance { get; private set; }

        #endregion

        #region Fields

        private readonly int windowId = EngineerGlobals.GetNextWindowId();

        private bool hasChanged;
        private bool isEditorLocked;
        private int numberOfStages;
        private Rect windowPosition = new Rect(265.0f, 45.0f, 0, 0);

        #region Styles

        private GUIStyle areaBodiesStyle;
        private GUIStyle areaStyle;
        private GUIStyle buttonStyle;
        private GUIStyle infoStyle;
        private GUIStyle titleStyle;
        private GUIStyle windowStyle;

        #endregion

        #endregion

        #region Properties

        private bool compactMode;
        private bool showAllStages;
        private bool showReferenceBodies;
        private bool useAtmosphericDetails;
        private bool visible;

        /// <summary>
        ///     Gets and sets whether the display is enabled.
        /// </summary>
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        /// <summary>
        ///     Gets and sets whether to show in compact mode.
        /// </summary>
        public bool CompactMode
        {
            get { return this.compactMode; }
            set { this.compactMode = value; }
        }

        /// <summary>
        ///     Gets and sets whether to show all stages.
        /// </summary>
        public bool ShowAllStages
        {
            get { return this.showAllStages; }
            set { this.showAllStages = value; }
        }

        /// <summary>
        ///     Gets and sets whether to use atmospheric details.
        /// </summary>
        public bool UseAtmosphericDetails
        {
            get { return this.useAtmosphericDetails; }
            set { this.useAtmosphericDetails = value; }
        }

        /// <summary>
        ///     Gets and sets whether to show the reference body selection.
        /// </summary>
        public bool ShowReferenceBodies
        {
            get { return this.showReferenceBodies; }
            set { this.showReferenceBodies = value; }
        }

        #endregion

        #region Initialisation

        private void Awake()
        {
            Instance = this;
            this.Load();
        }

        private void Start()
        {
            this.InitialiseStyles();
            RenderingManager.AddToPostDrawQueue(0, this.OnDraw);
        }

        /// <summary>
        ///     Initialises all the styles that are required.
        /// </summary>
        private void InitialiseStyles()
        {
            this.areaBodiesStyle = new GUIStyle(HighLogic.Skin.box);

            this.windowStyle = new GUIStyle(HighLogic.Skin.window)
            {
                alignment = TextAnchor.UpperLeft
            };

            this.areaStyle = new GUIStyle(HighLogic.Skin.box)
            {
                padding = new RectOffset(0, 0, 9, 0)
            };

            this.buttonStyle = new GUIStyle(HighLogic.Skin.button)
            {
                normal =
                {
                    textColor = Color.white
                },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            this.titleStyle = new GUIStyle(HighLogic.Skin.label)
            {
                normal =
                {
                    textColor = Color.white
                },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                stretchWidth = true
            };

            this.infoStyle = new GUIStyle(HighLogic.Skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                stretchWidth = true
            };
        }

        #endregion

        #region Update and Drawing

        private void Update()
        {
            try
            {
                if (!this.visible || EditorLogic.fetch == null || EditorLogic.fetch.ship.Count <= 0)
                {
                    return;
                }

                // Configure the simulation parameters based on the selected reference body.
                SimulationManager.Instance.Gravity = CelestialBodies.Instance.SelectedBodyInfo.Gravity;
                if (this.useAtmosphericDetails)
                {
                    SimulationManager.Instance.Atmosphere = CelestialBodies.Instance.SelectedBodyInfo.Atmosphere * 0.01d;
                }
                else
                {
                    SimulationManager.Instance.Atmosphere = 0;
                }

                SimulationManager.Instance.TryStartSimulation();
            }
            catch
            {
                /* A null reference exception is thrown when checking if EditorLogic.fetch != null??? */
            }
        }

        private void OnDraw()
        {
            try
            {
                if (!this.visible || EditorLogic.fetch == null || EditorLogic.fetch.ship.Count <= 0)
                {
                    return;
                }

                SimulationManager.Instance.RequestSimulation();

                // Change the window title based on whether in compact mode or not.
                string title;
                if (!this.compactMode)
                {
                    title = "KERBAL ENGINEER REDUX " + EngineerGlobals.AssemblyVersion;
                }
                else
                {
                    title = "K.E.R. " + EngineerGlobals.AssemblyVersion;
                }

                // Reset the window size when the staging or something else has changed.
                int stageCount = SimulationManager.Instance.Stages.Count(stage => this.showAllStages || stage.DeltaV > 0);
                if (this.hasChanged || stageCount != this.numberOfStages)
                {
                    this.hasChanged = false;
                    this.numberOfStages = stageCount;

                    this.windowPosition.width = 0;
                    this.windowPosition.height = 0;
                }

                this.windowPosition = GUILayout.Window(this.windowId, this.windowPosition, this.Window, title, this.windowStyle).ClampToScreen();

                // Check editor lock to manage click-through.
                this.CheckEditorLock();
            }
            catch
            {
                /* A null reference exception is thrown when checking if EditorLogic.fetch != null??? */
            }
        }

        /// <summary>
        ///     Checks whether the editor should be locked to stop click-through.
        /// </summary>
        private void CheckEditorLock()
        {
            if (this.windowPosition.MouseIsOver())
            {
                EditorLogic.fetch.State = EditorLogic.EditorState.GUI_SELECTED;
                this.isEditorLocked = true;
            }
            else if (!this.windowPosition.MouseIsOver() && this.isEditorLocked)
            {
                EditorLogic.fetch.State = EditorLogic.EditorState.PAD_UNSELECTED;
                this.isEditorLocked = false;
            }
        }

        /// <summary>
        ///     Draws the OnGUI window.
        /// </summary>
        private void Window(int windowId)
        {
            // Draw the compact mode toggle.
            if (GUI.Toggle(new Rect(this.windowPosition.width - 70.0f, 5.0f, 65.0f, 20.0f), this.compactMode, "COMPACT", this.buttonStyle) != this.compactMode)
            {
                this.hasChanged = true;
                this.compactMode = !this.compactMode;
            }

            // When not in compact mode draw the 'All Stages' and 'Atmospheric' toggles.
            if (!this.compactMode)
            {
                if (GUI.Toggle(new Rect(this.windowPosition.width - 153.0f, 5.0f, 80.0f, 20.0f), this.showAllStages, "ALL STAGES", this.buttonStyle) != this.showAllStages)
                {
                    this.hasChanged = true;
                    this.showAllStages = !this.showAllStages;
                }

                this.useAtmosphericDetails = GUI.Toggle(new Rect(this.windowPosition.width - 251.0f, 5.0f, 95.0f, 20.0f), this.useAtmosphericDetails, "ATMOSPHERIC", this.buttonStyle);

                if (GUI.Toggle(new Rect(this.windowPosition.width - 379.0f, 5.0f, 125.0f, 20.0f), this.showReferenceBodies, "REFERENCE BODIES", this.buttonStyle) != this.showReferenceBodies)
                {
                    this.hasChanged = true;
                    this.showReferenceBodies = !this.showReferenceBodies;
                }
            }

            // Draw the main informational display box.

            if (!this.compactMode)
            {
                GUILayout.BeginHorizontal(this.areaStyle);
                this.DrawStageNumbers();
                this.DrawPartCount();
                this.DrawCost();
                this.DrawMass();
                this.DrawIsp();
                this.DrawThrust();
                this.DrawTwr();
                this.DrawDeltaV();
                this.DrawBurnTime();
                GUILayout.EndHorizontal();

                if (this.showReferenceBodies)
                {
                    GUILayout.BeginVertical(this.areaBodiesStyle);
                    this.DrawReferenceBodies();
                    GUILayout.EndVertical();
                }
            }
            else // Draw only a few details when in compact mode.
            {
                GUILayout.BeginHorizontal(this.areaStyle);
                this.DrawStageNumbers();
                this.DrawTwr();
                this.DrawDeltaV();
                GUILayout.EndHorizontal();
            }

            GUI.DragWindow();
        }

        /// <summary>
        ///     Draws all the reference bodies.
        /// </summary>
        private void DrawReferenceBodies()
        {
            var index = 0;

            foreach (var bodyName in CelestialBodies.Instance.BodyList.Keys)
            {
                if (index % 8 == 0)
                {
                    if (index > 0)
                    {
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.BeginHorizontal();
                }
                if (GUILayout.Toggle(CelestialBodies.Instance.SelectedBodyName == bodyName, bodyName, this.buttonStyle))
                {
                    CelestialBodies.Instance.SelectedBodyName = bodyName;
                }
                index++;
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        ///     Draws the stage number column.
        /// </summary>
        private void DrawStageNumbers()
        {
            GUILayout.BeginVertical(GUILayout.Width(30.0f));
            GUILayout.Label(string.Empty, this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label("S" + stage.Number, this.titleStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the part count column.
        /// </summary>
        private void DrawPartCount()
        {
            GUILayout.BeginVertical(GUILayout.Width(50.0f));
            GUILayout.Label("PARTS", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.PartCount.ToString("N0"), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the cost column.
        /// </summary>
        private void DrawCost()
        {
            GUILayout.BeginVertical(GUILayout.Width(100.0f));
            GUILayout.Label("COST", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.Cost.ToString("N0") + " / " + stage.TotalCost.ToString("N0"), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the mass column.
        /// </summary>
        private void DrawMass()
        {
            GUILayout.BeginVertical(GUILayout.Width(100.0f));
            GUILayout.Label("MASS", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.Mass.ToMass(false) + " / " + stage.TotalMass.ToMass(), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the specific impluse column.
        /// </summary>
        private void DrawIsp()
        {
            GUILayout.BeginVertical(GUILayout.Width(50.0f));
            GUILayout.Label("ISP", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.Isp.ToString("F1") + "s", this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the thrust column.
        /// </summary>
        private void DrawThrust()
        {
            GUILayout.BeginVertical(GUILayout.Width(75.0f));
            GUILayout.Label("THRUST", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.Thrust.ToForce(), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Drwas the thrust to weight ratio column.
        /// </summary>
        private void DrawTwr()
        {
            GUILayout.BeginVertical(GUILayout.Width(50.0f));
            GUILayout.Label("TWR", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.ThrustToWeight.ToString("F2"), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the deltaV column.
        /// </summary>
        private void DrawDeltaV()
        {
            GUILayout.BeginVertical(GUILayout.Width(100.0f));
            GUILayout.Label("DELTA-V", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.DeltaV.ToString("N0") + " / " + stage.InverseTotalDeltaV.ToString("N0") + "m/s", this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        /// <summary>
        ///     Draws the burn time column.
        /// </summary>
        private void DrawBurnTime()
        {
            GUILayout.BeginVertical(GUILayout.Width(75.0f));
            GUILayout.Label("BURN", this.titleStyle);
            foreach (var stage in SimulationManager.Instance.Stages)
            {
                if (this.showAllStages || stage.DeltaV > 0)
                {
                    GUILayout.Label(stage.Time.ToTime(), this.infoStyle);
                }
            }
            GUILayout.EndVertical();
        }

        #endregion

        #region Save and Load

        /// <summary>
        ///     Saves the settings when this object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            try
            {
                var handler = new SettingHandler();
                handler.Set("visible", this.visible);
                handler.Set("windowPositionX", this.windowPosition.x);
                handler.Set("windowPositionY", this.windowPosition.y);
                handler.Set("compactMode", this.compactMode);
                handler.Set("showAllStages", this.showAllStages);
                handler.Set("useAtmosphericDetails", this.useAtmosphericDetails);
                handler.Set("showReferenceBodies", this.showReferenceBodies);
                handler.Set("selectedBodyName", CelestialBodies.Instance.SelectedBodyName);
                handler.Save("BuildAdvanced.xml");
            }
            catch
            {
                print("[KerbalEngineer]: Failed to save BuildAdvanced settings.");
            }
        }

        /// <summary>
        ///     Loads the settings when this object is created.
        /// </summary>
        private void Load()
        {
            try
            {
                var handler = SettingHandler.Load("BuildAdvanced.xml");
                handler.Get("visible", ref this.visible);
                this.windowPosition.x = handler.Get("windowPositionX", this.windowPosition.x);
                this.windowPosition.y = handler.Get("windowPositionY", this.windowPosition.y);
                handler.Get("compactMode", ref this.compactMode);
                handler.Get("showAllStages", ref this.showAllStages);
                handler.Get("useAtmosphericDetails", ref this.useAtmosphericDetails);
                CelestialBodies.Instance.SelectedBodyName = handler.Get("selectedBodyName", CelestialBodies.Instance.SelectedBodyName);
            }
            catch
            {
                print("[KerbalEngineer/BuildAdvanced]: Failed to load BuildAdvanced settings.");
            }
        }

        #endregion
    }
}