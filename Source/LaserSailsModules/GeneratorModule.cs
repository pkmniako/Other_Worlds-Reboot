using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LaserSailsModules
{
    public class LaserGeneratorModule : PartModule
    {
        [KSPField(isPersistant = true)]
        public bool isOn = false; //If True, Laser is On (No shit sherlock)
        [KSPField]
        public float laserForce; //Newtons that Generates
        [KSPField]
        public String heatModel; //Name of the model for heating effects
        [KSPField]
        public float elecRequirement; //Amount of electricty per second the laser needs to run

        public float heatModelTimer = 0; //Timer for the heating effect animation (From 0 to 100)
                                         //0 = Off      100 = Max
        public float heatSpeed = 0.1f;                              

        [KSPField(guiActive = true, guiName = "Status")] //Status output
        protected string status = "";
        [KSPField(guiActive = true, guiName = "Electricity Consumption")] //Electricity Consumption output
        protected string consumption = "";


        [KSPEvent(guiActive = true, guiName = "Turn Lasers On", active = true, guiActiveEditor = false)]
        public void TurnOnLaser()
        {
            Debug.Log("[Sprite Mod] Laser Turned On");
            isOn = true;
            vessel.GetComponent<LaserVesselModule>().resetValues();
        }

        [KSPEvent(guiActive = true, guiName = "Turn Lasers Off", active = false, guiActiveEditor = false)]
        public void TurnOffLaser()
        {
            Debug.Log("[Sprite Mod] Laser Turned Off");
            isOn = false;
            vessel.GetComponent<LaserVesselModule>().resetValues();
        }

        public override void OnStart(StartState state)
        {
            Fields["status"].guiActive = true;
            Fields["consumption"].guiActive = true;

            if (isOn) heatModelTimer = 100;
            else heatModelTimer = 0;
        }

        public override void OnUpdate()
        {
            if (isOn)
            {
                status = "Generator Running";
                heatModelTimer += heatSpeed;
                consumption = Mathf.Round(elecRequirement * 1000f) / 1000f + " / " + Mathf.Round(elecRequirement * 1000f) / 1000f + " ec/s || " + heatModelTimer;
                Events["TurnOffLaser"].active = true; Events["TurnOnLaser"].active = false;
            }
            else if (!isOn)
            {
                status = "Ready to start Running";
                heatModelTimer -= heatSpeed;
                consumption = "0 / " + Mathf.Round(elecRequirement * 1000f) / 1000f + " ec/s || " + heatModelTimer;
                Events["TurnOnLaser"].active = true; Events["TurnOffLaser"].active = false;
            }

            if (heatModelTimer < 0) heatModelTimer = 0;
            else if (heatModelTimer > 100) heatModelTimer = 100;
        }
    }
}
