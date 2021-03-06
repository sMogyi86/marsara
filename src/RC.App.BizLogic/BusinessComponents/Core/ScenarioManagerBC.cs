﻿using System;
using System.IO;
using RC.App.BizLogic.Services.Core;
using RC.App.BizLogic.Views;
using RC.App.BizLogic.Views.Core;
using RC.Common.ComponentModel;
using RC.Common.Diagnostics;
using RC.Engine.Maps.ComponentInterfaces;
using RC.Engine.Maps.PublicInterfaces;
using RC.Engine.Simulator.ComponentInterfaces;
using RC.Engine.Simulator.Engine;
using RC.Engine.Simulator.Metadata;
using RC.Engine.Simulator.MotionControl;
using RC.Engine.Pathfinder.PublicInterfaces;

namespace RC.App.BizLogic.BusinessComponents.Core
{
    /// <summary>
    /// The implementation of the ScenarioManager business component.
    /// </summary>
    [Component("RC.App.BizLogic.ScenarioManagerBC")]
    class ScenarioManagerBC : IScenarioManagerBC, IComponent
    {
        #region IComponent methods

        /// <see cref="IComponent.Start"/>
        public void Start()
        {
            this.viewFactoryRegistry = ComponentManager.GetInterface<IViewFactoryRegistry>();
            this.tilesetManager = ComponentManager.GetInterface<ITilesetManagerBC>();
            this.mapLoader = ComponentManager.GetInterface<IMapLoader>();
            this.scenarioLoader = ComponentManager.GetInterface<IScenarioLoader>();
            this.pathfinder = ComponentManager.GetInterface<IPathfinder>();
        }

        /// <see cref="IComponent.Stop"/>
        public void Stop()
        {
            this.ActiveScenarioChanged = null;
            this.CloseScenario();

            this.viewFactoryRegistry = null;
            this.tilesetManager = null;
            this.mapLoader = null;
            this.scenarioLoader = null;
            this.pathfinder = null;
        }

        #endregion IComponent methods

        #region IScenarioManagerBC methods

        /// <see cref="IScenarioManagerBC.NewScenario"/>
        public void NewScenario(string mapName, string tilesetName, string defaultTerrain, Common.RCIntVector mapSize)
        {
            if (this.activeScenario != null) { throw new InvalidOperationException("Another scenario is currently active!"); }

            IMapAccess map = this.mapLoader.NewMap(mapName, this.tilesetManager.GetTileSet(tilesetName), defaultTerrain, mapSize);
            this.activeScenario = this.scenarioLoader.NewScenario(map);

            this.RegisterFactoryMethods();

            if (this.ActiveScenarioChanged != null) { this.ActiveScenarioChanged(this.activeScenario); }
        }

        /// <see cref="IScenarioManagerBC.OpenScenario"/>
        public void OpenScenario(string filename)
        {
            if (this.activeScenario != null) { throw new InvalidOperationException("Another scenario is currently active!"); }
            if (filename == null) { throw new ArgumentNullException("fileName"); }

            byte[] mapBytes = File.ReadAllBytes(filename);
            MapHeader mapHeader = this.mapLoader.LoadMapHeader(mapBytes);
            IMapAccess map = this.mapLoader.LoadMap(this.tilesetManager.GetTileSet(mapHeader.TilesetName), mapBytes);
            this.pathfinder.Initialize(new MapWalkabilityReader(map), MAX_MOVING_ENTITY_SIZE);
            this.activeScenario = this.scenarioLoader.LoadScenario(map, mapBytes);

            this.RegisterFactoryMethods();

            if (this.ActiveScenarioChanged != null) { this.ActiveScenarioChanged(this.activeScenario); }
        }

        /// <see cref="IScenarioManagerBC.SaveScenario"/>
        public void SaveScenario(string filename)
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no active scenario!"); }
            if (filename == null) { throw new ArgumentNullException("fileName"); }

            /// Serialize the map and the scenario.
            byte[] mapBytes = this.mapLoader.SaveMap(this.activeScenario.Map);
            byte[] scenarioBytes = this.scenarioLoader.SaveScenario(this.activeScenario);

