using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LaserSailsModules
{
    public class LaserSailModule : PartModule, IModuleInfo
    {
        //Sail Data
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;
        [KSPField(isPersistant = false)]
        public bool isOn = false;
        [KSPField(isPersistant = true)]
        public double secBought;

        [KSPField(guiName = "Laser Seconds to Buy", guiFormat = "F1", guiActive = true, isPersistant = false, guiActiveEditor = false)]
        [UI_FloatRange(minValue = 0f, maxValue = 10f, stepIncrement = 0.1f, controlEnabled = true)]
        public float timeToBuy = 0;

        [KSPField]
        public float laserForce;
        [KSPField]
        public float area;
        [KSPField]
        public String animName;

        public float laserCost = 15000;
        public double laserAccD = 0;
        public double laserForceD = 0;
        public double dtk = 0;

        //Interface text
        [KSPField(guiActive = true, guiName = "Seconds avaliable")]
        protected String secBoughtS = "";
        [KSPField(guiActive = true, guiName = "Cost per second")]
        protected String laserCostS = "";
        [KSPField(guiActive = true, guiName = "Acceleration")]
        protected String laserAccS = "";
        [KSPField(guiActive = true, guiName = "Status")]
        protected String status = "";
        [KSPField(guiActive = true, guiName = "Test")]
        protected String testField = "";
        public static String temp;


        private Animation deployAnimation;




        //GameObject ui;
        public static string testString;
        [KSPEvent(guiActive = true, guiName = "Toggle Laser Manager", active = true)]
        public void toggleUI()
        {
            LaserUI.ToggleUI();
        }

        [KSPEvent(guiActive = true, guiName = "Debug Vessel Module", active = true)]
        public void uiDEBUG()
        {
            double Tal = Math.Pow(Math.Atan(2d / 70000d), 2);

            Debug.Log("[Laser Manager] " + testString);
            Debug.Log("[Laser Manager] Force Magnitude: " + vessel.GetTotalMass());
        }

        //Buy Seconds to be used later
        [KSPEvent(guiActive = true, guiName = "Buy Seconds", active = true)]
        public void buySailSeconds()
        {
            if (timeToBuy > 0)
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    if (Funding.CanAfford(timeToBuy * laserCost))
                    {
                        secBought += timeToBuy;
                        Debug.Log("[Sprite Mod] Bought " + timeToBuy + " seconds for " + (laserCost * timeToBuy) + " funds");
                        ScreenMessages.PostScreenMessage("-" + (laserCost * timeToBuy) + " funds", 2.0f, ScreenMessageStyle.UPPER_CENTER);
                        Funding.Instance.AddFunds(-(laserCost * timeToBuy), TransactionReasons.None);
                        timeToBuy = 0;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("Not enough money", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                }
                else
                {
                    secBought += timeToBuy;
                    Debug.Log("[Sprite Mod] Bought " + timeToBuy + " seconds");
                    timeToBuy = 0;
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Sell Seconds", active = true)]
        public void sellSailSeconds()
        {
            if (secBought > 0.001)
            {
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                {
                    ScreenMessages.PostScreenMessage("Sold " + secBought + " seconds for " + (secBought * laserCost), 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    Funding.Instance.AddFunds((laserCost * secBought), TransactionReasons.None);
                    secBought = 0;
                }
                else
                {
                    secBought = 0;
                }
            }
        }

        //[KSPEvent(guiActive = true, guiName = "Deploy Laser Sail", active = true, guiActiveEditor = false)]
        public void DeployLaserSail()
        {
            Debug.Log("[Sprite Mod] Laser Sail deployed");
            isDeployed = true;
            runAnimation(animName, deployAnimation, 1f, 0f);
        }

        //[KSPEvent(guiActive = true, guiName = "Retract Laser Sail", active = false, guiActiveEditor = false)]
        public void RetractLaserSail()
        {
            Debug.Log("[Sprite Mod] Laser Sail folded");
            isDeployed = false;
            isOn = false;
            runAnimation(animName, deployAnimation, -1f, 1f);
        }

        //[KSPEvent(guiActive = true, guiName = "RUN LASERS", active = false)]
        public void RunLasers()
        {
            Debug.Log("[Sprite Mod] Laser Sail reciving laser acceleration");
            isOn = true;
        }

        //[KSPEvent(guiActive = true, guiName = "STOP LASERS", active = false)]
        public void StopLasers()
        {
            Debug.Log("[Sprite Mod] Laser Sail stopped reciving laser acceleration");
            isOn = false;
        }

        public void ToggleLasers()
        {
            if (isOn)
                StopLasers();
            else
                RunLasers();
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Laser Sail", active = true, guiActiveEditor = true)]
        public void ToggleSail()
        {
            if (!isDeployed) DeployLaserSail();
            else RetractLaserSail();
        }

        [KSPAction("Toggle Sail")]
        public void toggleSailAction(KSPActionParam param)
        {
            if (!isDeployed) DeployLaserSail();
            else RetractLaserSail();
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[Sprite Mod] New Part with LaserSailModule detected. laserForce = " + laserForce + " and animName: " + animName);
            deployAnimation = part.FindModelAnimators(animName).FirstOrDefault();
            if (isDeployed) { runAnimation(animName, deployAnimation, 1f, 0f); }

            //Action Group
            Actions["toggleSailAction"].active = true;

            //Editor Tabs
            if (state == StartState.Editor)
            {
                Fields["timeToBuy"].guiActive = false;
                Fields["timeToBuy"].guiActiveEditor = false;
                Events["ToggleSail"].active = true;
            }
            else
            {
                Fields["timeToBuy"].guiActive = true;
                Events["ToggleSail"].active = false;
            }
        }

        public override void OnUpdate()
        {
            Fields["status"].guiActive = true;
            Fields["testField"].guiActive = true;
            Fields["secBoughtS"].guiActive = true;
            Fields["laserCostS"].guiActive = true;

            //Interface Events
            Events["buySailSeconds"].guiActive = true;

            //Interface Text Data Output
            Fields["laserAccS"].guiActive = isDeployed;

            if (secBought > 0.001) secBoughtS = secBought.ToString("0.000") + " s";
            else secBoughtS = "0 s";
            laserAccS = (laserAccD / 9.8066).ToString("0.000") + " g";
            laserCostS = laserCost.ToString("0.00") + " funds/s";
            if (!isDeployed)
            {
                status = "Idle";
            }
            else
            {
                if (!isOn)
                {
                    status = "Ready";
                }
                else
                {
                    double dT = TimeWarp.fixedDeltaTime;
                    if (DistanceToHome(this, vessel.orbit, this.part.transform.up, Planetarium.GetUniversalTime()) <= Math.Max(70000,FlightGlobals.Bodies[1].atmosphereDepth))
                    {
                        status = "Too close to home planet";
                    }
                    else if (secBought < dT && LaserUI.isModeKerbin)
                    {
                        status = "Not enough seconds avilable";
                    }
                    else
                    {
                        status = "Accelerating";
                    }
                }
            }
        }

        public void FixedUpdate()
        {

            if (!isOn) { return; }
            else
            {
                double dT = TimeWarp.fixedDeltaTime;

                if (DistanceToHome(this, vessel.orbit, this.part.transform.up, Planetarium.GetUniversalTime()) > Math.Max(70000, FlightGlobals.Bodies[1].atmosphereDepth) && (!LaserUI.isModeKerbin || secBought > 0))
                {
                    temp = " / DistanceToHome: " + DistanceToHome(this, vessel.orbit, this.part.transform.up, Planetarium.GetUniversalTime());
                    if (vessel.GetComponent<LaserVesselModule>() == null) return;
                    double UT = Planetarium.GetUniversalTime();

                    Vector3d finalForce;

                    if (LaserUI.isModeKerbin) finalForce = CalculateLaserForce(this, vessel.orbit, this.part.transform.up, UT);
                    else finalForce = CalculateLaserForce2(this, vessel.orbit, this.part.transform.up, UT, vessel.GetComponent<LaserVesselModule>().sourceVessel);

                    Vector3d finalAcc = Vector3.ClampMagnitude((finalForce / (vessel.GetTotalMass()*1000.0d)), (LaserUI.maxGee * 9.8066f)) * (area / vessel.GetComponent<LaserVesselModule>().sailArea);
                    vessel.ChangeWorldVelocity(finalAcc * dT);

                    laserForceD = finalForce.magnitude;
                    laserAccD = finalAcc.magnitude;

                    if(LaserUI.isModeKerbin) secBought -= dT*Math.Min( 1 , finalAcc.magnitude/(finalForce / (vessel.GetTotalMass() * 1000.0d)).magnitude );
                }
            }
        }

        public static Vector3d CalculateLaserForce(LaserSailModule sail, Orbit orbit, Vector3d normal, double UT) //Returns a Vector3d dependent to the relative position with Kerbin or Body[1]
        {
            Vector3d homePosition = FlightGlobals.Bodies[1].getPositionAtUT(UT);
            Vector3d ownPosition = orbit.getPositionAtUT(UT);
            Vector3d relativePosition = ownPosition - homePosition;
            double angle = Vector3d.Angle(normal, relativePosition);

            if (angle > 90d) { normal = -normal; angle = 180.0-angle; }

            double distance = relativePosition.magnitude - FlightGlobals.Bodies[1].Radius;

            double areaCovered = Math.Min(1, Math.Pow(Math.Sqrt(sail.area/LaserUI.shapeConstant), 2)/Math.Pow(LaserUI.focusAspectRatio*distance,2) );
            double forceMagnitude = Math.Cos(Mathf.Deg2Rad * angle) * LaserUI.forcePerGenerator * LaserUI.generatorsAtKSC[LaserUI.kscLevel] * areaCovered;

            Vector3d force = normal * forceMagnitude;
            
            return force;
        }

        public static Vector3d CalculateLaserForce2(LaserSailModule sail, Orbit orbit, Vector3d normal, double UT, Vessel v) //Returns a Vector3d dependent to the relative position and power of Vessel v
        {
            Vector3d sourcePosition = v.orbit.getPositionAtUT(UT);
            Vector3d ownPosition = orbit.getPositionAtUT(UT);
            Vector3d relativePosition = ownPosition - sourcePosition;
            double angle = Vector3d.Angle(normal, relativePosition);
            if (angle > 90d) { normal = -normal; angle = 180.0 - angle; }

            double distance = relativePosition.magnitude;

            double areaCovered = Math.Min(1, (Math.Sqrt(sail.area / LaserUI.shapeConstant)) / Math.Pow(LaserUI.focusAspectRatio * distance, 2));
            double forceMagnitude = Math.Cos(Mathf.Deg2Rad * angle) * v.GetComponent<LaserVesselModule>().totalForce * areaCovered;

            Vector3d force = normal * forceMagnitude;
            testString = "Magnitude of Force: " + forceMagnitude + " / Distance: " + relativePosition.magnitude + temp;
            
            return force;
        }

        public static double DistanceToHome(LaserSailModule sail, Orbit orbit, Vector3d normal, double UT)
        {
            Vector3d homePosition = FlightGlobals.Bodies[1].getPositionAtUT(UT);
            Vector3d ownPosition = orbit.getPositionAtUT(UT);
            Vector3d relativePosition = ownPosition - homePosition;

            double output = relativePosition.magnitude - FlightGlobals.Bodies[1].Radius;
            return output;
        }

        private void runAnimation(string animationMame, Animation anim, float speed, float aTime)
        {
            if (anim != null)
            {
                anim[animationMame].speed = speed;
                if (!anim.IsPlaying(animationMame))
                {
                    anim[animationMame].wrapMode = WrapMode.Default;
                    anim[animationMame].normalizedTime = aTime;
                    anim.Blend(animationMame, 1);
                }
            }
        }



        public string GetModuleTitle()
        {
            return "Laser Sail";
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            string output = "Area: " + area;
            return output;
        }
    }
}
