﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using ScrollingShooterWindowsLibrary;

namespace ScrollingShooter
{
    /// <summary>
    /// A class for managing the loading, updating, and rendering of
    /// levels.
    /// </summary>
    public class LevelManager
    {
        public enum Level
        {
            Airbase,
            moon,
            AlienBaseSafe,
            crystalland,
            lavaLevel2,
        }

        Game game;
        SpriteBatch spriteBatch;
        BasicEffect basicEffect;

        public float scrollDistance;
        Rectangle scrollBounds;

        public bool Loading = true;
        public bool Paused = false;
        public bool Scrolling = true;
        public bool Ending = false;
        public bool LevelDone = false;
        public bool ResetLevel = false;

        public Tilemap CurrentMap;
        public Song CurrentSong;

        private Level level;


        /// <summary>
        /// Creates a new LevelManager
        /// </summary>
        /// <param name="game"></param>
        public LevelManager(Game game)
        {
            this.game = game;
            Ending = false;
            LevelDone = false;
        }


        /// <summary>
        /// Loads the LevelManager's content and initializes any
        /// functionality that needs a created graphics device
        /// </summary>
        public void LoadContent()
        {
            // Create our scroll bounds (the part of the world that is visible)
            // Note 
            scrollBounds = new Rectangle(0, 0, 384, 360);

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(game.GraphicsDevice);

            // Create a basic effect, used with the spritebatch 
            basicEffect = new BasicEffect(game.GraphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
            };
        }

        public void UnloadLevel()
        {
            CurrentMap = null;
            CurrentSong = null;
            scrollDistance = 0;
        }


        /// <summary>
        /// Loads a new level asynchronously
        /// </summary>
        /// <param name="level">The name of the level to load</param>
        public void LoadLevel(string level)
        {
            Loading = true;
            Ending = false;
            LevelDone = false;

            ThreadStart threadStarter = delegate
            {
                CurrentMap = game.Content.Load<Tilemap>("Tilemaps/" + level);
                CurrentMap.LoadContent(game.Content);
                
                // Set the "current" scroll offset for all levels
                for (int i = 0; i < CurrentMap.Layers.Count(); i++)
                {
                    CurrentMap.Layers[i].CurrentScrollOffset = CurrentMap.Layers[i].ScrollOffset;
                }
                 
                // Load the background music
                if (CurrentMap.MusicTitle != null && CurrentMap.MusicTitle != "")
                {
                    CurrentSong = game.Content.Load<Song>("Music/" + CurrentMap.MusicTitle);
                }
                else
                {
                    CurrentSong = null;
                }

                // Set the *default* starting scroll position to 16 tiles
                // above the bottom of the map
                scrollDistance = (-2 * CurrentMap.Height * CurrentMap.TileHeight) + (2 * 16 * CurrentMap.TileHeight);

                // Load the game objects
                for (int i = 0; i < CurrentMap.GameObjectGroupCount; i++)
                {
                    for (int j = 0; j < CurrentMap.GameObjectGroups[i].GameObjectData.Count(); j++)
                    {
                        GameObjectData goData = CurrentMap.GameObjectGroups[i].GameObjectData[j];
                        Vector2 position = new Vector2(goData.Position.Center.X, goData.Position.Center.Y);
                        GameObject go;

                        switch (goData.Category)
                        {
                            case "PlayerStart":
                                ScrollingShooterGame.Game.Player.Position = position;
                                ScrollingShooterGame.Game.Player.LayerDepth = CurrentMap.GameObjectGroups[i].LayerDepth;
                                 scrollDistance = -2 * position.Y + 300;
                                break;

                            case "LevelEnd":
                                break;

                            case "Powerup":
                                go = ScrollingShooterGame.GameObjectManager.CreatePowerup((PowerupType)Enum.Parse(typeof(PowerupType), goData.Type), position);
                                CurrentMap.GameObjectGroups[i].GameObjectData[j].ID = go.ID;
                                go.LayerDepth = CurrentMap.GameObjectGroups[i].LayerDepth;
                                go.ScrollingSpeed = CurrentMap.GameObjectGroups[i].ScrollingSpeed;
                                break;

                            case "Enemy":
                                go = ScrollingShooterGame.GameObjectManager.CreateEnemy((EnemyType)Enum.Parse(typeof(EnemyType), goData.Type), position);
                                CurrentMap.GameObjectGroups[i].GameObjectData[j].ID = go.ID;
                                go.LayerDepth = CurrentMap.GameObjectGroups[i].LayerDepth;
                                go.ScrollingSpeed = CurrentMap.GameObjectGroups[i].ScrollingSpeed;
                                break;
                            case "Boss":
                                go = ScrollingShooterGame.GameObjectManager.CreateBoss((BossType)Enum.Parse(typeof(BossType), goData.Type), position);
                                CurrentMap.GameObjectGroups[i].GameObjectData[j].ID = go.ID;
                                go.LayerDepth = CurrentMap.GameObjectGroups[i].LayerDepth;
                                go.ScrollingSpeed = CurrentMap.GameObjectGroups[i].ScrollingSpeed;
                                break;
                        }
                    }
                }

                //Start level off to scroll
                Scrolling = true;

                // Mark level as loaded
                Loading = false;
            };

            Thread loadingThread = new Thread(threadStarter);
            loadingThread.Start();
        }

