using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LaserSailsModules
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class LaserUI : MonoBehaviour
    {
        private static GameObject panelPrefab;

        //Constants
        public static float maxSecondsToBuy = 10; //Max Seconds you can buy at once
        public static float costPerSecond = 15000; //Cost of buying 1 second
        public static double forcePerGenerator = 1750.0; //Force in N per generator
        public static int[] generatorsAtKSC = { 1, 2, 4 }; //Number of generators at the KSC by upgrade
        public static double shapeConstant = 4; //Constant used in force calculations that is dependant of shape. 2 refers to square sails and PI refers to circular sails
        public static Color disabledColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        public static double focusAspectRatio = 0.0000141421362; //How wide a laser beam is (1 meter) at a distance (50 kilometers)

        public static GameObject PanelPrefab
        {
            get { return panelPrefab; }
        }

        //UI element and check
        public static GameObject UI = null;
        public static bool isLoaded = false;

        //UI Objects References
        public static GameObject m_toggleSails = null; 
        public static GameObject m_toggleRun = null;
        public static GameObject m_buttonKerbin = null;
        public static GameObject m_buttonVessel = null;
        public static GameObject m_buttonExit = null;
        public static GameObject m_textBuy = null;
        public static GameObject m_textSeconds = null;
        public static GameObject m_textStatus = null;
        public static GameObject m_sliderBuy = null;
        public static GameObject m_toggleBuy = null;

        public static GameObject m_sliderGee = null;
        public static GameObject m_textGee = null;
        public static GameObject m_textMaxGee = null;
        public static GameObject m_toggleLeft = null;
        public static GameObject m_toggleRight = null;
        public static GameObject m_spaceVesselName = null;
        public static GameObject m_textVessel = null;
        public static GameObject m_textForce = null;

        //Variables
        public static float secondsToBuy = 0;
        public static int vesselSelection = 0; //if -1, vesselmodule will search for previous vessel. If +1, vesselmodule will search for next vessel
        public static float maxGee = 1;
        public static bool isModeKerbin = true;
        public static int kscLevel;

        private void Awake()
        {
            GameEvents.onGameSceneSwitchRequested.Add(OnSceneChange);

            string path = KSPUtil.ApplicationRootPath + "GameData/OtherWorldsReboot/Parts/UnityAssets";

            AssetBundle prefabs = AssetBundle.LoadFromFile(path + "/laserUI_bundle.dat");

            panelPrefab = prefabs.LoadAsset("LSM_Panel") as GameObject;

            Debug.Log("[Sprite] Number of childs on Laser UI: " + panelPrefab.transform.childCount);

            ReadConfig();
        }

        void OnSceneChange(GameEvents.FromToAction<GameScenes, GameScenes> fromToScenes)
        {
            if (UI != null)
            {
                DestroyUI();
            }
        }

        public static void ReadConfig()
        {
            UrlDir.UrlConfig cfg = GameDatabase.Instance.GetConfigs("LaserSailConfiguration")[0];
            if (cfg != null)
            {
                Debug.Log("[Laser Manager] Configuration file for Laser Variables was found");

                if (cfg.config.GetValue("maxSecondsToBuy") != null) maxSecondsToBuy = Single.Parse(cfg.config.GetValue("maxSecondsToBuy"));
                if (cfg.config.GetValue("costPerSecond") != null) costPerSecond = Single.Parse(cfg.config.GetValue("costPerSecond"));
                if (cfg.config.GetValue("forcePerGenerator") != null) forcePerGenerator = Single.Parse(cfg.config.GetValue("forcePerGenerator"));
                if (cfg.config.GetValue("generatorsAtKSC_level1") != null) generatorsAtKSC[0] = int.Parse(cfg.config.GetValue("generatorsAtKSC_level1"));
                if (cfg.config.GetValue("generatorsAtKSC_level2") != null) generatorsAtKSC[1] = int.Parse(cfg.config.GetValue("generatorsAtKSC_level2"));
                if (cfg.config.GetValue("generatorsAtKSC_level3") != null) generatorsAtKSC[2] = int.Parse(cfg.config.GetValue("generatorsAtKSC_level3"));
                if (cfg.config.GetValue("shapeConstant") != null) shapeConstant = Single.Parse(cfg.config.GetValue("shapeConstant"));
            }
            else
            {
                Debug.Log("[Laser Manager] Configuration file for Laser Variables couldn't be found, using hardcoded ones");
            }
        }

        public static void UpdateRunButton(bool turnOn)
        {
            if (turnOn)
            {
                m_toggleRun.GetComponent<Toggle>().interactable = true;
            }
            else
            {
                m_toggleRun.GetComponent<Toggle>().isOn = false;
                m_toggleRun.GetComponent<Toggle>().interactable = false;
            }
        }

        public static void PostBuyEvent()
        {
            m_sliderBuy.GetComponent<Slider>().value = 0;
            m_toggleBuy.GetComponent<Toggle>().isOn = false;
        }

        public static void SwitchMode(bool isKerbin)
        {
            if(isLoaded)
            {
                isModeKerbin = isKerbin;

                //Updating Interactability of Buttons
                m_sliderBuy.GetComponent<Slider>().interactable = isKerbin;
                m_toggleBuy.GetComponent<Toggle>().interactable = isKerbin;
                m_toggleLeft.GetComponent<Toggle>().interactable = !isKerbin;
                m_toggleRight.GetComponent<Toggle>().interactable = !isKerbin;

                //Updating Texts Colors
                Color kColor; Color vColor;
                if (isKerbin) { kColor = Color.white; vColor = disabledColor; }
                else { vColor = Color.white; kColor = disabledColor; }
                m_textBuy.GetComponent<Text>().color = kColor;
                m_textSeconds.GetComponent<Text>().color = kColor;
                m_textVessel.GetComponent<Text>().color = vColor;
                m_textForce.GetComponent<Text>().color = vColor;
                m_spaceVesselName.GetComponent<Image>().color = vColor;
            }
        }

        public static void UpdateMaxGee()
        {
            if(isLoaded)
            {
                maxGee = Mathf.Pow(10, m_sliderGee.GetComponent<Slider>().value);
                if (maxGee >= 1) maxGee = Mathf.Round(maxGee);
                else maxGee = Mathf.Round(maxGee * 100f) / 100f;

                m_textMaxGee.GetComponent<Text>().text = maxGee + " g";
            }
        }

        public static void UpdateText_Buy() { m_textBuy.GetComponent<Text>().text = "+ " + (Mathf.Round(m_sliderBuy.GetComponent<Slider>().value * maxSecondsToBuy * 10) / 10) + "s   - " + (Mathf.Round(m_sliderBuy.GetComponent<Slider>().value * maxSecondsToBuy*10)/ 10) * costPerSecond + " \\F"; }
        public static void UpdateText_Seconds(float input) { m_textSeconds.GetComponent<Text>().text = "Seconds left of flight: " + input + "s"; }
        public static void UpdateText_Status(string input) { m_textStatus.GetComponent<Text>().text = input; }
        public static void UpdateText_Gee(float input) { m_textGee.GetComponent<Text>().text = (Math.Floor(input * 1000) / 1000) + " g"; }
        public static void UpdateText_Vessel(string input) { m_textVessel.GetComponent<Text>().text = input; }
        public static void UpdateText_Force(float input) { m_textForce.GetComponent<Text>().text = (Math.Floor((input/forcePerGenerator)*100))/100 + " Gen"; }

        public static void LoadUI()
        {
            kscLevel = (int)PSystemSetup.Instance.SpaceCenterFacilities.First(f => f.facilityName == "TrackingStation").GetFacilityLevel() * 2;

            if (UI == null)
            {
                if (PanelPrefab != null)
                {
                    UI = Instantiate(PanelPrefab); //Instantiation of UI element

                    //Buttons and things
                    m_toggleSails = (GameObject)UI.transform.Find("toggleSails").gameObject;
                    m_toggleRun = (GameObject)UI.transform.Find("toggleRun").gameObject;
                    m_buttonKerbin = (GameObject)UI.transform.Find("buttonKerbin").gameObject;
                    m_buttonVessel = (GameObject)UI.transform.Find("buttonVessel").gameObject;
                    m_sliderBuy = (GameObject)UI.transform.Find("sliderBuy").gameObject;
                    m_toggleBuy = (GameObject)UI.transform.Find("toggleBuy").gameObject;
                    m_buttonExit = (GameObject)UI.transform.Find("buttonExit").gameObject;
                    m_toggleLeft = (GameObject)UI.transform.Find("toggleLeft").gameObject;
                    m_toggleRight = (GameObject)UI.transform.Find("toggleRight").gameObject;
                    m_sliderGee = (GameObject)UI.transform.Find("sliderGee").gameObject;
                    m_spaceVesselName = (GameObject)UI.transform.Find("spaceVesselName").gameObject;

                    //Text objects
                    m_textBuy = (GameObject)UI.transform.Find("textBuy").gameObject;
                    m_textSeconds = (GameObject)UI.transform.Find("textSeconds").gameObject;
                    m_textStatus = (GameObject)UI.transform.Find("textStatus").gameObject;
                    m_textVessel = (GameObject)UI.transform.Find("textVessel").gameObject;
                    m_textForce = (GameObject)UI.transform.Find("textForce").gameObject;
                    m_textGee = (GameObject)UI.transform.Find("textGee").gameObject;
                    m_textMaxGee = (GameObject)UI.transform.Find("textMaxGee").gameObject;

                    //Listeners
                    m_toggleSails.GetComponent<Toggle>().onValueChanged.AddListener(delegate { UpdateRunButton(m_toggleSails.GetComponent<Toggle>().isOn); });
                    m_buttonKerbin.GetComponent<Button>().onClick.AddListener(delegate { SwitchMode(true);  } );
                    m_buttonVessel.GetComponent<Button>().onClick.AddListener(delegate { SwitchMode(false); } );
                    m_sliderBuy.GetComponent<Slider>().onValueChanged.AddListener(delegate { UpdateText_Buy(); });
                    m_buttonExit.GetComponent<Button>().onClick.AddListener(delegate { DestroyUI(); });
                    m_sliderGee.GetComponent<Slider>().onValueChanged.AddListener(delegate { UpdateMaxGee(); });
                    m_toggleRight.GetComponent<Toggle>().onValueChanged.AddListener(delegate { vesselSelection = +1; });
                    m_toggleLeft.GetComponent<Toggle>().onValueChanged.AddListener(delegate { vesselSelection = -1; } );

                    ResetUI();

                    UI.transform.SetParent(MainCanvasUtil.MainCanvas.transform);

                    isLoaded = true;

                    Debug.Log("[Lasers Manager] Succesfuly opened UI");
                }
                else
                {
                    Debug.Log("[Lasers Manager] The UI couldn't be loaded. (Couldn't instantiate PanelPrefab)");
                }
            }
            else
                Debug.Log("[Lasers Manager] LoadUI() was called but UI is already loaded");

        }

        public static void DestroyUI()
        {
            UI.DestroyGameObject();
            UI = null;
            isLoaded = false;
            Debug.Log("[Lasers Manager] Succesfuly destroyed UI");
        }

        public static void ToggleUI()
        {
            if (isLoaded)
                DestroyUI();
            else
                LoadUI();
        }

        public static void ResetUI()
        {
            UpdateRunButton(false);
            UI.AddComponent<LaserUI_Drag>(); //Add Drag MonoBehaivour to LaserUI

            //Setup of objects' propieties
            m_toggleSails.GetComponent<Toggle>().isOn = false;
            m_sliderBuy.GetComponent<Slider>().value = 0;
            m_toggleBuy.GetComponent<Toggle>().isOn = false;
            m_textGee.GetComponent<Text>().text = "0 g";
            m_textMaxGee.GetComponent<Text>().text = "1 g";
            
            //Vessel Button Specific
            m_toggleLeft.GetComponent<Toggle>().interactable = false;
            m_toggleRight.GetComponent<Toggle>().interactable = false;
            m_textVessel.GetComponent<Text>().color = disabledColor;
            m_textForce.GetComponent<Text>().color = disabledColor;
            m_spaceVesselName.GetComponent<Image>().color = disabledColor;

            //Methods calling with special update requirements
            UpdateText_Status("Status: Sails Stowed");
            UpdateText_Seconds(0);

            maxGee = 1;
            secondsToBuy = 0;
            isModeKerbin = true;
        }
    }
    
    class LaserUI_Drag : MonoBehaviour, IDragHandler
    {
        /*
         * Basic MonoBehaivour to be attached to a UI to add drag captabilities 
         * Made by Niako the Duck, but use it if you want
         */
        public void OnDrag(PointerEventData data)
        {
            this.transform.position += new Vector3(Mouse.delta.x, -Mouse.delta.y, 0);
        }
    }
}
