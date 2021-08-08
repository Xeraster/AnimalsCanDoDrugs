using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace AnimalsCanDoDrugs
{

    [StaticConstructorOnStartup]
    public static class MainLoader
    {
        public static List<drugEntry> staticDrugList;
        static MainLoader()
        {
            //Log.Message("Animals Can Do Drugs startup success");

            var harmony = new Harmony("AnimalsCanDoDrugs");
            harmony.PatchAll();
            Log.Message("[AnimalsCanDoDrugs]patched harmony assembly. Startup success.");

            //populate defaults. Anything in this list defaults to false if no settings are found. Anything not in this list defaults to true
            AnimalsCanDoDrugsSettings.drugDefaults.Add("Luciferium");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("Flake");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("SmokeleafJoint");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("PotassiumIodide");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("GR_AgePills");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("GR_FeatherdustJoint");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("GR_PoisonAmpoule");
            AnimalsCanDoDrugsSettings.drugDefaults.Add("GR_VirulentPoison");

            Log.Message("[AnimalsCanDoDrugs]before populate drug function");
            populateDrugArray();
            Log.Message("[AnimalsCanDoDrugs]after populate drug function");

            applyAdditionalSettings();

        }

        public static void applyAdditionalSettings()
        {
            Log.Message("in the applyAdditionalSettings() function");
            if (AnimalsCanDoDrugsSettings.staticPrisonerRecreation)
            {
                Log.Message("[AnimalsCanDoDrugs]colonists only=" + NeedDefOf.Joy.colonistsOnly + " colonists+prisoners only=" + NeedDefOf.Joy.colonistAndPrisonersOnly);
                Log.Message("[AnimalsCanDoDrugs]never on prisoner=" + NeedDefOf.Joy.neverOnPrisoner + " show on need list=" + NeedDefOf.Joy.showOnNeedList);
                NeedDefOf.Joy.neverOnPrisoner = false;
                NeedDefOf.Joy.colonistsOnly = false;
                NeedDefOf.Joy.colonistAndPrisonersOnly = true;
            }


            if (AnimalsCanDoDrugsSettings.staticGiveDrugsNutrition)
            {
                Log.Message("[AnimalsCanDoDrugs]in the give drugs nutrition part");
                foreach (ThingDef boozeDef in ThingCategoryDefOf.Drugs.childThingDefs)
                {
                    if ((boozeDef.StatBaseDefined(StatDefOf.Nutrition) && boozeDef.ingestible.preferability == FoodPreferability.NeverForNutrition))
                    {
                        //for each found drug, put it in this list
                        boozeDef.ingestible.preferability = FoodPreferability.DesperateOnlyForHumanlikes;
                        //Log.Message(boozeDef.defName + " found. food pref = " + boozeDef.ingestible.preferability.ToString());
                    }
                }

                if (rimCuisine2BoozeLoaded())
                {
                    Log.Message("[AnimalsCanDoDrugs] If an error happens under this line, move this mod below Rimcuisine 2 booze in the load order and try again");
                    foreach (ThingDef booze2Def in ThingCategoryDef.Named("RC2_Alcohol").childThingDefs)
                    {
                        if (booze2Def != null && booze2Def.ingestible.joyKind.defName == "Chemical" && booze2Def.StatBaseDefined(StatDefOf.Nutrition) && booze2Def.ingestible.preferability == FoodPreferability.NeverForNutrition && booze2Def.defName != "RC2_AmbrosiaNectar")
                        {
                            //for each found drug, put it in this list
                            booze2Def.ingestible.preferability = FoodPreferability.DesperateOnlyForHumanlikes;
                            //Log.Message(booze2Def.defName + " found. food pref = " + booze2Def.ingestible.preferability.ToString());
                        }
                    }
                    Log.Message("[AnimalsCanDoDrugs] If this message printed, that error I was talking about didn't happen");
                }
            }
            //Log.Message("done with the additional settings function");
        }

        public static bool rimCuisine2BoozeLoaded()
        {
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod.PackageId.Contains("crustypeanut.rc2.babe"))
                {
                    Log.Message("[AnimalsCanDoDrugs] " + mod.Name + " detected with package id of " + mod.PackageId);
                    Log.Message("[AnimalsCanDoDrugs] If rimcuisine 2 ever changes its booze category to anything other than 'RC2_Alcohol' or ever changes its package id from 'crustypeanut.rc2.babe', it will cause this step to fail");
                    return true;
                }
                //Log.Message(mod.Name + " " + mod.PackageId + " " + mod.enabled);
            }
            return false;
        }
        public static void applyDrugSettings()
        {
            foreach (drugEntry drug in AnimalsCanDoDrugsSettings.intermediateList)
            {
                if (drug.nutritionEnabled) AddNutritionToDrug(drug);
                else if (!drug.nutritionEnabled && ThingDef.Named(drug.defName).StatBaseDefined(StatDefOf.Nutrition))
                {
                    Log.Message("[AnimalsCanDoDrugs]removing nutrition modifier from " + drug.defName);
                    List<StatModifier> newStatList = new List<StatModifier>();
                    //List<StatModifier> theStats = ThingDef.Named(drug.defName).statBases;
                    foreach (StatModifier stat in ThingDef.Named(drug.defName).statBases)
                    {
                        if (stat.ToString() != StatDefOf.Nutrition.defName + "-" + "0.01")
                        {
                            newStatList.Add(stat);
                        }
                    }
                    ThingDef.Named(drug.defName).statBases = newStatList;
                    ThingDef.Named(drug.defName).ingestible.preferability = FoodPreferability.NeverForNutrition;
                }
            }
        }

        public static void AddNutritionToDrug(drugEntry drugToModify)
        {
            //get def by name. This makes it easy to implement other mod's drugs without adding dependencies 
            ThingDef drugDef = DefDatabase<ThingDef>.GetNamed(drugToModify.defName);

            //drug nutrition is the stat modifier that needs to be applied to all the drugs that are desired to have nutrition values added to them
            StatModifier drugNutrition = new StatModifier();
            drugNutrition.value = 0.01f;
            drugNutrition.stat = StatDefOf.Nutrition;

            //apply the nutrition stat base to the drug
            drugDef.statBases.Add(drugNutrition);
            //drugDef.ingestible.preferability = FoodPreferability.DesperateOnlyForHumanlikes;
        }

        public static bool areListsEqual(List<drugEntry> drugList1, List<drugEntry> drugList2)
        {
            bool equalSoFar = true;
            if (drugList1.Count != drugList2.Count)
            {
                Log.Message("[AnimalsCanDoDrugs]drugList1 size = " + drugList1.Count + " and drugList2 size = " + drugList2.Count);
                return false;
            }
            for (int i = 0; i < drugList1.Count; i++)
            {
                if (drugList1.ToArray()[i].defName != drugList2.ToArray()[i].defName)
                {
                    equalSoFar = false;
                    break;
                }
            }

            if (equalSoFar)
            {
                Log.Message("[AnimalsCanDoDrugs]lists are equal");
                return true;
            }
            else
            {
                Log.Message("[AnimalsCanDoDrugs]lists are not equal");
                return false;
            }
        }

        //get a list of all loaded drug defs and populate them into a List
        public static void populateDrugArray()
        {
            //convention for drug entry class constructor is drugEntry(name, defname)
            //Log.Message("clear static drug list");
            staticDrugList = new List<drugEntry>();

            //Log.Message("get ready to run through the loop that checks for all the different kinds of drugs");

            foreach (ThingDef drugDef in ThingCategoryDefOf.Drugs.childThingDefs)
            {
                if (drugDef != null)
                {
                    //only load drugs that don't already have a nutrition value
                    //this keeps it from getting overloaded with crap since there are lots of mods that add zillions of different types of booze
                    if (!drugDef.StatBaseDefined(StatDefOf.Nutrition))
                    {
                        //for each found drug, put it in this list
                        staticDrugList.Add(new drugEntry(drugDef.defName));
                        //Log.Message(drugDef.defName + " found. food pref = " + drugDef.ingestible.preferability.ToString());
                    }
                }
            }
            //Log.Message("done with the for loop");
            shittyCompare compareObject = new shittyCompare();
            staticDrugList.Sort(compareObject);
            Log.Message("[AnimalsCanDoDrugs]list sorted alphabetically");
            //now, compare the loaded defs to the saved defs
            //there are 3 cases:
            //1. loaded xml drug entries are the same size as the games defs
            //2. loaded xml drug entries are larger size than games defs
            //3. loaded xml drug entries are smaller size than games defs

            //if case 1, don't do anything else
            if (!areListsEqual(staticDrugList, AnimalsCanDoDrugsSettings.intermediateList))
            {
                //what to do if case 3 or case 2
                Log.Message("[AnimalsCanDoDrugs]xml drug defs are greater than game loaded drug defs");
                foreach (drugEntry drug in staticDrugList)
                {
                    foreach (drugEntry loadedDrug in AnimalsCanDoDrugsSettings.intermediateList)
                    {
                        if (drug.defName == loadedDrug.defName)
                        {
                            drug.nutritionEnabled = loadedDrug.nutritionEnabled;
                        }
                    }
                }
                AnimalsCanDoDrugsSettings.intermediateList = staticDrugList;
            }

            //now that all the loading has been completed, let's apply nutrition settings to each drug
            applyDrugSettings();
            AnimalsCanDoDrugsSettings.initLoaded = true;

        }

    }



    public class AnimalsCanDoDrugsSettings : ModSettings
    {
        public static List<string> drugDefaults = new List<string>();

        public List<drugEntry> nonStaticDrugList = new List<drugEntry>();
        public static List<drugEntry> intermediateList = new List<drugEntry>();
        public static bool initLoaded = false;
        public bool giveDrugsNutritionPreference = false;
        public bool enablePrisonerRecreation = true;

        public static bool staticGiveDrugsNutrition = false;
        public static bool staticPrisonerRecreation = true;

        public string name = "ACDD";
        public int loadID = 0;

        public override void ExposeData()
        {
            //Log.Message("is mainloader already loaded?");

            //if expose data is in save mode and has alrready loaded, do this
            //Log.Message("MainLoader.alreadyLoaded = true apparently.");
            nonStaticDrugList = intermediateList;
            Log.Message("[AnimalsCanDoDrugs]set nonStaticDrugList = MainLoader.staticDrugList. About to run foreach loop");

            Scribe_Collections.Look(ref nonStaticDrugList, "activatedDrugs", LookMode.Deep);

            Log.Message("[AnimalsCanDoDrugs]Done with foreach loop. setting MainLoader.staticDrugList = nonStaticDrugList");
            Scribe_Values.Look(ref giveDrugsNutritionPreference, "giveDrugsNutritionPreference", false, true);
            Scribe_Values.Look(ref enablePrisonerRecreation, "enablePrisonerRecreation", true, true);
            staticGiveDrugsNutrition = giveDrugsNutritionPreference;
            staticPrisonerRecreation = enablePrisonerRecreation;
            Log.Message("[AnimalsCanDoDrugs]Done with standard options");
            shittyCompare compareObject = new shittyCompare();
            nonStaticDrugList.Sort(compareObject);
            intermediateList = nonStaticDrugList;

            //Log.Message("About to run base.ExposeData()");
            base.ExposeData();
            //Log.Message("finished exposing data");
            //only do this part if it's after the part where the game defs themselves have been loaded
            if (initLoaded)
            {
                Log.Message("[AnimalsCanDoDrugs]running applyDrugSettings() from within exposeData()");
                MainLoader.applyDrugSettings();
            }
            //if expose data is running its initial startup routine, do this
            else
            {
                Log.Message("[AnimalsCanDoDrugs]exposeData claims initLoaded = false. If there are no errors 1 line above or 1 line below this message, this is fine and intended");
            }
        }

    }


    public class AnimalsCanDoDrugs : Mod
    {

        public static Vector2 scrollPosition;

        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public AnimalsCanDoDrugsSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public AnimalsCanDoDrugs(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<AnimalsCanDoDrugsSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            float height_modifier;
            //you can have 23 things per view regardless of ui scaling. This works out to 25.391304348 screen units (I guess) per line of text
            if (AnimalsCanDoDrugsSettings.intermediateList.Count < 24) height_modifier = 100f;
            else height_modifier = 100f + ((AnimalsCanDoDrugsSettings.intermediateList.Count - 23) * 25);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 26f, inRect.height + height_modifier);
            Rect outRect = new Rect(0f, 30f, inRect.width, inRect.height - 30f);
            Listing_Standard listingStandard = new Listing_Standard();
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            //Widgets listingStandard = new Widgets();
           // listingStandard.BeginScrollView(outRect, ref scrollPosition, ref viewRect);
            listingStandard.Begin(viewRect);

            listingStandard.CheckboxLabeled("Give minimum food preferability to all drugs/alcohol (causes funny things, but NOT RECOMMENDED)", ref settings.giveDrugsNutritionPreference, "NOT RECOMMENDED. Enables the ability for prisoners to eat drugs without force-feeding it to them. It makes colonists do stupid things such as serve yayo to downed pawns and kill them.");
            listingStandard.CheckboxLabeled("Prisoners have recreation need", ref settings.enablePrisonerRecreation, "Enables recreation needs bar for prisoners.");
            listingStandard.Label("Checkmark whichever drugs you want animals to be allowed to eat on their own. Note that you do not need to restart the game for any changes below this line to take effect.");
            foreach (drugEntry drug in AnimalsCanDoDrugsSettings.intermediateList)
            {
               listingStandard.CheckboxLabeled(ThingDef.Named(drug.defName).label, ref drug.nutritionEnabled, generateDescription(drug));
            }

            listingStandard.Label("If you are reading this, all the stuff got loaded correctly");

            //listingStandard.EndScrollView(ref viewRect);
            Widgets.EndScrollView();
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            //return "MyExampleModName".Translate();
            return "Animals Can Do Drugs";
        }

        //i'm making a function to return the mouseover description string since pasting this in the function in the CheckboxLavel in DoSettingsWindowContents would get too cluttered
        public string generateDescription(drugEntry whichDrug)
        {
            return "allow animals to sometimes eat " + ThingDef.Named(whichDrug.defName).label + ". " + ThingDef.Named(whichDrug.defName).description;
        }

    }

    //some drugs (in other mods) have different names from their def names
    //in order to make the settings menu not look stupid, i'm doing this to differentiate from those 2 things
    public class drugEntry : IExposable
    {
        public string defName = "error";
        public bool nutritionEnabled = false;

        public drugEntry()
        {
            //name = theName;
            defName = "error";
            nutritionEnabled = false;
        }

        public drugEntry(string theDefName)
        {
            //name = theName;
            defName = theDefName;
            nutritionEnabled = getDefaults();
        }

        public drugEntry(string theDefName, bool theNutritionValue)
        {
            //name = theName;
            defName = theDefName;
            nutritionEnabled = theNutritionValue;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref defName, "drugName");
            Scribe_Values.Look(ref nutritionEnabled, "nutritionEnabled", false, true);
        }

        public int CompareTo(drugEntry compDrug)
        {
            //it sorts in alphabetical order of the defname and not the label name
            if (compDrug == null) return 1;
            return this.defName.CompareTo(compDrug.defName);
        }

        public bool getDefaults()
        {
            //Log.Message("does drugDefaults contain " + defName + "?");
            if (AnimalsCanDoDrugsSettings.drugDefaults.Contains(defName)) return false;
            else return true;
        }
    }

    public class shittyCompare : IComparer<drugEntry>
    {
        public int Compare(drugEntry drug1, drugEntry drug2)
        {
            if (drug1.defName == null || drug2.defName == null)
            {
                return 0;
            }
            return drug1.CompareTo(drug2);
        }
    }

    //this works by changing the input paremeter minPrefOverride by ref. Apparently doing it like this works. Cool
    [HarmonyPatch(typeof(FoodUtility), "BestFoodSourceOnMap")]
    static class thingThatMakesPrisonersEatDrugsIfAvailableAndNotStarveLikeFuckingDumbasses
    {
        static bool Prefix(Pawn getter, Pawn eater, bool desperate, out ThingDef foodDef, FoodPreferability maxPref = FoodPreferability.MealLavish, bool allowPlant = true, bool allowDrug = true, bool allowCorpse = true, bool allowDispenserFull = true, bool allowDispenserEmpty = true, bool allowForbidden = false, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false, bool ignoreReservations = false, FoodPreferability minPrefOverride = FoodPreferability.Undefined)
        {
            //need to include getter.IsPrisoner so colonists don't just hook prisoners up with free drugs
            //the intended purpose is so prisoners will eat drugs or booze (if no food is available) rather than starve. Not to have them high and happy
            if (eater.IsPrisoner && eater.IsTeetotaler() == false && getter.IsPrisoner)
            {
                //need to include a check to make prisoners only do this if no food is available. Or I could just not. Or I could make a configurable setting to turn that on or off
                //disregard the comment above this. Thanks to the food preferability system, prisoners will already eat the healthist food available and not just pig out on cocaine if they're hungry
                Log.Message("make prisoner eat drugs or booze");
                minPrefOverride = FoodPreferability.DesperateOnly;
                foodDef = null;
                //__result = true;
                return true;
            }
            foodDef = null;
            //__result = false;
            return true;
        }
    }
    //public static Thing BestFoodSourceOnMap(Pawn getter, Pawn eater, bool desperate, out ThingDef foodDef, FoodPreferability maxPref = FoodPreferability.MealLavish, bool allowPlant = true, bool allowDrug = true, bool allowCorpse = true, bool allowDispenserFull = true, bool allowDispenserEmpty = true, bool allowForbidden = false, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false, bool ignoreReservations = false, FoodPreferability minPrefOverride = FoodPreferability.Undefined)

    /*
    [HarmonyPatch(typeof(FoodUtility))]
    [HarmonyPatch(nameof(FoodUtility.WillEat))]
    [HarmonyPatch(typeof(FoodUtility), "WillEat", new[] { typeof(Pawn), typeof(ThingDef), typeof(Pawn), typeof(bool) })]
    static class thingThatAllowsPrisonersToEatDrugsThingDef
    {
        static bool Prefix(this Pawn p, ThingDef food, Pawn getter = null, bool careIfNotAcceptableForTitle = true)
        {
            if (food.IsWithinCategory(ThingCategoryDefOf.Drugs) && p.IsPrisoner)
            {
                Log.Message(p.Name + " is looking for food and isprisoner=" + p.IsPrisoner + " item=" + food.label);
                Log.Message("got into that new if that I made (thingdef)");
                //DrugAIUtility.IngestAndTakeToInventoryJob(GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForDef(food), Verse.AI.PathEndMode.ClosestTouch, TraverseParms.For(p)), p);
                return true;
            }
            if (!p.RaceProps.CanEverEat(food))
            {
                return false;
            }
            if (p.foodRestriction != null)
            {
                FoodRestriction currentRespectedRestriction = p.foodRestriction.GetCurrentRespectedRestriction(getter);
                if (currentRespectedRestriction != null && !currentRespectedRestriction.Allows(food) && food.IsWithinCategory(currentRespectedRestriction.filter.DisplayRootCategory.catDef))
                {
                    return false;
                }
            }
            if (careIfNotAcceptableForTitle && FoodUtility.InappropriateForTitle(food, p, allowIfStarving: true))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FoodUtility))]
    [HarmonyPatch(nameof(FoodUtility.WillEat))]
    [HarmonyPatch(typeof(FoodUtility), "WillEat", new[] { typeof(Pawn), typeof(Thing), typeof(Pawn), typeof(bool) })]
    static class thingThatAllowsPrisonersToEatDrugsThing
    {
        static bool Prefix(this Pawn p, Thing food, Pawn getter = null, bool careIfNotAcceptableForTitle = true)
        {
            if (p.IsPrisoner) Log.Message("a prisoner is looking for food");
            if (food.def.IsWithinCategory(ThingCategoryDefOf.Drugs) && p.IsPrisoner)
            {
                Log.Message(p.Name + " is looking for food and isprisoner=" + p.IsPrisoner + " item=" + food.Label);
                Log.Message("got into that new if that I made (thing mode)");
                Job doDrugs = DrugAIUtility.IngestAndTakeToInventoryJob(food, p);
                doDrugs.count = 2;
                p.jobs.curDriver.EndJobWith(JobCondition.Succeeded);
                p.jobs.jobQueue.EnqueueLast(doDrugs);
                return true;
            }
            if (!p.RaceProps.CanEverEat(food))
            {
                return false;
            }
            if (p.foodRestriction != null)
            {
                FoodRestriction currentRespectedRestriction = p.foodRestriction.GetCurrentRespectedRestriction(getter);
                if (currentRespectedRestriction != null && !currentRespectedRestriction.Allows(food) && (food.def.IsWithinCategory(ThingCategoryDefOf.Foods) || food.def.IsWithinCategory(ThingCategoryDefOf.Corpses)))
                {
                    return false;
                }
            }
            if (careIfNotAcceptableForTitle && FoodUtility.InappropriateForTitle(food.def, p, allowIfStarving: true))
            {
                return false;
            }
            return true;
        }
    }*/

}
