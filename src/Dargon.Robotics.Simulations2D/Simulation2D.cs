using Dargon.Robotics.Simulations2D.Utilities;
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Dargon.Commons.Collections;
using Dargon.Robotics.Debugging;

namespace Dargon.Robotics.Simulations2D {
   public class Simulation2D : Game, IRenderer {
      private const float kTicksPerMillisecond = 10.0f;
      private const float kTickIntervalSeconds = 1.0f / (1000.0f * kTicksPerMillisecond);
      private readonly DateTime startTime = DateTime.Now;
      private readonly GraphicsDeviceManager graphicsDeviceManager;
      private readonly World world;
      private readonly ConcurrentSet<ISimulationEntity> entities;
      private readonly IDebugRenderContext debugRenderContext;
      private int ticksExecuted = 0;
      private SpriteBatch spriteBatch;
      private Texture2D whiteRectangle;
      private RenderTarget2D invertedRenderTarget;

      public Simulation2D(ConcurrentSet<ISimulationEntity> entities, IDebugRenderContext debugRenderContext) {
         this.entities = entities;
         this.debugRenderContext = debugRenderContext;

         Content.RootDirectory = "Assets";
         ConvertUnits.SetDisplayUnitToSimUnitRatio(50f);

         graphicsDeviceManager = new GraphicsDeviceManager(this) {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
         };

         var gravity = Vector2.Zero;
         world = new World(gravity);
         foreach (var entity in entities) {
            entity.Initialize(world);
         }
      }

      protected override void LoadContent() {
         base.LoadContent();
         spriteBatch = new SpriteBatch(GraphicsDevice);
         SpriteBatchEx.GraphicsDevice = GraphicsDevice;
         whiteRectangle = CreateSolidBitmap(Color.White);
         invertedRenderTarget = new RenderTarget2D(GraphicsDevice, 1280, 720);

         Window.Position = Point.Zero;
      }

      protected override void UnloadContent() {
         base.UnloadContent();
         whiteRectangle.Dispose();
         spriteBatch.Dispose();
         graphicsDeviceManager.Dispose();
      }

      protected override void Draw(GameTime gameTime) {
         base.Draw(gameTime);

         GraphicsDevice.SetRenderTarget(invertedRenderTarget);
         GraphicsDevice.RasterizerState = RasterizerState.CullNone;
         GraphicsDevice.Clear(Color.Black);
         spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(0, 0, 0));
         foreach(var entity in entities) {
            entity.Render(this);
         }
         for (var i = -15; i < 15; i++) {
            DrawLineSegmentWorld(new Vector2(i, 0), new Vector2(i, 40), Color.Gray);
            DrawLineSegmentWorld(new Vector2(0, i), new Vector2(40, i), Color.Gray);
         }
         var debugScene = debugRenderContext.CurrentScene;
         foreach (var quad in debugScene.Quads) {
            DrawCenteredRectangleWorld(
               new Vector2((float)quad.Position.X, (float)quad.Position.Y),
               new Vector2((float)quad.Extents.X, (float)quad.Extents.Y),
               quad.Rotation,
               new Color(quad.Color.R, quad.Color.G, quad.Color.B, quad.Color.A));
         }
         spriteBatch.End();
         GraphicsDevice.SetRenderTarget(null);
         GraphicsDevice.RasterizerState = RasterizerState.CullNone;
         GraphicsDevice.Textures[0] = invertedRenderTarget;
         GraphicsDevice.DrawUserPrimitives(
            PrimitiveType.TriangleStrip,
            new [] {
               new VertexPositionColorTexture(Vector3.Zero, Color.White, Vector2.UnitY),
               new VertexPositionColorTexture(Vector3.UnitX * 1280, Color.White, Vector2.One),
               new VertexPositionColorTexture(Vector3.UnitY * 720, Color.White, Vector2.Zero),
               new VertexPositionColorTexture(new Vector3(1280, 720, 0), Color.White, Vector2.UnitX)
            }, 0, 2, VertexPositionColorTexture.VertexDeclaration);
      }

      protected override void Update(GameTime gameTime) {
         base.Update(gameTime);
         var millisecondsElapsed = (DateTime.Now - startTime).TotalMilliseconds;
         var desiredTicksExecuted = millisecondsElapsed * kTicksPerMillisecond;
         while (ticksExecuted < desiredTicksExecuted) {
            ticksExecuted++;
            world.Step(kTickIntervalSeconds);
            foreach (var entity in entities) {
               entity.Tick(kTickIntervalSeconds);
            }
         }
      }

      private Texture2D CreateSolidBitmap(Color color) {
         var texture = new Texture2D(GraphicsDevice, 1, 1);
         texture.SetData(new[] { color });
         return texture;
      }

      public void DrawCenteredRectangleWorld(Vector2 center, Vector2 extents, float rotation, Color color) {
         var centerScreen = ConvertUnits.ToDisplayUnits(center);
         var extentsScreen = ConvertUnits.ToDisplayUnits(extents);

         spriteBatch.Draw(
            whiteRectangle,
            centerScreen,
            null,
            color,
            rotation,
            Vector2.One / 2,
            extentsScreen,
            SpriteEffects.None,
            0);
      }

      public void DrawLineSegmentWorld(Vector2 a, Vector2 b, Color color) {
         var aScreen = ConvertUnits.ToDisplayUnits(a);
         var bScreen = ConvertUnits.ToDisplayUnits(b);
         spriteBatch.DrawLine(aScreen, bScreen, color);
      }
      public void DrawForceVectorWorld(Vector2 origin, Vector2 vector, Color color) {
         const float kScaling = 0.1f;
         var aScreen = ConvertUnits.ToDisplayUnits(origin);
         var bScreen = ConvertUnits.ToDisplayUnits(origin + vector * kScaling);
         spriteBatch.DrawLine(aScreen, bScreen, color);
      }
   }
}