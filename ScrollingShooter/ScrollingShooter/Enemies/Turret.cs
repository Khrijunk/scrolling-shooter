﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace ScrollingShooter
{
    /// <summary>
    /// An enemy turret that turns to the player and fires at them
    /// </summary>
    public class Turret : Enemy
    {   
        // Turret Variables
        /// <summary>
        /// spritesheet with the turret texture
        /// </summary>
        Texture2D spritesheet;

        /// <summary>
        /// Position of the turret
        /// </summary>
        Vector2 position;

        /// <summary>
        /// Bounds of the turret on the spritesheet
        /// </summary>
        Rectangle spriteBounds = new Rectangle();

        /// <summary>
        /// Offset from the center to the tip of the left barrel and subtract half the width of the bullet (2.5f)
        /// </summary>
        Vector2 offsetLeft;

        /// <summary>
        /// Offset from the center to the tip of the right barrel and subtract half the width of the bullet (2.5f)
        /// </summary>
        Vector2 offsetRight;

        /// <summary>
        /// Int to determine which cannon we are firing from: 0 = right barrel, 1 = left barrel
        /// </summary>
        int barrel = 0; 

        /// <summary>
        /// Rotation of the turret
        /// </summary>
        float alpha;

        /// <summary>
        /// Shot delay of the turret
        /// </summary>
        float shotDelay;

        /// <summary>
        /// The bounding rectangle of the Dart
        /// </summary>
        public override Rectangle Bounds
        {
            get { return new Rectangle((int)position.X, (int)position.Y, spriteBounds.Width, spriteBounds.Height); }
        }

        /// <summary>
        /// Creates a new instance of an enemy turret
        /// </summary>
        /// <param name="content">A ContentManager to load resources with</param>
        /// <param name="position">The position of the turret in the game world</param>
        public Turret(ContentManager content, Vector2 position)
        {
            this.position = position;

            spritesheet = content.Load<Texture2D>("Spritesheets/newsh3.shp.000000");

            spriteBounds.X = 161;
            spriteBounds.Y = 62;
            spriteBounds.Width = 14;
            spriteBounds.Height = 19;

            alpha = 0;

            shotDelay = 0;

            // Offset from the center to the tip of the left barrel and subtract half the width of the bullet (2.5f) and half the width of the turret
            offsetLeft = new Vector2(-4 - 2f, (float)this.Bounds.Height / 2);

            // Offset from the center to the tip of the right barrel and subtract half the width of the bullet (2.5f) and half the width of the turret
            offsetRight = new Vector2(4 - 2f, (float)this.Bounds.Height / 2);
        }

        /// <summary>
        /// Updates the Turret
        /// </summary>
        /// <param name="elapsedTime">The in-game time between the previous and current frame</param>
        public override void  Update(float elapsedTime)
        {
            // Update the shot timer
            shotDelay += elapsedTime;

            // Sense the player's position
            PlayerShip player = ScrollingShooterGame.Game.player;
            Vector2 playerPosition = new Vector2(player.Bounds.Center.X, player.Bounds.Center.Y);

            // Get a vector from our position to the player's position
            Vector2 toPlayer = playerPosition - this.position;

            if(toPlayer.LengthSquared() < 150000)
            {
                // TODO: Figure out why the bullet spawn doesn't always match up perfectly with the cannon barrel

                // We sense the player's ship!                  
                // Get a normalized turning vector
                toPlayer.Normalize();

                // Rotate towards them!
                this.alpha = (float)Math.Atan2(toPlayer.Y, toPlayer.X) - MathHelper.PiOver2;

                // If it is time to shoot, fire a bullet towards the player
                if (shotDelay > 1f)
                {
                    // Rotation Matrix to get the rotated offset vectors
                    Matrix rotMatrix = Matrix.CreateRotationZ(alpha);

                    // Offset vector that adjusts according to which barrel we are using
                    Vector2 offset;

                    // Bullet velocity
                    float bulletVel = 200f;

                    // Figure out which barrel we are using and offset it with the toPlayer direction vector
                    if (barrel == 0)
                    {
                        offset = Vector2.Transform(offsetRight, rotMatrix);
                        barrel = 1;
                    }
                    else
                    {
                        offset = Vector2.Transform(offsetLeft, rotMatrix);
                        barrel = 0;
                    }

                    // Spawn the bullet
                    // TODO: Add this to the enemy projectiles list once it's created
                    ScrollingShooterGame.Game.projectiles.Add(new EnemyBullet(ScrollingShooterGame.Game.Content, this.position + offset, bulletVel * toPlayer));

                    // Reset the shot delay
                    shotDelay = 0;
                }

            }                        
        }

        /// <summary>
        /// Draw the turret on-screen
        /// </summary>
        /// <param name="elapsedTime">The in-game time between the previous and current frame</param>
        /// <param name="spriteBatch">An already initialized SpriteBatch, ready for Draw() commands</param>
        public override void Draw(float elapsedTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(spritesheet, Bounds, spriteBounds, Color.White, alpha, new Vector2(spriteBounds.Width / 2, spriteBounds.Height / 2), SpriteEffects.None, 0f);
        }

    }
}
