﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.App.BizLogic.PublicInterfaces;
using RC.Engine.Maps.PublicInterfaces;
using RC.Common;
using RC.Engine.Simulator.PublicInterfaces;

namespace RC.App.BizLogic.Core
{
    /// <summary>
    /// Implementation of views on the objects of the currently opened map.
    /// </summary>
    class MapObjectView : MapViewBase, IMapObjectView
    {
        /// <summary>
        /// Constructs a MapObjectView instance.
        /// </summary>
        /// <param name="map">The subject of this view.</param>
        public MapObjectView(IMapAccess map, IMapContentManager<IGameObject> gameObjects)
            : base(map)
        {
            if (gameObjects == null) { throw new ArgumentNullException("gameObjects"); }
            this.gameObjects = gameObjects;
            this.selectedObjects = new HashSet<IGameObject>();
        }

        #region IMapObjectView methods

        /// <see cref="IMapObjectView.GetVisibleMapObjects"/>
        public List<MapObjectInstance> GetVisibleMapObjects(RCIntRectangle displayedArea)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow;
            RCIntVector displayOffset;
            this.CalculateCellWindow(displayedArea, out cellWindow, out displayOffset);

            List<MapObjectInstance> retList = new List<MapObjectInstance>();
            HashSet<IGameObject> visibleGameObjects = this.gameObjects.GetContents(
                new RCNumRectangle(cellWindow.X - (RCNumber)1 / (RCNumber)2,
                                   cellWindow.Y - (RCNumber)1 / (RCNumber)2,
                                   cellWindow.Width,
                                   cellWindow.Height));
            foreach (IGameObject gameObj in visibleGameObjects)
            {
                retList.Add(new MapObjectInstance()
                {
                    Sprite = new MapSpriteInstance() { Index = -1, DisplayCoords = RCIntVector.Undefined, Section = RCIntRectangle.Undefined },
                    SelectionIndicatorColorIdx = this.selectedObjects.Contains(gameObj) ? 0 : 1,
                    SelectionIndicator = (RCIntRectangle)((gameObj.Position - cellWindow.Location) * new RCNumVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL)) - displayOffset,
                    Values = null
                });
            }
            return retList;
        }

        /// <see cref="IMapObjectView.SelectObjects"/>
        public void SelectObjects(RCIntRectangle displayedArea, RCIntRectangle selectionBox)
        {
            if (displayedArea == RCIntRectangle.Undefined) { throw new ArgumentNullException("displayedArea"); }
            if (!new RCIntRectangle(0, 0, this.MapSize.X, this.MapSize.Y).Contains(displayedArea)) { throw new ArgumentOutOfRangeException("displayedArea"); }

            RCIntRectangle cellWindow;
            RCIntVector displayOffset;
            this.CalculateCellWindow(displayedArea, out cellWindow, out displayOffset);

            RCNumRectangle selectionBoxOnMap = ((RCNumRectangle)(selectionBox + displayOffset) / new RCNumVector(BizLogicConstants.PIXEL_PER_NAVCELL, BizLogicConstants.PIXEL_PER_NAVCELL)) + cellWindow.Location;
            this.selectedObjects = this.gameObjects.GetContents(selectionBoxOnMap);
        }

        #endregion IMapObjectView methods

        /// <summary>
        /// Reference to the map content manager that contains the game objects of the scenario currently being simulated.
        /// </summary>
        private IMapContentManager<IGameObject> gameObjects;

        /// <summary>
        /// PROTOTYPE CODE
        /// TODO: Move selection handling to the engine!!!
        /// </summary>
        private HashSet<IGameObject> selectedObjects;
    }
}
