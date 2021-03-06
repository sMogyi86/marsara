﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;

namespace RC.App.BizLogic.Views
{
    /// <summary>
    /// Interface of views of an object being placed onto the map.
    /// </summary>
    public interface IObjectPlacementView
    {
        /// <summary>
        /// Gets the object placement box at the given mouse position inside the displayed area.
        /// </summary>
        /// <param name="position">The position of the mouse pointer in pixels.</param>
        /// <returns>The object placement box to be displayed.</returns>
        ObjectPlacementBox GetObjectPlacementBox(RCIntVector position);

        /// <summary>
        /// Gets the suggestion boxes inside the displayed area.
        /// </summary>
        /// <returns>The suggestion boxes to be displayed.</returns>
        List<RCIntRectangle> GetSuggestionBoxes();

        /// <summary>
        /// Steps the preview animation of the object. If the object has no preview animation then this function has no effect.
        /// </summary>
        void StepPreviewAnimation();
    }

    /// <summary>
    /// Dummy interfaces needed to distinguish between the factory methods of the MapEditorModeObjectPlacementView,
    /// the NormalModeMapObjectPlacementView and the TerrainObjectPlacementView.
    /// </summary>
    public interface INormalModeMapObjectPlacementView : IObjectPlacementView { }
    public interface IMapEditorModeObjectPlacementView : IObjectPlacementView { }
    public interface ITerrainObjectPlacementView : IObjectPlacementView { }
}