            /// Write the serialized data into the output file.
            int outIdx = 0;
            byte[] outputBytes = new byte[mapBytes.Length + scenarioBytes.Length];
            for (int i = 0; i < mapBytes.Length; i++, outIdx++) { outputBytes[outIdx] = mapBytes[i]; }
            for (int i = 0; i < scenarioBytes.Length; i++, outIdx++) { outputBytes[outIdx] = scenarioBytes[i]; }
            File.WriteAllBytes(filename, outputBytes);
        }

        /// <see cref="IScenarioManagerBC.CloseScenario"/>
        public void CloseScenario()
        {
            if (this.activeScenario != null)
            {
                this.UnregisterFactoryMethods();

                IMapAccess map = this.activeScenario.Map;
                this.activeScenario.Dispose();
                map.Close();
                this.activeScenario = null;

                if (this.ActiveScenarioChanged != null) { this.ActiveScenarioChanged(this.activeScenario); }
            }
        }

        /// <see cref="IScenarioManagerBC.ActiveScenario"/>
        public Scenario ActiveScenario { get { return this.activeScenario; } }

        /// <see cref="IScenarioManagerBC.Metadata"/>
        public IScenarioMetadata Metadata { get { return this.scenarioLoader.Metadata; } }

        /// <see cref="IScenarioManagerBC.ActiveScenarioChanged"/>
        public event Action<Scenario> ActiveScenarioChanged; 

        #endregion IScenarioManagerBC methods

        #region View factory methods

