﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RC.Engine.Simulator.Core
{
    /// <summary>
    /// Constants defined for reading and writing metadata XML descriptors.
    /// </summary>
    static class XmlMetadataConstants
    {
        public const string SIMMETADATA_ELEM = "simulationMetadata";
        public const string BUILDINGTYPE_ELEM = "buildingType";
        public const string ADDONTYPE_ELEM = "addonType";
        public const string UNITTYPE_ELEM = "unitType";
        public const string UPGRADETYPE_ELEM = "upgradeType";
        public const string TYPE_NAME_ATTR = "name";
        public const string GENERALDATA_ELEM = "generalData";
        public const string COSTS_ELEM = "costs";
        public const string GROUNDWEAPON_ELEM = "groundWeapon";
        public const string AIRWEAPON_ELEM = "airWeapon";
        public const string CREATEDIN_ELEM = "createdIn";
        public const string MAINBUILDING_ELEM = "mainBuilding";
        public const string RESEARCHEDIN_ELEM = "researchedIn";
        public const string PREVIOUSLEVEL_ELEM = "previousLevel";
        public const string REQUIRES_ELEM = "requires";
        public const string SPRITE_ELEM = "sprite";
        public const string SPRITE_IMAGE_ATTR = "image";
        public const string SPRITE_TRANSPCOLOR_ATTR = "transparentColor";
        public const string SPRITE_OWNERMASKCOLOR_ATTR = "ownerMaskColor";
        public const string FRAME_ELEM = "frame";
        public const string FRAME_NAME_ATTR = "name";
        public const string FRAME_SOURCEREGION_ATTR = "sourceRegion";
        public const string FRAME_OFFSET_ATTR = "offset";
        public const string GENDATA_AREA_ELEM = "area";
        public const string GENDATA_ARMOR_ELEM = "armor";
        public const string GENDATA_MAXENERGY_ELEM = "maxEnergy";
        public const string GENDATA_MAXHP_ELEM = "maxHP";
        public const string GENDATA_SIGHTRANGE_ELEM = "sightRange";
        public const string GENDATA_SIZE_ELEM = "size";
        public const string GENDATA_SPEED_ELEM = "speed";
        public const string COSTS_BUILDTIME_ELEM = "buildTime";
        public const string COSTS_FOODCOST_ELEM = "food";
        public const string COSTS_GASCOST_ELEM = "gas";
        public const string COSTS_MINERALCOST_ELEM = "mineral";
        public const string WPN_COOLDOWN_ELEM = "cooldown";
        public const string WPN_DAMAGE_ELEM = "damage";
        public const string WPN_DAMAGETYPE_ELEM = "damageType";
        public const string WPN_INCREMENT_ELEM = "increment";
        public const string WPN_RANGEMAX_ELEM = "rangeMax";
        public const string WPN_RANGEMIN_ELEM = "rangeMin";
        public const string WPN_SPLASHTYPE_ELEM = "splashType";
    }
}