using System;
using UnityEngine;
using KSP.UI.Screens;
using KSP.IO;

using ClickThroughFix;
using ToolbarControl_NS;

using System.Collections;
using System.Collections.Generic;

namespace SettingsMaster
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class SettingsMaster : MonoBehaviour
    {
        ToolbarControl toolbarControl;
        bool GUIEnabled;
        const int GUIid = 1535387;
        Rect WindowRect;
        GUIStyle scrollbar_style = new GUIStyle(HighLogic.Skin.scrollView);

        int scrollBarHeight = Math.Min(1000, Screen.height - 200);

        GUIStyle buttonEnabled;
        GUIStyle buttonDisabled;
        GUIStyle selectedButtonStyle;
        bool buttonsInitted = false;

        List<Type> savedParameterTypes = null;
        //bool nodesHidden = false;
        bool useblizzy = false;
        Vector2 scrollVector;


        List<string> whitelist = new List<string>()
        {
            "AdvancedParams",
            "MissionParamsFacilities",
            "MissionParamsExtras",
            "MissionParamsGeneral"
        };

        public class Node
        {
            public Type node;
            public bool enabled;
            public string sectionName;

            public Node(Type t, string n)
            {
                node = t;
                enabled = true;
                sectionName = n;
            }
        }
        public class Section
        {
            public string sectionName;
            public bool enabled;
            public Section(string n)
            {
                sectionName = n;
                enabled = true;
            }
        }
        
        static List<Node> nodeList = null;
        static Dictionary<string, Section> sectionList = null;
        static int activeSettings = 0;

        const int MAXSETTINGS = 16;

        bool pauseFlag = false;
        private MiniSettings _miniSettings;
        GameObject miniSettings;

        public void Start()
        {
            CountCustomParameterNodes();
            if (activeSettings <= MAXSETTINGS)
            {
                Destroy(this);
            }

            scrollbar_style.padding = new RectOffset(3, 3, 3, 3);
            scrollbar_style.border = new RectOffset(3, 3, 3, 3);
            scrollbar_style.margin = new RectOffset(1, 1, 1, 1);
            scrollbar_style.overflow = new RectOffset(1, 1, 1, 1);

            double WindowX = (Screen.width - 400)/2;
            double WindowY = (Screen.height - (scrollBarHeight +110))/2;

            WindowRect = new Rect((float)WindowX, (float)WindowY, 400, scrollBarHeight + 110);

            GameEvents.onGamePause.Add(onGamePause);
            GameEvents.onGameUnpause.Add(onGameUnPause);
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);

            CheckActiveSettings();

            OnGUIAppLauncherReady();
            DontDestroyOnLoad(this);
        }

        public void OnDestroy()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
            GameEvents.onGamePause.Remove(onGamePause);
            GameEvents.onGameUnpause.Remove(onGameUnPause);
            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
        }

        void OnGUIAppLauncherReady()
        {

            toolbarControl = gameObject.AddComponent<ToolbarControl>();
            toolbarControl.AddToAllToolbars(ToggleGUI, ToggleGUI,
                ApplicationLauncher.AppScenes.SPACECENTER |
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW ,
                "ParameterTypes_NS",
                "parameterTypesButton",
                "ParameterTypes/PluginData/Textures/Gear_38",
                "ParameterTypes/PluginData/Textures/Gear_24",
                "ParameterTypes"
            );
            toolbarControl.UseBlizzy(useblizzy);
        }

        void ToggleGUI()
        {
            GUIEnabled = !GUIEnabled;
        }

        void CountCustomParameterNodes()
        {
            nodeList = new List<Node>();
            sectionList = new Dictionary<string, Section>();
            foreach (var pt in GameParameters.ParameterTypes)
            {
                if (pt.IsSubclassOf(typeof(GameParameters.CustomParameterNode)))
                {
                    if (!pt.IsAbstract)
                    {
                        if (!whitelist.Contains(pt.Name))
                        {
                            Type type = pt;
                            GameParameters.CustomParameterNode cpn = (GameParameters.CustomParameterNode)Activator.CreateInstance(type);
                            nodeList.Add(new Node(pt, cpn.Section));
                            if (!sectionList.ContainsKey(cpn.Section))
                                sectionList.Add(cpn.Section, new Section(cpn.Section));
                        }
                    }
                }
            }
            activeSettings = 0;
            foreach (var s in sectionList)
                if (s.Value.enabled)
                    activeSettings++;
        }

        void UpdateButton()
        {
            if (toolbarControl != null)
            {
                if (!buttonsInitted)
                {
                    buttonEnabled = new GUIStyle(GUI.skin.button);
                    buttonDisabled = new GUIStyle(GUI.skin.button);

                    buttonEnabled.normal.textColor = Color.green;
                    buttonDisabled.normal.textColor = Color.red;
                    buttonsInitted = true;
                }

                toolbarControl.UseBlizzy(useblizzy);

                if (activeSettings > MAXSETTINGS)
                    toolbarControl.SetTexture("ParameterTypes/PluginData/Textures/Gear_38",
                    "ParameterTypes/PluginData/Textures/Gear_24");
                else
                    toolbarControl.SetTexture("ParameterTypes/PluginData/Textures/Gear_Green_38",
                    "ParameterTypes/PluginData/Textures/Gear_Green_24");
            }
        }

        void onGameSceneLoadRequested(GameScenes gs)
        {
            GUIEnabled = false;
        }

        void onGamePause()
        {
            pauseFlag = true;
        }

        void onGameUnPause()
        {
            pauseFlag = false;
        }

        void CheckActiveSettings()
        {
            Debug.Log("CheckActiveSettings, sectionList.Count: " + sectionList.Count);
            if (activeSettings > MAXSETTINGS)
            {
                int cnt = 0;
                foreach (var s in sectionList)
                {
                    if (s.Value.enabled)
                    {
                        s.Value.enabled = false;
                        foreach (var pt in nodeList)
                            if (pt.sectionName == s.Key)
                            {
                                pt.enabled = false;
                            }
                        cnt++;
                        if (activeSettings - cnt <= MAXSETTINGS)
                            break;
                    }
                }
                HideSelectedNodes();
                activeSettings -= cnt;
                ScreenMessages.PostScreenMessage("Too many Settings pages, " + cnt + " pages disabled!!", 10.0f, ScreenMessageStyle.UPPER_CENTER);
                Debug.Log("Too many Settings pages, " + cnt + " pages disabled!!");
            }
        }

        public void OnGUI()
        {
            if (pauseFlag)
            {
                miniSettings =
                    GameObject.Find("Mini Settings Dialog");
                if (miniSettings != null)
                {
                    _miniSettings = miniSettings.GetComponent<MiniSettings>();

                    if (_miniSettings != null)
                    {
                        CheckActiveSettings();
                    }
                }
            }
            if (HighLogic.LoadedScene == GameScenes.MAINMENU)
            {
                CheckActiveSettings();
            }
            UpdateButton();

            if (!GUIEnabled)
                return;
   
            WindowRect = ClickThruBlocker.GUILayoutWindow(GUIid, WindowRect, DoWindow, "Settings Master");
        }

        void HideSelectedNodes()
        {
            if (savedParameterTypes ==  null)
                savedParameterTypes = new List<Type>(GameParameters.ParameterTypes);
            GameParameters.ParameterTypes = new List<Type>(savedParameterTypes);
            foreach (var pt in nodeList)
                if (!pt.enabled)
                    GameParameters.ParameterTypes.Remove(pt.node);
            //nodesHidden = true;
        }

        void RestoreNodes()
        {
            GameParameters.ParameterTypes = new List<Type>(savedParameterTypes);
            //nodesHidden = false;
            CountCustomParameterNodes();
        }

        void DoWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Settings pages Count:");
            GUILayout.FlexibleSpace();
            GUILayout.Label(nodeList.Count.ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide All"))
            {
                foreach (var pt in nodeList)
                    pt.enabled = false;
                foreach (var s in sectionList)
                    s.Value.enabled = false;
                activeSettings = 0;
                HideSelectedNodes();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Show All"))
            {
                foreach (var pt in nodeList)
                    pt.enabled = true;
                foreach (var s in sectionList)
                    s.Value.enabled = true;
                activeSettings = sectionList.Count;
                HideSelectedNodes();
            }
            GUILayout.EndHorizontal();

            Debug.Log("Begin scrollview");
            GUILayout.BeginHorizontal();
            scrollVector = GUILayout.BeginScrollView(scrollVector, scrollbar_style, GUILayout.Height(scrollBarHeight));
            foreach (var s in sectionList)
            {
                GUILayout.BeginHorizontal();
                if (s.Value.enabled)
                    selectedButtonStyle = buttonEnabled;
                else
                    selectedButtonStyle = buttonDisabled;
                if (GUILayout.Button(s.Key, selectedButtonStyle))
                {

                    s.Value.enabled = !s.Value.enabled;
                    foreach (var pt in nodeList)
                        if (pt.sectionName == s.Key)
                        {
                            pt.enabled = !pt.enabled;
                        }
                    HideSelectedNodes();
                    if (s.Value.enabled)
                        activeSettings++;
                    else
                        activeSettings--;
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();


#if false
            GUILayout.BeginHorizontal();

            if (nodesHidden)
            {
                if (GUILayout.Button("Restore nodes"))
                    RestoreNodes();

            }
            else
            {
                if (GUILayout.Button("Hide nodes"))
                {
                    HideSelectedNodes();
                }
            }
            GUILayout.EndHorizontal();
#endif

            GUILayout.BeginHorizontal();
            useblizzy = GUILayout.Toggle(useblizzy, "Use Blizzy Toolbar");
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }


    }
}

