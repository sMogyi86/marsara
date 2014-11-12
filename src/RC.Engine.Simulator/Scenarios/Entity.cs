﻿using RC.Common;
using RC.Common.ComponentModel;
using RC.Engine.Maps.PublicInterfaces;
using RC.Engine.Simulator.ComponentInterfaces;
using RC.Engine.Simulator.PublicInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Engine.Simulator.MotionControl;

namespace RC.Engine.Simulator.Scenarios
{
    /// <summary>
    /// Scenario elements that have activities on the map.
    /// </summary>
    public abstract class Entity : HeapedObject, ISearchTreeContent, IMotionControlTarget
    {
        /// <summary>
        /// Constructs an entity instance.
        /// </summary>
        /// <param name="elementTypeName">The name of the element type of this entity.</param>
        public Entity(string elementTypeName)
        {
            if (elementTypeName == null) { throw new ArgumentNullException("elementTypeName"); }

            this.position = this.ConstructField<RCNumVector>("position");
            this.velocity = this.ConstructField<RCNumVector>("velocity");
            this.id = this.ConstructField<int>("id");
            this.typeID = this.ConstructField<int>("typeID");
            this.pathTracker = this.ConstructField<PathTrackerBase>("pathTracker");
            this.owner = this.ConstructField<Player>("owner");
            this.scenario = this.ConstructField<Scenario>("scenario");

            this.elementType = ComponentManager.GetInterface<IScenarioLoader>().Metadata.GetElementType(elementTypeName);
            this.scenario.Write(null);
            this.currentAnimations = new List<AnimationPlayer>();
            this.entityActuator = null;
            this.pathFinder = ComponentManager.GetInterface<IPathFinder>();

            this.position.Write(RCNumVector.Undefined);
            this.velocity.Write(new RCNumVector(0, 0));
            this.id.Write(-1);
            this.typeID.Write(this.elementType.ID);
            this.pathTracker.Write(null);
            this.owner.Write(null);
        }

        /// <summary>
        /// TODO: this is only a temporary solution for testing motion control.
        /// </summary>
        public void Move(RCNumVector toCoords)
        {
            if (this.pathTracker.Read() != null) { this.pathTracker.Read().TargetPosition = toCoords; }
        }

        #region Public interface

        /// <summary>
        /// Gets the ID of the entity.
        /// </summary>
        public IValueRead<int> ID { get { return this.id; } }

        /// <summary>
        /// Gets the position value of this entity.
        /// </summary>
        public IValueRead<RCNumVector> PositionValue { get { return this.position; } }

        /// <summary>
        /// Gets the metadata type definition of the entity.
        /// </summary>
        public IScenarioElementType ElementType { get { return this.elementType; } }

        /// <summary>
        /// Gets the players of the currently active animations of this entity.
        /// </summary>
        public IEnumerable<AnimationPlayer> CurrentAnimations { get { return this.currentAnimations; } }

        /// <summary>
        /// Gets the owner of this entity or null if this entity is neutral or is a start location.
        /// </summary>
        public Player Owner { get { return this.owner.Read(); } }

        /// <summary>
        /// Gets the scenario that this entity belongs to.
        /// </summary>
        public Scenario Scenario { get { return this.scenario.Read(); } }

        /// <summary>
        /// Gets the coordinates of the quadratic tiles that are currently visible by this entity.
        /// </summary>
        /// TODO: later the sight range will depend on the upgrades of the players!
        public IEnumerable<RCIntVector> VisibleQuadCoords
        {
            get
            {
                /// TODO: Take ground levels into account!
                RCIntVector quadCoord = this.Scenario.Map.GetCell(this.position.Read().Round()).ParentQuadTile.MapCoords;
                foreach (RCIntVector relativeQuadCoord in this.elementType.RelativeQuadCoordsInSight)
                {
                    RCIntVector absoluteQuadCoord = quadCoord + relativeQuadCoord;
                    if (absoluteQuadCoord.X >= 0 && absoluteQuadCoord.X < this.Scenario.Map.Size.X &&
                        absoluteQuadCoord.Y >= 0 && absoluteQuadCoord.Y < this.Scenario.Map.Size.Y)
                    {
                        yield return quadCoord + relativeQuadCoord;
                    }
                }
            }
        }

