using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using IllusionPlugin;
using TMPro;

/// <summary>
///  New Calorie Counting algorithm that uses physical movements relative to weight and preset values.
///  Will be much more accurate than previous counting method.
/// </summary>

namespace BeFitMod
{
    class igcv02x : MonoBehaviour
    {
        private StandardLevelSceneSetupDataSO lvlData;
        private int lifeCalories = Plugin.Instance.Config.lifeCalories;
        private int dailyCalories = Plugin.Instance.Config.dailyCalories;
        private int currentSessionCals = Plugin.Instance.Config.sessionCalories;
        float playerWeightlbs = Plugin.Instance.Config.weightLBS;
        float weightKg;
        float playerWeightkgs = Plugin.Instance.Config.weightKGS;
        bool lbsOrKgs = Plugin.Instance.Config.metricUnits;
        ////////////////////////////////////////////////////////////////////////////////////////
        float timeBFU = (1/90f); // 1 frame per 90, fixed update
        float timeAccuracy = Plugin.Instance.Config.calorieCounterAccuracy;
        float timeBRU; 
        float hoursInFixedUpdate;
        ////////////////////////////////////////////////////////////////////////////////////////
        float totalCaloriesBurnt = 0;
        float[] headvelocityCoefficient = new float[5] { 1f, 1.4f, 2, 3f, 4 };
        float[] METSVALShead = new float[5] { 14f, 15, 16, 17, 19 };
        ////////////////////////////////////////////////////////////////////////////////////////
        float[] handvelocityCoefficient = new float[5] { 0.85f, 1.9f, 2.75f, 3.7f, 5f };
        float[] METSVALShands = new float[5] { 6.35f, 8.8f, 9.6f, 10.45f, 13f };
        ////////////////////////////////////////////////////////////////////////////////////////
        bool dcc = Plugin.Instance.Config.inGameCaloriesDisplay;
        Vector3 HMDvelocity;
        Vector3 LHCvelocity;
        Vector3 RHCvelocity;
        GameObject LiveCount;
        public static TextMeshPro LiveCountText;
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        void Awake()
        {
            timeBRU = (timeAccuracy * timeBFU);
            hoursInFixedUpdate = (timeBRU / 3600);
            lvlData = Resources.FindObjectsOfTypeAll<StandardLevelSceneSetupDataSO>().FirstOrDefault();
            if (lbsOrKgs)
            {
                weightKg = playerWeightkgs;
            }
            else
            {
                weightKg = playerWeightlbs * 0.4535924f;
            }
            ////////////////////////////////////////////////////////////////////////////////////////
            LiveCountText = this.gameObject.AddComponent<TextMeshPro>();
            LiveCountText.renderer.enabled = dcc;
            LiveCountText.text = "0";
            LiveCountText.fontSize = 4;
            LiveCountText.color = Color.white;
            LiveCountText.font = Resources.Load<TMP_FontAsset>("Beon SDF No-Glow");
            LiveCountText.alignment = TextAlignmentOptions.Center;
            transform.position = Plugin.counterPosition;
            ////////////////////////////////////////////////////////////////////////////////////////
            LiveCount = new GameObject("Label");
            TextMeshPro label = LiveCount.AddComponent<TextMeshPro>();
            label.renderer.enabled = dcc;
            label.text = "Calories";
            label.fontSize = 3;
            label.color = Color.white;
            label.font = Resources.Load<TMP_FontAsset>("Beon SDF No-Glow");
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.position = Plugin.counterPosition;
            LiveCount.transform.parent = LiveCountText.transform;
            LiveCount.transform.localPosition = new Vector3(0, 0.4f, 0);
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void OnDestroy()
        {
            int calories = (int)totalCaloriesBurnt;
            headvelocityCoefficient = null;
            METSVALShead = null;
            handvelocityCoefficient = null;
            METSVALShands = null;
            MenuDisplay.countLGC.text = lvlData.difficultyBeatmap.level.songName;
            MenuDisplay.labelLG.text = "Last Played Song";
            MenuDisplay.lgcText.text = (calories).ToString();
            MenuDisplay.cscText.text = (currentSessionCals + calories).ToString();
            MenuDisplay.lcText.text = (lifeCalories + calories).ToString();
            MenuDisplay.dcText.text = (dailyCalories + calories).ToString();
            Plugin.Instance.Config.lifeCalories = lifeCalories + calories;
            Plugin.Instance.Config.dailyCalories = dailyCalories + calories;
            Plugin.Instance.Config.sessionCalories = currentSessionCals + calories;
            //Console.WriteLine(Plugin.modLog + "Current Calories: " + ModPrefs.GetInt("fitNessMod", "sessionCalories", 0, true));
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        void FixedUpdate()
        {
            InputTracking.GetNodeStates(nodeStates);
            if (!IsRunning)
            {
                StartCoroutine("CalorieCounter");
            }
            else if (!colChange)
            {
                StartCoroutine("colorChange");
            }
        }
        bool colChange = false;
        IEnumerator colorChange()
        {
            colChange = true;
            if (totalCaloriesBurnt >= 8 && totalCaloriesBurnt <= 14)
            {
                LiveCountText.color = Color.cyan;
            }
            else if(totalCaloriesBurnt >= 15 && totalCaloriesBurnt <= 24)
            {
                LiveCountText.color = Color.blue;
            }
            else if(totalCaloriesBurnt >= 25 && totalCaloriesBurnt <= 34)
            {
                LiveCountText.color = Color.magenta;
            }
            else if(totalCaloriesBurnt >= 35 && totalCaloriesBurnt <= 45)
            {
                LiveCountText.color = Color.red;
            }
            
            yield return new WaitForSeconds(10);
            colChange = false;
        }
        ///////////////////////////////////////////////////////////////////////////////////////////////////
        private void calCalc(float METS)
        {
            float lastBurn = (METS * weightKg * hoursInFixedUpdate); // Calories burned per run time, calculated at awake()
            totalCaloriesBurnt += lastBurn;
            int displayText = (int)totalCaloriesBurnt;
            LiveCountText.text = displayText.ToString();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////
        bool IsRunning = false;
        float[] aveAll;
        IEnumerator CalorieCounter()
        {
            YieldInstruction waitForFixedUpdate = new WaitForFixedUpdate();
            while (true)
            {
                IsRunning = true;
                aveAll = new float[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                foreach (XRNodeState ns in nodeStates)
                {
                    ///////////////////////////////////////////////////////////////////            Head           ///////////////////////////////////////////            Head
                    if (ns.nodeType == XRNode.Head)
                    {
                        //Console.WriteLine("Head");
                        ns.TryGetVelocity(out HMDvelocity);
                        if (Math.Abs(HMDvelocity.x) >= headvelocityCoefficient[0])
                        {
                            if (Math.Abs(HMDvelocity.x) >= headvelocityCoefficient[0] && Math.Abs(HMDvelocity.x) < headvelocityCoefficient[1])
                            {
                                aveAll[0] = METSVALShead[0]; 
                            }
                            else if (Math.Abs(HMDvelocity.x) >= headvelocityCoefficient[1] && Math.Abs(HMDvelocity.x) < headvelocityCoefficient[2])
                            {
                                aveAll[0] = METSVALShead[1];
                            }
                            else if (Math.Abs(HMDvelocity.x) >= headvelocityCoefficient[2] && Math.Abs(HMDvelocity.x) < headvelocityCoefficient[3])
                            {
                                aveAll[0] = METSVALShead[2];
                            }
                            else if (Math.Abs(HMDvelocity.x) >= headvelocityCoefficient[3] && Math.Abs(HMDvelocity.x) < headvelocityCoefficient[4])
                            {
                                aveAll[0] = METSVALShead[3];
                            }
                            else if (Math.Abs(HMDvelocity.x) >= 3)
                            {
                                aveAll[0] = METSVALShead[4];
                            }
                        }
                        if (Math.Abs(HMDvelocity.y) >= headvelocityCoefficient[0])
                        {
                            if (Math.Abs(HMDvelocity.y) >= headvelocityCoefficient[0] && Math.Abs(HMDvelocity.y) < headvelocityCoefficient[1])
                            {
                                aveAll[1] = METSVALShead[0]; ;
                            }
                            else if (Math.Abs(HMDvelocity.y) >= headvelocityCoefficient[1] && Math.Abs(HMDvelocity.y) < headvelocityCoefficient[2])
                            {
                                aveAll[1] = METSVALShead[1];
                            }
                            else if (Math.Abs(HMDvelocity.y) >= headvelocityCoefficient[2] && Math.Abs(HMDvelocity.y) < headvelocityCoefficient[3])
                            {
                                aveAll[1] = METSVALShead[2];
                            }
                            else if (Math.Abs(HMDvelocity.y) >= headvelocityCoefficient[3] && Math.Abs(HMDvelocity.y) < headvelocityCoefficient[4])
                            {
                                aveAll[1] = METSVALShead[3];
                            }
                            else if (Math.Abs(HMDvelocity.y) >= 3)
                            {
                                aveAll[1] = METSVALShead[4];
                            }
                        }
                        if (Math.Abs(HMDvelocity.z) >= headvelocityCoefficient[0])
                        {
                            if (Math.Abs(HMDvelocity.z) >= headvelocityCoefficient[0] && Math.Abs(HMDvelocity.z) < headvelocityCoefficient[1])
                            {
                                aveAll[2] = METSVALShead[0];
                            }
                            else if (Math.Abs(HMDvelocity.z) >= headvelocityCoefficient[1] && Math.Abs(HMDvelocity.z) < headvelocityCoefficient[2])
                            {
                                aveAll[2] = METSVALShead[1];
                            }
                            else if (Math.Abs(HMDvelocity.z) >= headvelocityCoefficient[2] && Math.Abs(HMDvelocity.z) < headvelocityCoefficient[3])
                            {
                                aveAll[2] = METSVALShead[2];
                            }
                            else if (Math.Abs(HMDvelocity.z) >= headvelocityCoefficient[3] && Math.Abs(HMDvelocity.z) < headvelocityCoefficient[4])
                            {
                                aveAll[2] = METSVALShead[3];
                            }
                            else if (Math.Abs(HMDvelocity.z) >= 3)
                            {
                                aveAll[2] = METSVALShead[4];
                            }
                        }
                        //Console.WriteLine("Head Complete");
                    }
                    ///////////////////////////////////////////////////////////////////////            Left Hand          ///////////////////////////////////////////            Left Hand
                    if (ns.nodeType == XRNode.LeftHand)
                    {
                        //Console.WriteLine("LeftHand");
                        ns.TryGetVelocity(out LHCvelocity);
                        if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[0] && Math.Abs(LHCvelocity.x) < handvelocityCoefficient[1])
                            {
                                aveAll[3] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[1] && Math.Abs(LHCvelocity.x) < handvelocityCoefficient[2])
                            {
                                aveAll[3] = METSVALShands[1];
                            }
                            else if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[2] && Math.Abs(LHCvelocity.x) < handvelocityCoefficient[3])
                            {
                                aveAll[3] = METSVALShands[2];
                            }
                            else if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[3] && Math.Abs(LHCvelocity.x) < handvelocityCoefficient[4])
                            {
                                aveAll[3] = METSVALShands[3];
                            }
                            else if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[4])
                            {
                                aveAll[3] = METSVALShands[4];
                            }
                        }
                        if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[0] && Math.Abs(LHCvelocity.y) < handvelocityCoefficient[1])
                            {
                                aveAll[4] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[1] && Math.Abs(LHCvelocity.y) < handvelocityCoefficient[2])
                            {
                                aveAll[4] = METSVALShands[1];
                            }
                            else if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[2] && Math.Abs(LHCvelocity.y) < handvelocityCoefficient[3])
                            {
                                aveAll[4] = METSVALShands[2];
                            }
                            else if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[3] && Math.Abs(LHCvelocity.y) < handvelocityCoefficient[4])
                            {
                                aveAll[4] = METSVALShands[3];
                            }
                            else if (Math.Abs(LHCvelocity.y) >= handvelocityCoefficient[4])
                            {
                                aveAll[4] = METSVALShands[4];
                            }
                        }
                        if (Math.Abs(LHCvelocity.z) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(LHCvelocity.z) >= handvelocityCoefficient[0] && Math.Abs(LHCvelocity.z) < handvelocityCoefficient[1])
                            {
                                aveAll[5] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(LHCvelocity.x) >= handvelocityCoefficient[1] && Math.Abs(LHCvelocity.z) < handvelocityCoefficient[2])
                            {
                                aveAll[5] = METSVALShands[1];
                            }
                            else if (Math.Abs(LHCvelocity.z) >= handvelocityCoefficient[2] && Math.Abs(LHCvelocity.z) < handvelocityCoefficient[3])
                            {
                                aveAll[5] = METSVALShands[2];
                            }
                            else if (Math.Abs(LHCvelocity.z) >= handvelocityCoefficient[3] && Math.Abs(LHCvelocity.z) < handvelocityCoefficient[4])
                            {
                                aveAll[5] = METSVALShands[3];
                            }
                            else if (Math.Abs(LHCvelocity.z) >= handvelocityCoefficient[4])
                            {
                                aveAll[5] = METSVALShands[4];
                            }
                        }
                        //Console.WriteLine("Left Hand Complete");
                    }
                    /////////////////////////////////////////////////////////////////////           Right Hand        ///////////////////////////////////////////            Right Hand
                    if (ns.nodeType == XRNode.RightHand)
                    {
                        //Console.WriteLine("RightHand");
                        ns.TryGetVelocity(out RHCvelocity);
                        if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[0] && Math.Abs(RHCvelocity.x) < handvelocityCoefficient[1])
                            {
                                aveAll[6] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[1] && Math.Abs(RHCvelocity.x) < handvelocityCoefficient[2])
                            {
                                aveAll[6] = METSVALShands[1];
                            }
                            else if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[2] && Math.Abs(RHCvelocity.x) < handvelocityCoefficient[3])
                            {
                                aveAll[6] = METSVALShands[2];
                            }
                            else if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[3] && Math.Abs(RHCvelocity.x) < handvelocityCoefficient[4])
                            {
                                aveAll[6] = METSVALShands[3];
                            }
                            else if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[4])
                            {
                                aveAll[6] = METSVALShands[4];
                            }


                        }
                        if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[0] && Math.Abs(RHCvelocity.y) < handvelocityCoefficient[1])
                            {
                                aveAll[7] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[1] && Math.Abs(RHCvelocity.y) < handvelocityCoefficient[2])
                            {
                                aveAll[7] = METSVALShands[1];
                            }
                            else if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[2] && Math.Abs(RHCvelocity.y) < handvelocityCoefficient[3])
                            {
                                aveAll[7] = METSVALShands[2];
                            }
                            else if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[3] && Math.Abs(RHCvelocity.y) < handvelocityCoefficient[4])
                            {
                                aveAll[7] = METSVALShands[3];
                            }
                            else if (Math.Abs(RHCvelocity.y) >= handvelocityCoefficient[4])
                            {
                                aveAll[7] = METSVALShands[4];
                            }


                        }
                        if (Math.Abs(RHCvelocity.z) >= handvelocityCoefficient[0])
                        {
                            if (Math.Abs(RHCvelocity.z) >= handvelocityCoefficient[0] && Math.Abs(RHCvelocity.z) < handvelocityCoefficient[1])
                            {
                                aveAll[8] = METSVALShands[0]; //Equivalent to 2 to 3 mph for one hour
                            }
                            else if (Math.Abs(RHCvelocity.x) >= handvelocityCoefficient[1] && Math.Abs(RHCvelocity.z) < handvelocityCoefficient[2])
                            {
                                aveAll[8] = METSVALShands[1];
                            }
                            else if (Math.Abs(RHCvelocity.z) >= handvelocityCoefficient[2] && Math.Abs(RHCvelocity.z) < handvelocityCoefficient[3])
                            {
                                aveAll[8] = METSVALShands[2];
                            }
                            else if (Math.Abs(RHCvelocity.z) >= handvelocityCoefficient[3] && Math.Abs(RHCvelocity.z) < handvelocityCoefficient[4])
                            {
                                aveAll[8] = METSVALShands[3];
                            }
                            else if (Math.Abs(RHCvelocity.z) >= handvelocityCoefficient[4])
                            {
                                aveAll[8] = METSVALShands[4];
                            }
                        }
                        //Console.WriteLine("Right Hand Complete");
                    }
                }
                int count = 0;
                for (int i = 0; i < 9; i++)
                {
                    if (aveAll[i] > 1)
                    {
                        //Console.WriteLine(aveAll[i]);
                        count++;
                        aveAll[9] = aveAll[9] + aveAll[i];
                    }
                }
                if (aveAll[9] > 1 && aveAll[9] < 100)
                {
                    float counted = aveAll[9] / count;
                    //Console.WriteLine(counted); //averages
                    calCalc((aveAll[9] / count));
                }
                for (float duration = timeAccuracy; duration > 0; duration--)
                {
                    yield return new WaitForFixedUpdate();
                }
                IsRunning = false;
            }
        }
    }
}



