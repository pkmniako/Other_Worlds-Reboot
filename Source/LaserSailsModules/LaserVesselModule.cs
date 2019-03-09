using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LaserSailsModules
{
    public class LaserVesselModule : VesselModule
    {
        [KSPField(isPersistant = true)]
        public bool isGenerator;
        
        [KSPField(isPersistant = true)]
        public float totalForce;

        [KSPField(isPersistant = true)]
        public int sailNumber;

        [KSPField(isPersistant = true)]
        public float sailArea;

        public bool isCurrentVessel = false;
        public bool deployed = false;
        public bool running = false;
        public bool uiOpened = false;

        public Vessel sourceVessel = null;
        public int currentSourceVessel = -1;
        public bool hasSourceVessel = false;

        public override void OnLoadVessel()
        {
            List<LaserSailModule> sm = vessel.FindPartModulesImplementing<LaserSailModule>();
            List<LaserGeneratorModule> gm = vessel.FindPartModulesImplementing<LaserGeneratorModule>();

            if(sm.Count > 0 || gm.Count > 0)
            {
                resetValues();
            }
        }

        public override void OnUnloadVessel()
        {
            List<LaserSailModule> sm = vessel.FindPartModulesImplementing<LaserSailModule>();
            List<LaserGeneratorModule> gm = vessel.FindPartModulesImplementing<LaserGeneratorModule>();

            if (sm.Count > 0 || gm.Count > 0)
                resetValues();

            if (LaserUI.isLoaded && isCurrentVessel)
                LaserUI.DestroyUI();
        }

        public void resetValues()
        {
            isGenerator = false;
            sailNumber = 0;
            sailArea = 0;
            isCurrentVessel = false;
            totalForce = 0;

            List<LaserSailModule> Sail_modules = vessel.FindPartModulesImplementing<LaserSailModule>();
            if (Sail_modules.Count != 0)
            {
                sailNumber = Sail_modules.Count + 0;
                foreach (LaserSailModule m in Sail_modules)
                {
                    sailArea += m.area;
                }
            }

            List<LaserGeneratorModule> Gen_modules = vessel.FindPartModulesImplementing<LaserGeneratorModule>();
            if (Gen_modules.Count != 0)
            {
                isGenerator = true;
                foreach (LaserGeneratorModule m in Gen_modules)
                {
                    totalForce += m.laserForce;
                }
            }
        }

        //Updating UI and Part Modules based on UI
        void FixedUpdate()
        {
            if(vessel == FlightGlobals.ActiveVessel)
            {
                if(LaserUI.isLoaded)
                {
                    //Buy process
                    if(LaserUI.m_toggleBuy.GetComponent<Toggle>().isOn)
                    {
                        List<LaserSailModule> modules = vessel.FindPartModulesImplementing<LaserSailModule>();
                        float s = LaserUI.m_sliderBuy.GetComponent<Slider>().value;
                        float min = -1;

                        foreach (LaserSailModule m in modules)
                        {
                            m.secBought += (Mathf.Floor((s*LaserUI.maxSecondsToBuy)*10))/10;
                            if (min < 0 || m.secBought < min)
                                min = (float)m.secBought;
                        }

                        LaserUI.PostBuyEvent();
                        LaserUI.UpdateText_Buy();
                        LaserUI.UpdateText_Seconds(min);
                    }

                    //Search for first craft with generator module on opening the UI
                    if (uiOpened == false)
                    {
                        SearchVessel(1); // if firstStart is 1, this will avoid ksp Crashing due to trying to search vessel -1
                    }

                    //Updating Seconds Text
                    if (running || uiOpened == false)
                    {
                        List<LaserSailModule> modules = vessel.FindPartModulesImplementing<LaserSailModule>();
                        float min = -1;

                        foreach (LaserSailModule m in modules)
                        {
                            if (min < 0 || m.secBought < min)
                                min = (float)m.secBought;
                        }
                        LaserUI.UpdateText_Seconds(min);
                        uiOpened = true;
                    }

                    //Toggle - Run functionality
                    if (LaserUI.m_toggleSails.GetComponent<Toggle>().isOn != deployed)
                    {
                        List<LaserSailModule> modules = vessel.FindPartModulesImplementing<LaserSailModule>();

                        if (LaserUI.m_toggleSails.GetComponent<Toggle>().isOn)
                        {
                            foreach (LaserSailModule m in modules)
                            {
                                if (!m.isDeployed)
                                    m.DeployLaserSail();
                            }
                            Debug.Log("[Laser Manager] Deployed " + modules.Count + " un-deployed sails");
                            LaserUI.UpdateText_Status("Status: Ready to run lasers");
                        }
                        else
                        {
                            foreach (LaserSailModule m in modules)
                            {
                                if (m.isDeployed)
                                    m.RetractLaserSail();
                            }
                            Debug.Log("[Laser Manager] Stowed " + modules.Count + " deployed sails");
                            LaserUI.UpdateText_Status("Status: Sails Stowed");
                        }
                        deployed = LaserUI.m_toggleSails.GetComponent<Toggle>().isOn;
                    }

                    else if (LaserUI.m_toggleRun.GetComponent<Toggle>().isOn != running)
                    {
                        List<LaserSailModule> modules = vessel.FindPartModulesImplementing<LaserSailModule>();

                        if (LaserUI.m_toggleRun.GetComponent<Toggle>().isOn)
                        {
                            foreach (LaserSailModule m in modules)
                            {
                                if (m.isDeployed)
                                    m.RunLasers();
                            }
                            Debug.Log("[Laser Manager] Running " + modules.Count + " deployed sails");
                            LaserUI.UpdateText_Status("Status: Propulsed Flight");
                        }
                        else
                        {
                            foreach (LaserSailModule m in modules)
                            {
                                if (m.isOn)
                                    m.StopLasers();
                            }
                            Debug.Log("[Laser Manager] Turning off " + modules.Count + " running sails");
                            LaserUI.UpdateText_Status("Status: Ready to run lasers");
                        }
                        running = LaserUI.m_toggleRun.GetComponent<Toggle>().isOn;
                    }

                    //Vessel Search Functionality
                    if(LaserUI.vesselSelection != 0)
                    {
                        SearchVessel(0);
                    }

                    if (sourceVessel == null)
                    {
                        hasSourceVessel = false;
                        currentSourceVessel = -1;
                    }
                }

                if(!LaserUI.isLoaded && uiOpened)
                {
                    running = false;
                    deployed = false;
                    uiOpened = false;
                    List<LaserSailModule> modules = vessel.FindPartModulesImplementing<LaserSailModule>();
                    foreach (LaserSailModule m in modules)
                    {
                        if (m.isOn)
                            m.StopLasers();
                    }
                    Debug.Log("[Laser Manager] UI is closed... Turning off all sails!");
                }
            }
        }

        public void SearchVessel(int firstStart)
        {
            Debug.Log("[Laser Manager] Searching for a vessel with a generator...");
            currentSourceVessel += firstStart;
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                currentSourceVessel += LaserUI.vesselSelection;
                if (currentSourceVessel == FlightGlobals.Vessels.Count) { currentSourceVessel = 0; }
                else if (currentSourceVessel == -1) { currentSourceVessel += FlightGlobals.Vessels.Count; }

                Vessel v = FlightGlobals.Vessels[currentSourceVessel];
                if (v != null)
                {
                    if (v.GetComponent<LaserVesselModule>().isGenerator && !v.isActiveVessel)
                    {
                        sourceVessel = v;
                        Debug.Log("[Laser Manager] Found vessel " + v.name);
                        hasSourceVessel = true;
                        LaserUI.UpdateText_Vessel(sourceVessel.name);
                        LaserUI.UpdateText_Force(sourceVessel.GetComponent<LaserVesselModule>().totalForce);
                        LaserUI.vesselSelection = 0;
                        return;
                    }
                }
            }
            Debug.Log("[Laser Manager] No vessels with a generator were found");
            currentSourceVessel += LaserUI.vesselSelection - firstStart;
        }
    }
}