        /// <summary>
        /// Adds this entity to the map into the given position.
        /// </summary>
        /// <param name="position">The position of this entity on the map.</param>
        /// <returns>True if the entity was successfully added to the map; otherwise false.</returns>
        public bool AddToMap(RCNumVector position)
        {
            if (this.scenario.Read() == null) { throw new InvalidOperationException("The entity has not yet been added to a scenario!"); }
            if (this.position.Read() != RCNumVector.Undefined) { throw new InvalidOperationException("The entity has already been added to the map!"); }

            bool isValidPosition = this.ValidatePosition(position);
            if (isValidPosition)
            {
                this.SetPosition(position);
                this.Scenario.VisibleEntities.AttachContent(this);
            }
            return isValidPosition;
        }

        /// <summary>
        /// Removes this unit from the map.
        /// </summary>
        public RCNumVector RemoveFromMap()
        {
            if (this.scenario.Read() == null) { throw new InvalidOperationException("The entity has not yet been added to a scenario!"); }
            if (this.position.Read() == RCNumVector.Undefined) { throw new InvalidOperationException("The entity has already been removed from the map!"); }

            RCNumVector currentPosition = this.position.Read();
            this.Scenario.VisibleEntities.DetachContent(this);
            this.position.Write(RCNumVector.Undefined);
            return currentPosition;
        }

        #endregion Public interface

        #region Protected methods for the derived classes

        /// <summary>
        /// Sets the position of this entity.
        /// </summary>
        /// <param name="newPos">The new position of this entity.</param>
        protected void SetPosition(RCNumVector newPos)
        {
            if (newPos == RCNumVector.Undefined) { throw new ArgumentNullException("newPos"); }

            if (this.BoundingBoxChanging != null) { this.BoundingBoxChanging(this); }
            this.position.Write(newPos);
            if (this.BoundingBoxChanged != null) { this.BoundingBoxChanged(this); }
        }

        /// <summary>
        /// Sets the current animation of this entity with undefined direction.
        /// </summary>
        /// <param name="animationName">The name of the animation to play.</param>
        protected void SetCurrentAnimation(string animationName)
        {
            this.SetCurrentAnimation(animationName, MapDirection.Undefined);
        }

        /// <summary>
        /// Sets the current animation of this entity with the given direction.
        /// </summary>
        /// <param name="animationName">The name of the animation to play.</param>
        /// <param name="direction">The direction of the animation.</param>
        protected void SetCurrentAnimation(string animationName, MapDirection direction)
        {
            if (animationName == null) { throw new ArgumentNullException("animationName"); }
            this.SetCurrentAnimations(new List<string>() { animationName }, direction);
        }

        /// <summary>
        /// Sets the current animations of this entity with undefined direction.
        /// </summary>
        /// <param name="animationNames">The names of the animations to play.</param>
        protected void SetCurrentAnimations(List<string> animationNames)
        {
            this.SetCurrentAnimations(animationNames, MapDirection.Undefined);
        }

        /// <summary>
        /// Sets the current animations of this entity with the given direction.
        /// </summary>
        /// <param name="animationNames">The names of the animations to play.</param>
        /// <param name="direction">The direction of the animations.</param>
        protected void SetCurrentAnimations(List<string> animationNames, MapDirection direction)
        {
            if (animationNames == null) { throw new ArgumentNullException("animationNames"); }

            this.currentAnimations.Clear();
            foreach (string name in animationNames)
            {
                this.currentAnimations.Add(new AnimationPlayer(this.elementType.AnimationPalette.GetAnimation(name), direction));
            }
        }

        /// <summary>
        /// Sets a new actuator for this entity.
        /// </summary>
        /// <param name="actuator">The actuator to be set or null to dispose the current actuator.</param>
        /// <exception cref="InvalidOperationException">If this entity has already an actuator and the parameter is not null.</exception>
        protected void SetActuator(EntityActuatorBase actuator)
        {
            if (this.entityActuator != null && actuator != null) { throw new InvalidOperationException("The entity already has an actuator!"); }
            
            if (this.entityActuator != null) { this.entityActuator.Dispose(); }
            this.entityActuator = actuator;
        }

