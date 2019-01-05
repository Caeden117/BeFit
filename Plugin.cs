/* BeFit | fitNessMod
 * Purpose: Count and store calories burned while playing BeatSaber
 * Date: 12/14/18
 * Origin Contributor: Viscoci
 * Contributors: --
 * */
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Collections;
using Object = UnityEngine.Object;
using CountersPlus.Custom;
using IllusionInjector;
using System.Linq;

namespace BeFitMod
{
    public class Plugin : IPlugin
    {
        public readonly Config Config = new Config(Path.Combine(Environment.CurrentDirectory, "UserData/befit.cfg"));
        private readonly WaitForSecondsRealtime _waitForSecondsRealtime = new WaitForSecondsRealtime(0.1f);
        public static Plugin Instance { get; set; }

        public string Name => "BeFitMod";
        public static string alias = "fitNessMod";
        public static string modLog = "[" + alias + " | LOG] ";
        public string Version => "v0.2.2"; //New Counting Algorithm
        bool enabled = true;
        public static bool safetyEnabled = false;
        public static Vector3 counterPosition = new Vector3(-4.25f, 0.1f, 7f);
        MenuDisplay display;
        igcv02x calCounter;

        public void OnApplicationStart()
        {
            Instance = this;
            ModPrefs.SetString(alias, "version", "v " + Version.ToString());
            Plugin.Instance.Config.sessionCalories = 0;
            Console.WriteLine(modLog + "Current Date: " + DateTime.Now.ToString("dd.MM.yyyy"));
            Console.WriteLine(modLog + Name + " " +  Version);
            if (PluginManager.Plugins.Any((IPlugin x) => x.Name == "Counters+"))
            {
                addCustomCounter();
            }
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }
        void addCustomCounter()
        {
            CustomCounter counter = new CustomCounter
            {
                JSONName = "calories", //Name in Counters+ config
                Name = "Calorie", //Name of Counter in game + in settings
                Mod = this, //Mod it was created from
                Counter = "inGameCalorieCounter", //Name of the regular spawned counter
            };
            CustomCounterCreator.CreateCustomCounter(counter);
        }

        public void OnApplicationQuit()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            calCounter = null;
            display = null;
            Config.Save();
        }
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            if (!enabled || safetyEnabled) return;  
            if (arg1.name == "GameCore") {  //Launch calories counter
                Console.WriteLine(modLog + "Scene Loaded succesfully");
                calCounter = null;
                calCounter = new GameObject("inGameCalorieCounter").AddComponent<igcv02x>();
                Console.WriteLine(modLog + "Calorie counter loaded!");
            }
            if(arg1.name == "Menu")
            {
                if (display != null) { return; }
                display = null;
                display = new GameObject("MenuDisplay").AddComponent<MenuDisplay>();
            }
            return;
        }
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "TutorialEnvironment")
            {
                safetyEnabled = true; //Disable mod
                return;
            }
            else { safetyEnabled = false; } //Enable Mod
            if (scene.name == "Menu" && !safetyEnabled) /// Settings Submenus 
            {
                SubMenus.Settings();
                SubMenus.playerProperties();
            }

        }

        public void OnLevelWasLoaded(int level)
        {
        }
        public void OnLevelWasInitialized(int level)
        {
        }
        public void OnUpdate()
        {
        }
        public void OnFixedUpdate()
        {
        }
    }
}
