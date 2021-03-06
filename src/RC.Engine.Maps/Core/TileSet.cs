﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;
using RC.Engine.Maps.PublicInterfaces;

namespace RC.Engine.Maps.Core
{
    /// <summary>
    /// This class represents a tileset. The maps are built up from isometric tiles and terrain
    /// objects which are defined by tilesets as well as their relationships.
    /// </summary>
    class TileSet : ITileSet
    {
        /// <summary>
        /// Constructs a TileSet with the given name.
        /// </summary>
        /// <param name="name">The name of the new TileSet.</param>
        public TileSet(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }

            this.name = name;
            this.isFinalized = false;
            this.terrainTypes = new Dictionary<string, TerrainType>();
            this.simpleTileTypes = new Dictionary<string, IsoTileType>();
            this.terrainObjectTypes = new Dictionary<string, TerrainObjectType>();
            this.mixedTileTypes = new Dictionary<Tuple<string, string, TerrainCombination>, IsoTileType>();
            this.allTileVariants = new RCSet<IsoTileVariant>();
            this.allTileVariantList = new List<IsoTileVariant>();
            this.allTerrainObjectList = new List<TerrainObjectType>();
        }

        #region ITileSet methods

        /// <see cref="ITileSet.GetTerrainType"/>
        public ITerrainType GetTerrainType(string name)
        {
            return this.GetTerrainTypeImpl(name);
        }

        /// <see cref="ITileSet.GetIsoTileType"/>
        public IIsoTileType GetIsoTileType(string terrainType)
        {
            return this.GetIsoTileTypeImpl(terrainType);
        }

        /// <see cref="ITileSet.GetIsoTileType"/>
        public IIsoTileType GetIsoTileType(string terrainTypeA, string terrainTypeB, TerrainCombination combination)
        {
            return this.GetIsoTileTypeImpl(terrainTypeA, terrainTypeB, combination);
        }

        /// <see cref="ITileSet.GetTerrainObjectType"/>
        public ITerrainObjectType GetTerrainObjectType(string name)
        {
            return this.GetTerrainObjectTypeImpl(name);
        }

        /// <see cref="ITileSet.TerrainTypes"/>
        public IEnumerable<ITerrainType> TerrainTypes { get { return this.terrainTypes.Values; } }

        /// <see cref="ITileSet.TerrainObjectTypes"/>
        public IEnumerable<ITerrainObjectType> TerrainObjectTypes { get { return this.allTerrainObjectList; } }

        /// <see cref="ITileSet.TileVariants"/>
        public IEnumerable<IIsoTileVariant> TileVariants { get { return this.allTileVariantList; } }

        /// <see cref="ITileSet.Name"/>
        public string Name { get { return this.name; } }

        #endregion ITileSet methods

        #region Internal public methods

        /// <see cref="ITileSet.GetTerrainType"/>
        public TerrainType GetTerrainTypeImpl(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (!this.terrainTypes.ContainsKey(name)) { throw new TileSetException(string.Format("TerrainType with name '{0}' doesn't exist!", name)); }

            return this.terrainTypes[name];
        }

        /// <see cref="ITileSet.GetIsoTileType"/>
        public IsoTileType GetIsoTileTypeImpl(string terrainType)
        {
            if (terrainType == null) { throw new ArgumentNullException("terrainType"); }
            if (!this.simpleTileTypes.ContainsKey(terrainType)) { throw new TileSetException(string.Format("Simple tile type for terrain type '{0}' doesn't exist!", terrainType)); }

            return this.simpleTileTypes[terrainType];
        }

        /// <see cref="ITileSet.GetIsoTileType"/>
        public IsoTileType GetIsoTileTypeImpl(string terrainTypeA, string terrainTypeB, TerrainCombination combination)
        {
            if (terrainTypeA == null) { throw new ArgumentNullException("terrainTypeA"); }
            if (terrainTypeB == null) { throw new ArgumentNullException("terrainTypeB"); }
            if (combination == TerrainCombination.Simple) { throw new ArgumentException("combination", "Invalid combination for a mixed tile type!"); }

            Tuple<string, string, TerrainCombination> key = new Tuple<string, string, TerrainCombination>(terrainTypeA, terrainTypeB, combination);
            if (!this.mixedTileTypes.ContainsKey(key)) { throw new TileSetException(string.Format("Mixed tile type for terrain types '{0}' and '{1}' with combination '{2}' doesn't exist!", terrainTypeA, terrainTypeB, combination)); }

            return this.mixedTileTypes[key];
        }