        /// <summary>
        /// Sets a new path-tracker for this entity.
        /// </summary>
        /// <param name="pathTracker">The path-tracker to be set or null to dispose the current path-tracker.</param>
        /// <exception cref="InvalidOperationException">If this entity has already a path-tracker and the parameter is not null.</exception>
        protected void SetPathTracker(PathTrackerBase pathTracker)
        {
            if (this.pathTracker.Read() != null && pathTracker != null) { throw new InvalidOperationException("The entity already has a path-tracker!"); }

            if (this.pathTracker.Read() != null) { this.pathTracker.Read().Dispose(); }
            this.pathTracker.Write(pathTracker);
        }

        /// <summary>
        /// Gets the velocity value of this entity.
        /// </summary>
        protected IValue<RCNumVector> VelocityValue { get { return this.velocity; } }

        #endregion Protected methods for the derived classes

        #region ISearchTreeContent members

        /// <see cref="ISearchTreeContent.BoundingBox"/>
        public RCNumRectangle BoundingBox
        {
            get { return new RCNumRectangle(this.position.Read() - this.ElementType.Area.Read() / 2, this.elementType.Area.Read()); }
        }

        /// <see cref="ISearchTreeContent.BoundingBoxChanging"/>
        public event ContentBoundingBoxChangeHdl BoundingBoxChanging;

        /// <see cref="ISearchTreeContent.BoundingBoxChanged"/>
        public event ContentBoundingBoxChangeHdl BoundingBoxChanged;

        #endregion ISearchTreeContent members

        #region IMotionControlTarget members

        /// <see cref="IMotionControlTarget.Position"/>
        public RCNumRectangle Position { get { return this.BoundingBox; } }

        /// <see cref="IMotionControlTarget.Velocity"/>
        public RCNumVector Velocity { get { return this.velocity.Read(); } }

        #endregion IMotionControlTarget members

        #region Internal members

        /// <summary>
        /// This method is called when this entity has been added to a scenario.
        /// </summary>
        /// <param name="scenario">The scenario where this entity is added to.</param>
        /// <param name="id">The ID of this entity.</param>
        internal void OnAddedToScenario(Scenario scenario, int id)
        {
            if (this.scenario.Read() != null) { throw new InvalidOperationException("The entity is already added to a scenario!"); }
            this.scenario.Write(scenario);
            this.id.Write(id);
            this.OnAddedToScenarioImpl();
        }

        /// <summary>
        /// This method is called when this entity has been removed from the scenario it is currently belongs to.
        /// </summary>
        internal void OnRemovedFromScenario()
        {
            if (this.scenario.Read() == null) { throw new InvalidOperationException("The entity doesn't not belong to a scenario!"); }
            if (this.position.Read() != RCNumVector.Undefined) { throw new InvalidOperationException("The entity is not removed from the map!"); }

            this.scenario.Write(null);
            this.id.Write(-1);
            this.OnRemovedFromScenarioImpl();
        }

        /// <summary>
        /// The method is called when this entity has been added to a player.
        /// </summary>
        /// <param name="owner">The player that owns this entity.</param>
        internal void OnAddedToPlayer(Player owner)
        {
            if (this.scenario.Read() == null) { throw new InvalidOperationException("The entity doesn't not belong to a scenario!"); }
            if (this.owner.Read() != null) { throw new InvalidOperationException("The entity is already added to a player!"); }
            this.owner.Write(owner);
        }

        /// <summary>
        /// The method is called when this entity has been removed from the player it is currently owned by.
        /// </summary>
        internal void OnRemovedFromPlayer()
        {
            if (this.scenario.Read() == null) { throw new InvalidOperationException("The entity doesn't not belong to a scenario!"); }
            if (this.owner.Read() == null) { throw new InvalidOperationException("The entity doesn't not belong to a player!"); }
            this.owner.Write(null);
        }

        /// <summary>
        /// Updates the state of this entity.
        /// </summary>
        internal void UpdateState()
        {
            /// Update the velocity and position of this entity only if it's visible on the map, has
            /// an actuator and a path-tracker and the path-tracker is active.
            if (this.scenario.Read().VisibleEntities.HasContent(this) &&
                this.entityActuator != null &&
                this.pathTracker.Read() != null &&
                this.pathTracker.Read().IsActive)
            {
                /// Update the state of the path-tracker of this entity.
                this.pathTracker.Read().Update();

                if (this.pathTracker.Read().IsActive)
                {
                    /// Update the velocity of this entity if the path-tracker is still active.
                    MotionController.UpdateVelocity(this, this.entityActuator, this.pathTracker.Read());
                }
                else
                {
                    /// Stop the entity if the path-tracker became inactive.
                    this.velocity.Write(new RCNumVector(0, 0));
                }

                /// If the velocity is not (0,0) update the position of this entity.
                if (this.velocity.Read() != new RCNumVector(0, 0))
                {
                    RCNumVector newPosition = this.position.Read() + this.velocity.Read();
                    if (this.ValidatePosition(newPosition)) { this.SetPosition(newPosition); }
                    else { this.velocity.Write(new RCNumVector(0, 0)); }
                }
            }

            /// Execute additional update procedures of the derived classes.
            this.UpdateStateImpl();
        }

