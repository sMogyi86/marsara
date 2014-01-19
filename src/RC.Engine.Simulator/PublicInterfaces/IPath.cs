﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common;
using RC.Engine.Maps.PublicInterfaces;

namespace RC.Engine.Simulator.PublicInterfaces
{
    /// <summary>
    /// The public interface of a path computed by the pathfinder component.
    /// </summary>
    public interface IPath
    {
        /// <summary>
        /// Gets whether the path is ready for use or not.
        /// </summary>
        bool IsReadyForUse { get; }

        /// <summary>
        /// Gets whether the given target node has been found or the pathfinding algorithm has reached it's iteration limit without finding the target node.
        /// </summary>
        bool IsTargetFound { get; }

        /// <summary>
        /// Aborts searching the path.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the path is ready for use or has already been aborted.</exception>
        void AbortSearch();

        /// <summary>
        /// Gets a section of the path.
        /// </summary>
        /// <param name="index">The index of the section to get.</param>
        /// <returns>The area of the section.</returns>
        /// <exception cref="InvalidOperationException">If the path is not ready for use or has already been aborted.</exception>
        RCIntRectangle this[int index] { get; }

        /// <summary>
        /// Gets the total number of sections on this path.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the path is not ready for use or has already been aborted.</exception>
        int Length { get; }

        /// <summary>
        /// Forgets every blocked edges that was used when the path was computed.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the path is not ready for use or has already been aborted.</exception>
        void ForgetBlockedEdges();
    }
}