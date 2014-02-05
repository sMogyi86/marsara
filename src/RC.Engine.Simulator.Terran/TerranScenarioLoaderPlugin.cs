﻿using RC.Common.ComponentModel;
using RC.Engine.Simulator.ComponentInterfaces;
using RC.Engine.Simulator.Scenarios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Engine.Simulator.Terran.Buildings;
using RC.Engine.Simulator.PublicInterfaces;
using RC.Engine.Simulator.Terran.Units;
using RC.Common;

namespace RC.Engine.Simulator.Terran
{
    /// <summary>
    /// This class represents the scenario loader plugin for the Terran race.
    /// </summary>
    [Plugin(typeof(IScenarioLoader))]
    class TerranScenarioLoaderPlugin : IPlugin<IScenarioLoaderPluginInstall>
    {
        /// <summary>
        /// Constructs a TerranScenarioLoaderPlugin instance.
        /// </summary>
        public TerranScenarioLoaderPlugin()
        {
        }

        /// <see cref="IPlugin<T>.Install"/>
        public void Install(IScenarioLoaderPluginInstall extendedComponent)
        {
            /// TODO: Write installation code here!
            extendedComponent.RegisterPlayerInitializer(RaceEnum.Terran, this.TerranInitializer);
        }

        /// <see cref="IPlugin<T>.Uninstall"/>
        public void Uninstall(IScenarioLoaderPluginInstall extendedComponent)
        {
            /// TODO: Write uninstallation code here!
        }

        /// <see cref="Player.Initializer"/>
        private void TerranInitializer(Player player)
        {
            if (player == null) { throw new ArgumentNullException("player"); }

            Scenario scenario = player.StartLocation.Scenario;
            CommandCenter commandCenter = new CommandCenter(player.StartLocation.QuadCoords);
            scenario.AddEntity(commandCenter);
            player.AddBuilding(commandCenter);

            SCV[] scvList = new SCV[12];
            RCIntVector cmdCenterTopLeft = commandCenter.Scenario.Map.GetQuadTile(commandCenter.QuadCoords).GetCell(new RCIntVector(0, 0)).MapCoords;
            for (int i = 0; i < scvList.Length; i++)
            {
                SCV scv = new SCV();
                scenario.AddEntity(scv);
                player.AddUnit(scv);
                scv.AddToMap(new RCNumVector(cmdCenterTopLeft + SCV_POSITIONS[i]));
            }
        }

        private static RCIntVector[] SCV_POSITIONS = new RCIntVector[12]
        {
            new RCIntVector(1, 14),
            new RCIntVector(4, 14),
            new RCIntVector(7, 14),
            new RCIntVector(10, 14),
            new RCIntVector(13, 14),
            new RCIntVector(1, 17),
            new RCIntVector(4, 17),
            new RCIntVector(7, 17),
            new RCIntVector(10, 17),
            new RCIntVector(13, 17),
            new RCIntVector(6, 20),
            new RCIntVector(9, 20),
        };
    }
}