        /// <summary>
        /// Checks whether the given position is valid for this entity.
        /// </summary>
        /// <param name="position">The position to be checked.</param>
        /// <returns>True if the given position is valid for this entity; otherwise false.</returns>
        /// <remarks>Must be overriden in the derived classes.</remarks>
        /// TODO: Make this method abstract and implement it in the derived classes! This is only a temporary solution.
        protected virtual bool ValidatePosition(RCNumVector position)
        {
            if (this.pathFinder.GetNavMeshNode(position) == null)
            {
                /// The entity wants to go to a non-walkable position on the map -> invalid position
                return false;
            }

            /// Detect collision with other entities.
            RCNumRectangle newEntityArea =
                new RCNumRectangle(position - this.ElementType.Area.Read() / 2, this.elementType.Area.Read());
            bool collision = false;
            foreach (Entity collidingEntity in this.scenario.Read().GetVisibleEntities<Entity>(newEntityArea))
            {
                if (collidingEntity != this) { collision = true; break; }
            }

            /// Return true if there is no collision with other entities.
            return !collision;
        }

        /// <summary>
        /// This method is called when this entity has been added to a scenario. Can be overriden in the derived classes.
        /// </summary>
        protected virtual void OnAddedToScenarioImpl() { }

        /// <summary>
        /// This method is called when this entity has been removed from it's scenario. Can be overriden in the derived classes.
        /// </summary>
        protected virtual void OnRemovedFromScenarioImpl() { }

        /// <summary>
        /// Derived classes can execute additional update procedures by overriding this method.
        /// The default implementation does nothing.
        /// </summary>
        protected virtual void UpdateStateImpl() { }

        /// <see cref="HeapedObject.DisposeImpl"/>
        protected override void DisposeImpl()
        {
            if (this.pathTracker.Read() != null) { this.pathTracker.Read().Dispose(); this.pathTracker.Write(null); }
            if (this.entityActuator != null) { this.entityActuator.Dispose(); this.entityActuator = null; }
        }

        #endregion Internal members

        #region Heaped members

        /// <summary>
        /// The position of this entity.
        /// </summary>
        private readonly HeapedValue<RCNumVector> position;

        /// <summary>
        /// The velocity of this entity.
        /// </summary>
        private readonly HeapedValue<RCNumVector> velocity;

        /// <summary>
        /// The ID of this entity.
        /// </summary>
        private readonly HeapedValue<int> id;

        /// <summary>
        /// The ID of the element type of this entity.
        /// </summary>
        private readonly HeapedValue<int> typeID;

        /// <summary>
        /// Reference to the path-tracker of this entity.
        /// </summary>
        private readonly HeapedValue<PathTrackerBase> pathTracker;

        /// <summary>
        /// Reference to the player who owns this entity or null if this entity is neutral or is a start location.
        /// </summary>
        private readonly HeapedValue<Player> owner;

        /// <summary>
        /// Reference to the scenario that this entity belongs to or null if this entity doesn't belong to any scenario.
        /// to a scenario.
        /// </summary>
        private readonly HeapedValue<Scenario> scenario;

        #endregion Heaped members

        /// <summary>
        /// The player of the currently active animations of this entity.
        /// </summary>
        private List<AnimationPlayer> currentAnimations;

        /// <summary>
        /// Reference to the element type of this entity.
        /// </summary>
        private IScenarioElementType elementType;

        /// <summary>
        /// Reference to the actuator of this entity.
        /// </summary>
        private EntityActuatorBase entityActuator;

        /// <summary>
        /// Reference to the RC.Engine.Simulator.PathFinder component.
        /// </summary>
        private IPathFinder pathFinder;
    }
}
