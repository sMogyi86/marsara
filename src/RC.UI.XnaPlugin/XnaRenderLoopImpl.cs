﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RC.Common;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Input;
using RC.Common.Diagnostics;

namespace RC.UI.XnaPlugin
{
    /// <summary>
    /// This class contains the implementation of the XNA render loop.
    /// </summary>
    class XnaRenderLoopImpl : Game
    {
        /// <summary>
        /// Method declaration for initialization after graphics device has been created.
        /// </summary>
        public delegate void InitializeDlgt();

        /// <summary>
        /// Method declaration for update.
        /// </summary>
        public delegate void UpdateDlgt();

        /// <summary>
        /// Method declaration for render.
        /// </summary>
        public delegate void RenderDlgt();

        /// <summary>
        /// Constructs an XnaRenderLoopImpl object.
        /// </summary>
        public XnaRenderLoopImpl(List<UpdateDlgt> updateFunctions,
                                 List<RenderDlgt> renderFunctions,
                                 List<InitializeDlgt> initFunctions)
        {
            if (updateFunctions == null) { throw new ArgumentNullException("updateFunctions"); }
            if (renderFunctions == null) { throw new ArgumentNullException("renderFunctions"); }
            if (initFunctions == null) { throw new ArgumentNullException("initFunctions"); }

            /// Store references to the Init/Update/Render functions.
            this.initFunctions = initFunctions;
            this.updateFunctions = updateFunctions;
            this.renderFunctions = renderFunctions;
            this.graphicsMgr = new GraphicsDeviceManager(this);

            /// Create a System.Windows.Forms reference to the main form of the application.
            this.mainForm = (Form)Form.FromHandle(this.Window.Handle);
            this.mainForm.FormBorderStyle = FormBorderStyle.None;
        }

        /// <summary>
        /// Gets the sprite batch.
        /// </summary>
        public SpriteBatch SpriteBatch { get { return this.spriteBatch; } }

        /// <summary>
        /// Gets or sets the size of the render screen.
        /// </summary>
        public RCIntVector ScreenSize
        {
            get { return this.screenSize; }
            set { this.screenSize = value; }
        }

        /// <summary>
        /// Gets the window of the application.
        /// </summary>
        public Form MainForm
        {
            get { return this.mainForm; }
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Screen[] screens = Screen.AllScreens;
            int screenIndexToUse = Math.Max(0, Math.Min(UIRoot.Instance.ScreenIndex, screens.Length - 1));
            this.mainForm.Left = screens[screenIndexToUse].WorkingArea.Left + this.mainForm.Left;
            this.mainForm.Top = screens[screenIndexToUse].WorkingArea.Top + this.mainForm.Top;

            foreach (InitializeDlgt initFunc in this.initFunctions)
            {
                initFunc();
            }

            // TODO: Add your initialization logic here
            this.graphicsMgr.PreferredBackBufferWidth = this.screenSize.X;
            this.graphicsMgr.PreferredBackBufferHeight = this.screenSize.Y;
            this.graphicsMgr.ApplyChanges();

            /// TODO: just for debugging
            //this.realMousePos = new RCIntVector(this.screenSize.X / 2, this.screenSize.Y / 2);
            //this.mainForm.MouseMove += OnMouseMove;

            base.Initialize();
        }

        /// <summary>
        /// TODO: just for debugging
        /// </summary>
        //private void OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    RCIntVector delta = new RCIntVector(e.X, e.Y) - this.realMousePos;
        //    if (delta.X != 0 || delta.Y != 0)
        //    {
        //        UIMouseManager.Instance.Pointer.Position += delta;
        //        TraceManager.WriteAllTrace(UIMouseManager.Instance.Pointer.Position, XnaTraceFilters.DETAILS);
        //        Mouse.SetPosition(this.realMousePos.X, this.realMousePos.Y);
        //    }
        //}

        /// <summary>
        /// TODO: just for debugging
        /// </summary>
        //private RCIntVector realMousePos;

        /// <summary>
        /// LoadContent will be called once per game and is the place to load all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            this.spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            this.spriteBatch.Dispose();
            this.spriteBatch = null;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            foreach (UpdateDlgt updateFunc in this.updateFunctions)
            {
                updateFunc();                
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            this.spriteBatch.Begin();
            foreach (RenderDlgt renderFunc in this.renderFunctions)
            {
                renderFunc();
            }
            this.spriteBatch.End();
            base.Draw(gameTime);
        }

        /// <summary>
        /// List of the initialization functions.
        /// </summary>
        private List<InitializeDlgt> initFunctions;

        /// <summary>
        /// List of the update functions.
        /// </summary>
        private List<UpdateDlgt> updateFunctions;

        /// <summary>
        /// List of the render functions.
        /// </summary>
        private List<RenderDlgt> renderFunctions;

        /// <summary>
        /// Reference to the graphics device manager.
        /// </summary>
        private GraphicsDeviceManager graphicsMgr;

        /// <summary>
        /// Reference to the sprite batch object that is used for rendering 2D frames.
        /// </summary>
        private SpriteBatch spriteBatch;

        /// <summary>
        /// The size of the render screen.
        /// </summary>
        private RCIntVector screenSize;

        /// <summary>
        /// Reference to the underlying System.Windows.Forms.Form.
        /// </summary>
        private Form mainForm;
    }
}