        /// <summary>
        /// Creates a view of type IMapTerrainView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMapTerrainView CreateMapTerrainView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no active scenario!"); }
            return new MapTerrainView();
        }

        /// <summary>
        /// Creates a view of type IFogOfWarView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IFogOfWarView CreateFogOfWarView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no active scenario!"); }
            return new FogOfWarView();
        }

        /// <summary>
        /// Creates a view of type ITileSetView.
        /// </summary>
        /// <returns>The created view.</returns>
        private ITileSetView CreateTileSetView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no active scenario!"); }
            return new TileSetView();
        }

        /// <summary>
        /// Creates a view of type IMetadataView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMetadataView CreateMetadataView()
        {
            return new MetadataView();
        }

        /// <summary>
        /// Creates a view of type IMapObjectView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMapObjectView CreateMapObjectView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no active scenario!"); }
            return new MapObjectView();
        }

        /// <summary>
        /// Creates a view of type IMapObjectDetailsView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMapObjectDetailsView CreateMapObjectDetailsView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no opened map!"); }

            return new MapObjectDetailsView();
        }

        /// <summary>
        /// Creates a view of type INormalModeObjectPlacementView.
        /// </summary>
        /// <returns>The created view.</returns>
        private INormalModeMapObjectPlacementView CreateNormalModeObjectPlacementView()
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no opened map!"); }

            return new NormalModeObjectPlacementView();
        }

        /// <summary>
        /// Creates a view of type IMapEditorModeObjectPlacementView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMapEditorModeObjectPlacementView CreateMapEditorModeObjectPlacementView(string buildingTypeName)
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no opened map!"); }
            if (buildingTypeName == null) { throw new ArgumentNullException("buildingTypeName"); }

            return new MapEditorModeObjectPlacementView(buildingTypeName);
        }

        /// <summary>
        /// Creates a view of type ITerrainObjectPlacementView.
        /// </summary>
        /// <returns>The created view.</returns>
        private ITerrainObjectPlacementView CreateTerrainObjectPlacementView(string terrainObjectName)
        {
            if (this.activeScenario == null) { throw new InvalidOperationException("There is no opened map!"); }
            if (terrainObjectName == null) { throw new ArgumentNullException("terrainObjectName"); }

            return new TerrainObjectPlacementView(terrainObjectName);
        }

        /// <summary>
        /// Creates a view of type ISelectionIndicatorView.
        /// </summary>
        /// <returns>The created view.</returns>
        private ISelectionIndicatorView CreateSelIndicatorView()
        {
            return new SelectionIndicatorView();
        }

        /// <summary>
        /// Creates a view of type ICommandView.
        /// </summary>
        /// <returns>The created view.</returns>
        private ICommandView CreateCommandView()
        {
            return new CommandView();
        }

        /// <summary>
        /// Creates a view of type IMinimapView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IMinimapView CreateMinimapView()
        {
            return new MinimapView();
        }

        /// <summary>
        /// Creates a view of type ISelectionDetailsView.
        /// </summary>
        /// <returns>The created view.</returns>
        private ISelectionDetailsView CreateSelectionDetailsView()
        {
            return new SelectionDetailsView();
        }

        /// <summary>
        /// Creates a view of type IProductionDetailsView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IProductionDetailsView CreateProductionDetailsView()
        {
            return new ProductionDetailsView();
        }

        /// <summary>
        /// Creates a view of type IPlayerView.
        /// </summary>
        /// <returns>The created view.</returns>
        private IPlayerView CreatePlayerView()
        {
            return new PlayerView();
        }

        /// <summary>
        /// Registers the implemented factory methods to the view factory.
        /// </summary>
        private void RegisterFactoryMethods()
        {
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateMapTerrainView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateFogOfWarView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateTileSetView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateMetadataView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateMapObjectView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateSelIndicatorView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateCommandView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateMinimapView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateSelectionDetailsView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateMapObjectDetailsView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateProductionDetailsView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreatePlayerView);
            this.viewFactoryRegistry.RegisterViewFactory(this.CreateNormalModeObjectPlacementView);
            this.viewFactoryRegistry.RegisterViewFactory<IMapEditorModeObjectPlacementView, string>(this.CreateMapEditorModeObjectPlacementView);
            this.viewFactoryRegistry.RegisterViewFactory<ITerrainObjectPlacementView, string>(this.CreateTerrainObjectPlacementView);
        }

        /// <summary>
        /// Unregisters the implemented factory methods from the view factory.
        /// </summary>
        private void UnregisterFactoryMethods()
        {
            this.viewFactoryRegistry.UnregisterViewFactory<IMapTerrainView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IFogOfWarView>();
            this.viewFactoryRegistry.UnregisterViewFactory<ITileSetView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IMetadataView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IMapObjectView>();
            this.viewFactoryRegistry.UnregisterViewFactory<ISelectionIndicatorView>();
            this.viewFactoryRegistry.UnregisterViewFactory<ICommandView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IMinimapView>();
            this.viewFactoryRegistry.UnregisterViewFactory<ISelectionDetailsView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IMapObjectDetailsView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IProductionDetailsView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IPlayerView>();
            this.viewFactoryRegistry.UnregisterViewFactory<INormalModeMapObjectPlacementView>();
            this.viewFactoryRegistry.UnregisterViewFactory<IMapEditorModeObjectPlacementView>();
            this.viewFactoryRegistry.UnregisterViewFactory<ITerrainObjectPlacementView>();
        }

        #endregion View factory methods

        /// <summary>
        /// Reference to the currently active scenario.
        /// </summary>
        private Scenario activeScenario;

        /// <summary>
        /// Reference to the RC.Engine.Maps.MapLoader component.
        /// </summary>
        private IMapLoader mapLoader;

        /// <summary>
        /// Reference to the RC.Engine.Scenarios.ScenarioLoader component.
        /// </summary>
        private IScenarioLoader scenarioLoader;

        /// <summary>
        /// Reference to the RC.App.BizLogic.TilesetManagerBC business component.
        /// </summary>
        private ITilesetManagerBC tilesetManager;

        /// <summary>
        /// Reference to the RC.Engine.Pathfinder component.
        /// </summary>
        private IPathfinder pathfinder;

        /// <summary>
        /// Reference to the registry interface of the RC.App.BizLogic.ViewFactory component.
        /// </summary>
        private IViewFactoryRegistry viewFactoryRegistry;

        /// <summary>
        /// The maximum size of moving entities can be handled by the RC.Engine.Pathfinder component.
        /// </summary>
        private const int MAX_MOVING_ENTITY_SIZE = 4;
    }
}
