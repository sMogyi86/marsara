﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.App.PresLogic.Controls;
using RC.Common.ComponentModel;
using RC.App.PresLogic.Panels;
using RC.Common;
using RC.UI;
using System.Threading;
using RC.Common.Diagnostics;
using System.Diagnostics;
using RC.App.BizLogic.Services;

namespace RC.App.PresLogic.Pages
{
    /// <summary>
    /// The Gameplay page of the RC application.
    /// </summary>
    public class RCGameplayPage : RCAppPage, IGameConnector
    {
        /// <summary>
        /// Constructs a gameplay page.
        /// </summary>
        public RCGameplayPage() : base()
        {
            this.mapDisplayBasic = new RCMapDisplayBasic(new RCIntVector(0, 13), new RCIntVector(320, 135));
            //this.mapWalkabilityDisplay = new RCMapWalkabilityDisplay(this.mapDisplayBasic);
            this.mapObjectDisplayEx = new RCMapObjectDisplay(this.mapDisplayBasic);
            this.selectionDisplayEx = new RCSelectionDisplay(this.mapObjectDisplayEx);
            this.objectPlacementDisplayEx = new RCObjectPlacementDisplay(this.selectionDisplayEx);
            this.mapDisplay = this.objectPlacementDisplayEx;

            this.inputManager = new GameplayInputManager();

            this.minimapPanel = new RCMinimapPanel(new RCIntRectangle(0, 120, 80, 80),
                                                   new RCIntRectangle(1, 1, 78, 78),
                                                   "RC.App.Sprites.MinimapPanel");
            this.detailsPanel = new RCDetailsPanel(new RCIntRectangle(80, 148, 170, 52),
                                                   new RCIntRectangle(0, 1, 170, 50),
                                                   "RC.App.Sprites.DetailsPanel");
            this.commandPanel = new RCCommandPanel(new RCIntRectangle(250, 130, 70, 70),
                                                   new RCIntRectangle(3, 3, 64, 64),
                                                   "RC.App.Sprites.CommandPanel");
            this.tooltipBar = new RCTooltipBar(new RCIntRectangle(0, 0, 209, 13),
                                               new RCIntRectangle(1, 1, 207, 11),
                                               "RC.App.Sprites.TooltipBar");
            this.resourceBar = new RCResourceBar(new RCIntRectangle(209, 0, 111, 13),
                                                 new RCIntRectangle(0, 1, 110, 11),
                                                 "RC.App.Sprites.ResourceBar");
            this.menuButtonPanel = new RCMenuButtonPanel(new RCIntRectangle(226, 140, 24, 8),
                                                         new RCIntRectangle(0, 0, 24, 8),
                                                         "RC.App.Sprites.MenuButton");            
            this.RegisterPanel(this.minimapPanel);
            this.RegisterPanel(this.detailsPanel);
            this.RegisterPanel(this.commandPanel);
            this.RegisterPanel(this.tooltipBar);
            this.RegisterPanel(this.resourceBar);
            this.RegisterPanel(this.menuButtonPanel);

            this.gameConnection = new AggregateGameConnector(new HashSet<IGameConnector> { this.mapDisplay, this.commandPanel });
        }

        #region IGameConnector methods

        /// <see cref="IGameConnector.Connect"/>
        public void Connect()
        {
            if (this.gameConnection.CurrentStatus != ConnectionStatusEnum.Offline) { throw new InvalidOperationException("The gameplay page is not offline!"); }
            
            /// TODO: A scenario shall be running at this point!
            ComponentManager.GetInterface<IMultiplayerService>().CreateNewGame(".\\maps\\testmap4.rcm", GameTypeEnum.Melee, GameSpeedEnum.Fastest);

            /// Create and start the map display control.
            this.gameConnection.ConnectorOperationFinished += this.OnConnected;
            this.gameConnection.Connect();
        }

        /// <see cref="IGameConnector.Disconnect"/>
        public void Disconnect()
        {
            if (this.gameConnection.CurrentStatus != ConnectionStatusEnum.Online) { throw new InvalidOperationException("The gameplay page is not online!"); }

            ComponentManager.GetInterface<IMultiplayerService>().LeaveCurrentGame();

            /// Deactivate mouse handling.
            this.menuButtonPanel.MouseSensor.ButtonDown -= this.OnMenuButtonPressed;
            this.inputManager.StopAndRemoveInputHandler("ScrollHandler");
            this.inputManager.StopAndRemoveInputHandler("NormalInputModeHandler");

            /// Detach the map display control from this page.
            this.DetachSensitive(this.mapDisplay);
            this.Detach(this.mapDisplay);

            /// Stop the map display control.
            this.gameConnection.ConnectorOperationFinished += this.OnDisconnected;
            this.gameConnection.Disconnect();
        }