        /// <see cref="ITileSet.GetTerrainObjectType"/>
        public TerrainObjectType GetTerrainObjectTypeImpl(string name)
        {
            if (name == null) { throw new ArgumentNullException("name"); }
            if (!this.terrainObjectTypes.ContainsKey(name)) { throw new TileSetException(string.Format("TerrainObjectType with name '{0}' doesn't exist!", name)); }

            return this.terrainObjectTypes[name];
        }

        #endregion Internal public methods

        #region Internal buildup methods

        /// <summary>
        /// Creates a terrain type with the given name.
        /// </summary>
        /// <param name="name">The name of the new terrain type.</param>
        public void CreateTerrainType(string name)
        {
            if (this.isFinalized) { throw new InvalidOperationException("It is not possible to create new terrain type for a finalized TileSet!"); }
            if (name == null) { throw new ArgumentNullException("name"); }
            if (this.terrainTypes.ContainsKey(name)) { throw new TileSetException(string.Format("Terrain type '{0}' already created!", name)); }

            this.terrainTypes.Add(name, new TerrainType(name, this));
        }

        /// <summary>
        /// Creates a simple tile type for the given terrain type.
        /// </summary>
        /// <param name="terrainType">The name of the terrain type.</param>
        public void CreateSimpleTileType(string terrainType)
        {
            if (this.isFinalized) { throw new InvalidOperationException("It is not possible to create new tile type for a finalized TileSet!"); }
            if (terrainType == null) { throw new ArgumentNullException("terrainType"); }
            if (this.simpleTileTypes.ContainsKey(terrainType)) { throw new TileSetException(string.Format("Simple tile type for terrain type '{0}' already exists!", terrainType)); }

            this.simpleTileTypes.Add(terrainType, new IsoTileType(terrainType, this));
        }

        /// <summary>
        /// Creates a mixed tile type for the given terrain types.
        /// </summary>
        /// <param name="terrainTypeA">The name of the first terrain type.</param>
        /// <param name="terrainTypeB">The name of the second terrain type.</param>
        /// <param name="combination">The combination of the terrain types in the new mixed tile type.</param>
        /// <remarks>Terrain type A must be the parent of terrain type B.</remarks>
        public void CreateMixedTileType(string terrainTypeA, string terrainTypeB, TerrainCombination combination)
        {
            if (this.isFinalized) { throw new InvalidOperationException("It is not possible to create new tile type for a finalized TileSet!"); }
            if (terrainTypeA == null) { throw new ArgumentNullException("terrainTypeA"); }
            if (terrainTypeB == null) { throw new ArgumentNullException("terrainTypeB"); }
            if (combination == TerrainCombination.Simple) { throw new ArgumentException("combination", "Invalid combination for a mixed tile type!"); }

            Tuple<string, string, TerrainCombination> key = new Tuple<string, string, TerrainCombination>(terrainTypeA, terrainTypeB, combination);
            if (this.mixedTileTypes.ContainsKey(key)) { throw new TileSetException(string.Format("Mixed tile type for terrain types '{0}' and '{1}' with combination '{2}' already exists!", terrainTypeA, terrainTypeB, combination)); }

            IsoTileType newTile = new IsoTileType(terrainTypeA, terrainTypeB, combination, this);
            this.mixedTileTypes.Add(key, newTile);
        }