        /// <summary>
        /// Updates the Level
        /// </summary>
        /// <param name="elapsedTime">the time elapsed between this and the previous frame</param>
        public void Update(float elapsedTime)
        {
            if (Paused)
            {
                // Unpase on space press
                if (Keyboard.GetState().IsKeyDown(Keys.Space)) Paused = false;
            }
            else if (Ending)
            {
                ScrollingShooterGame.Game.Player.EndLevel(elapsedTime);
            }
            else
            {
                // Update the scrolling distance - the distance
                // the screen has scrolled past the Player
                if (Scrolling)
                {

                    float scrollDelta = elapsedTime * (CurrentMap.Layers[CurrentMap.PlayerLayer].ScrollingSpeed);
                    scrollDistance += scrollDelta;
                    ScrollingShooterGame.Game.Player.Position -= new Vector2(0, scrollDelta / 2);

                    // Scroll all the tile layers
                    for (int i = 0; i < CurrentMap.LayerCount; i++)
                    {
                        CurrentMap.Layers[i].CurrentScrollOffset += elapsedTime * CurrentMap.Layers[i].ScrollingSpeed;
                    }
                    // Scrolls objects with the map
                    foreach (uint goID in ScrollingShooterGame.GameObjectManager.scrollingObjects)
                    {
                        GameObject go = ScrollingShooterGame.GameObjectManager.GetObject(goID);
                        go.ScrollWithMap(elapsedTime);
                        ScrollingShooterGame.GameObjectManager.UpdateGameObject(goID);
                    }

                }
                // Update only the game objects that appear near our scrolling region
                Rectangle bounds = new Rectangle(0,
                    (int)(-scrollDistance / 2) - 100,
                    CurrentMap.Width * CurrentMap.TileWidth,
                    16 * CurrentMap.TileHeight + 100);
                foreach (uint goID in ScrollingShooterGame.GameObjectManager.QueryRegion(bounds))
                {
                    GameObject go = ScrollingShooterGame.GameObjectManager.GetObject(goID);
                    go.Update(elapsedTime);
                    ScrollingShooterGame.GameObjectManager.UpdateGameObject(goID);
                }
                // Remove objects that we have passed
                Rectangle deleteBounds = new Rectangle(0,
                    (int)(-scrollDistance / 2) + (16 * CurrentMap.TileHeight + 50),
                    CurrentMap.Width * CurrentMap.TileWidth,
                    4480 - ((int)(-scrollDistance / 2) + (16 * CurrentMap.TileHeight + 50)));
                foreach (uint goID in ScrollingShooterGame.GameObjectManager.QueryRegion(deleteBounds))
                {
                    ScrollingShooterGame.GameObjectManager.DestroyObject(goID);
                }

                // Remove projectiles that are out of our update bounds
                foreach (uint goID in ScrollingShooterGame.GameObjectManager.projectileObjects)
                {
                    GameObject go = ScrollingShooterGame.GameObjectManager.GetObject(goID);
                    if (!bounds.Contains(go.Bounds))
                        ScrollingShooterGame.GameObjectManager.DestroyObject(goID);
                }
            }
        }