        /// <see cref="IGameConnector.CurrentStatus"/>
        public ConnectionStatusEnum CurrentStatus
        {
            get { return this.gameConnection.CurrentStatus; }
        }

        /// <see cref="IGameConnector.ConnectorOperationFinished"/>
        public event Action<IGameConnector> ConnectorOperationFinished;

        #endregion IGameConnector methods

        /// <see cref="RCAppPage.OnActivated"/>
        protected override void OnActivated()
        {
            this.minimapPanel.Show();
            this.detailsPanel.Show();
            this.commandPanel.Show();
            this.tooltipBar.Show();
            this.resourceBar.Show();
            this.menuButtonPanel.Show();

            /// TODO: connect shall be performed before activation
            this.Connect();
        }

        /// <see cref="RCAppPage.OnInactivating"/>
        protected override void OnInactivating()
        {
            this.Disconnect();
        }

        /// <summary>
        /// This method is called when the map display started successfully.
        /// </summary>
        private void OnConnected(IGameConnector sender)
        {
            this.gameConnection.ConnectorOperationFinished -= this.OnConnected;

            /// Attach the map display control to this page.
            this.Attach(this.mapDisplay);
            this.AttachSensitive(this.mapDisplay);
            this.mapDisplay.SendToBottom();

            /// Create the mouse handlers for the map display.
            this.inputManager.StartAndAddInputHandler("ScrollHandler", new ScrollHandler(this, this.mapDisplay));
            this.inputManager.StartAndAddInputHandler("NormalInputModeHandler", new NormalInputModeHandler(this.selectionDisplayEx, this.selectionDisplayEx));

            this.menuButtonPanel.MouseSensor.ButtonDown += this.OnMenuButtonPressed;

            if (this.ConnectorOperationFinished != null) { this.ConnectorOperationFinished(this); }
        }
        
        /// <summary>
        /// This method is called when the map display started successfully.
        /// </summary>
        private void OnDisconnected(IGameConnector sender)
        {
            this.gameConnection.ConnectorOperationFinished -= this.OnDisconnected;

            /// Remove the map display control.
            this.mapDisplayBasic = null;
            this.mapWalkabilityDisplay = null;
            this.mapObjectDisplayEx = null;
            this.selectionDisplayEx = null;
            this.objectPlacementDisplayEx = null;
            this.mapDisplay = null;
            
            if (this.ConnectorOperationFinished != null) { this.ConnectorOperationFinished(this); }

            /// TODO: later we don't need to stop the render loop here!
            UIRoot.Instance.GraphicsPlatform.RenderLoop.Stop();
        }

        /// <summary>
        /// This method is called when the "Menu" button has been pressed.
        /// </summary>
        /// <param name="sender">Reference to the button.</param>
        private void OnMenuButtonPressed(UISensitiveObject sender, UIMouseEventArgs evtArgs)
        {
            this.Deactivate();
        }

        /// <summary>
        /// Reference to the minimap panel.
        /// </summary>
        private RCMinimapPanel minimapPanel;

        /// <summary>
        /// Reference to the details panel.
        /// </summary>
        private RCDetailsPanel detailsPanel;

        /// <summary>
        /// Reference to the command panel.
        /// </summary>
        private RCCommandPanel commandPanel;

        /// <summary>
        /// Reference to the tooltip bar.
        /// </summary>
        private RCTooltipBar tooltipBar;

        /// <summary>
        /// Reference to the resource bar.
        /// </summary>
        private RCResourceBar resourceBar;

        /// <summary>
        /// Reference to the panel that contains the gameplay menu button.
        /// </summary>
        private RCMenuButtonPanel menuButtonPanel;

        /// <summary>
        /// Reference to the map display.
        /// </summary>
        private RCMapDisplay mapDisplay;

        /// <summary>
        /// The basic part of the map display.
        /// </summary>
        private RCMapDisplayBasic mapDisplayBasic;

        /// <summary>
        /// Extension of the map display that displays the walkability of the map cells.
        /// </summary>
        private RCMapWalkabilityDisplay mapWalkabilityDisplay;

        /// <summary>
        /// Extension of the map display that displays the map objects.
        /// </summary>
        private RCMapObjectDisplay mapObjectDisplayEx;

        /// <summary>
        /// Extension of the map display that displays the selection box and the selection indicators of the selected
        /// map objects.
        /// </summary>
        private RCSelectionDisplay selectionDisplayEx;

        /// <summary>
        /// Extension of the map display that displays the object placement boxes.
        /// </summary>
        private RCObjectPlacementDisplay objectPlacementDisplayEx;

        /// <summary>
        /// Reference to the game connector object.
        /// </summary>
        private IGameConnector gameConnection;

        /// <summary>
        /// Reference to the input manager.
        /// </summary>
        private GameplayInputManager inputManager;
    }
}