        /// <summary>
        /// Creates a TerrainObjectType.
        /// </summary>
        /// <param name="name">The name of the TerrainObjectType.</param>
        /// <param name="imgData">The byte sequence that contains the image data of the TerrainObjectType.</param>
        /// <param name="quadSize">The size of the TerrainObjectType in quadratic tiles.</param>
        /// <param name="transparentColor">The transparent color of the TerrainObjectType.</param>
        public void CreateTerrainObjectType(string name, byte[] imgData, RCIntVector quadSize, RCColor transparentColor)
        {
            if (this.isFinalized) { throw new InvalidOperationException("It is not possible to create new TerrainObjectType for a finalized TileSet!"); }
            if (name == null) { throw new ArgumentNullException("name"); }
            if (this.terrainObjectTypes.ContainsKey(name)) { throw new TileSetException(string.Format("TerrainObjectType with name '{0}' already exists!", name)); }

            TerrainObjectType newTerrainObject = new TerrainObjectType(name, imgData, quadSize, transparentColor, this);
            newTerrainObject.SetIndex(this.allTerrainObjectList.Count);
            this.terrainObjectTypes.Add(name, newTerrainObject);
            this.allTerrainObjectList.Add(newTerrainObject);
        }

        /// <summary>
        /// Registers the given variant with this tileset.
        /// </summary>
        /// <param name="variant">The variant to register.</param>
        public void RegisterVariant(IsoTileVariant variant)
        {
            if (this.isFinalized) { throw new InvalidOperationException("It is not possible to register tile variant for a finalized TileSet!"); }
            if (variant == null) { throw new ArgumentNullException("variant"); }
            if (variant.Tileset != this) { throw new InvalidOperationException("The variant is in another tileset!"); }
            if (this.allTileVariants.Contains(variant)) { throw new TileSetException("The variant has already been registered!"); }

            variant.SetIndex(this.allTileVariants.Count);
            this.allTileVariants.Add(variant);
            this.allTileVariantList.Add(variant);
        }

        /// <summary>
        /// Check and finalize the TileSet object. Buildup methods will be unavailable after calling this method.
        /// </summary>
        public void CheckAndFinalize()
        {
            /// Check whether the terrain tree has only one root.
            TerrainType root = null;
            foreach (TerrainType terrain in this.terrainTypes.Values)
            {
                TerrainType current = terrain;
                while (current.Parent != null)
                {
                    current = current.GetParentImpl();
                }

                if (root == null)
                {
                    root = current;                    
                }
                else if (root != current)
                {
                    throw new TileSetException("The terrain tree must have only one root!");
                }

                terrain.CheckAndFinalize();
            }

            /// Check the simple tile type objects.
            foreach (IsoTileType simpleTileType in this.simpleTileTypes.Values)
            {
                simpleTileType.CheckAndFinalize();
            }

            /// Check the mixed tile type objects.
            foreach (IsoTileType mixedTileType in this.mixedTileTypes.Values)
            {
                mixedTileType.CheckAndFinalize();
            }

            this.isFinalized = true;
        }

        /// <summary>
        /// Gets whether this tileset is finalized.
        /// </summary>
        public bool IsFinalized { get { return this.isFinalized; } }

        #endregion Internal buildup methods

        /// <summary>
        /// The name of this tileset.
        /// </summary>
        private string name;

        /// <summary>
        /// List of the terrain types of this tileset mapped by their name.
        /// </summary>
        private Dictionary<string, TerrainType> terrainTypes;

        /// <summary>
        /// List of the terrain object types mapped by their name.
        /// </summary>
        private Dictionary<string, TerrainObjectType> terrainObjectTypes;

        /// <summary>
        /// List of the simple tile types mapped by the names of the corresponding terrain types.
        /// </summary>
        private Dictionary<string, IsoTileType> simpleTileTypes;

        /// <summary>
        /// List of the mixed tile types mapped by the names of the corresponding terrain types and the combinations.
        /// </summary>
        private Dictionary<Tuple<string, string, TerrainCombination>, IsoTileType> mixedTileTypes;

        /// <summary>
        /// Set of all tile variants defined by this tileset.
        /// </summary>
        private RCSet<IsoTileVariant> allTileVariants;

        /// <summary>
        /// List of all tile variants defined by this tileset.
        /// </summary>
        private List<IsoTileVariant> allTileVariantList;

        /// <summary>
        /// List of all terrain objects defined by this tileset.
        /// </summary>
        private List<TerrainObjectType> allTerrainObjectList;

        /// <summary>
        /// Becomes true when this TileSet is finalized.
        /// </summary>
        private bool isFinalized;
    }
}