        /// <summary>
        /// Draw the level
        /// </summary>
        /// <param name="elapsedTime">The time between this and the last frame</param>
        public void Draw(float elapsedTime)
        {
            if (Paused)   
            {
                // TODO: Draw "Paused" Overlay
            }
            
            // Draw level
            Viewport viewport = game.GraphicsDevice.Viewport;
            basicEffect.World = Matrix.CreateScale(2, 2, 1);
            basicEffect.View = Matrix.CreateTranslation(new Vector3(0, scrollDistance, 0));
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, -1);

            spriteBatch.Begin(0, null, SamplerState.LinearClamp, null, null, basicEffect);

            for (int i = 0; i < CurrentMap.LayerCount; i++)
            {
                // To minimize drawn tiles, we limit ourselves to those onscreen
                int miny = (int)((-scrollDistance - 2 * CurrentMap.Layers[i].CurrentScrollOffset) /
                    (CurrentMap.TileHeight * 2));
                int maxy = miny + 15;

                // And those that exist
                if (miny < 0) miny = 0;
                if (maxy > CurrentMap.Height) maxy = CurrentMap.Height;

                for (int y = miny; y < maxy; y++)
                {
                    // Since our maps are only as wide as our rendering area, 
                    // no need for optimizaiton here
                    for (int x = 0; x < CurrentMap.Width; x++)
                    {
                        int index = x + y * CurrentMap.Width;
                        TileData tileData = CurrentMap.Layers[i].TileData[index];
                        if (tileData.TileID != 0)
                        {
                            Tile tile = CurrentMap.Tiles[tileData.TileID - 1];
                            Rectangle onScreen = new Rectangle(
                                x * CurrentMap.TileWidth,
                                (int)(y * CurrentMap.TileHeight + CurrentMap.Layers[i].CurrentScrollOffset),
                                CurrentMap.TileWidth,
                                CurrentMap.TileHeight);
                            spriteBatch.Draw(CurrentMap.Textures[tile.TextureID], onScreen, tile.Source, Color.White, 0f, new Vector2(0, CurrentMap.TileHeight / 2), tileData.SpriteEffects, CurrentMap.Layers[i].LayerDepth);
                        }
                    }
                }
            }

            // Draw only the game objects that appear within our scrolling region
            Rectangle bounds = new Rectangle(0,
                (int)(-scrollDistance / 2 - 100),
                CurrentMap.Width * CurrentMap.TileWidth,
                16 * CurrentMap.TileHeight + 100);

            foreach (uint goID in ScrollingShooterGame.GameObjectManager.QueryRegion(bounds))
            {
                GameObject go = ScrollingShooterGame.GameObjectManager.GetObject(goID);
                go.Draw(elapsedTime, spriteBatch);
            }

            spriteBatch.End();
            
        }

        public void NextLevel()
        {
            switch (level)
            {
                case Level.Airbase:
                    level = Level.lavaLevel2;
                    break;
                case Level.lavaLevel2:
                    level = Level.moon;
                    break;
                case Level.moon:
                    level = Level.crystalland;
                    break;
                case Level.crystalland:
                    level = Level.AlienBaseSafe;
                    break;
                case Level.AlienBaseSafe:
                    level = Level.Airbase;
                    break;
            }
            LoadLevel(level.ToString());
        }
    }
}
