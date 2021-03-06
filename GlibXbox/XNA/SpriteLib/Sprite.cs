﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.ComponentModel;
using System.Diagnostics;

namespace Glib.XNA.SpriteLib
{
    /// <summary>
    /// An enum indicating the origin type of a Sprite.
    /// </summary>
    public enum SpriteOriginType
    {
        /// <summary>
        /// The origin is located at 0,0 relative to the Sprite. This is the default.
        /// </summary>
        Zero,
        /// <summary>
        /// The origin is located at the center of the Sprite.
        /// </summary>
        Center,
        /// <summary>
        /// The origin is a user-specified value.
        /// </summary>
        Custom
    }

    /// <summary>
    /// An implementation of ISprite with many features, such as updated, drawn, and moved events, an easily accessible position, configurable position changes per update, center-point support, and scale support.
    /// </summary>
    [DebuggerDisplay("Position = {Position}")]
    public class Sprite : ISprite, ISpriteBatchManagerSprite, ITexturable, IPositionable, ISizedScreenObject, ISizable
    {
        #region Operators
        /// <summary>
        /// Convert the specified Sprite to a rectangle.
        /// </summary>
        /// <param name="spr">The Sprite to convert to a Rectangle.</param>
        /// <returns>The Rectangle representing the area of the Sprite.</returns>
        static public explicit operator Rectangle(Sprite spr)
        {
            return spr.Rectangle;
        }

        /// <summary>
        /// Convert the specified Sprite to a texture.
        /// </summary>
        /// <param name="spr">The Sprite to convert to a Texture2D.</param>
        /// <returns>The texture of the Sprite.</returns>
        static public explicit operator Texture2D(Sprite spr)
        {
            return spr.Texture;
        }
        #endregion

        private bool _visible = true;

        /// <summary>
        /// Gets or sets a boolean indicating whether the Sprite is visible.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; }
        }

        /// <summary>
        /// The speed of the sprite in X and Y.
        /// </summary>
        public Vector2 Speed = Vector2.Zero;

        private SpriteEffects _effect = SpriteEffects.None;

        /// <summary>
        /// The effect to apply to the Sprite when drawn.
        /// </summary>
        public virtual SpriteEffects Effect
        {
            get
            {
                return _effect;
            }
            set
            {
                _effect = value;
            }
        }

        /// <summary>
        /// The speed of the sprite along the X axis.
        /// </summary>
        public virtual float XSpeed
        {
            get
            {
                return Speed.X;
            }
            set
            {
                Speed.X = value;
                UpdateParams.UpdateX = true;
            }
        }

        /// <summary>
        /// A boolean representing whether or not to only draw a selected region of the sprite.
        /// </summary>
        public virtual bool OnlyDrawRegion
        {
            get
            {
                return DrawRegion != null;
            }
            set
            {
                if (!value)
                {
                    DrawRegion = null;
                }
            }
        }

        /// <summary>
        /// The selected region of the Sprite to draw. Set to null to draw the entire Sprite.
        /// </summary>
        public Rectangle? DrawRegion = null;

        /// <summary>
        /// An EventHandler called after the successful movement of this Sprite.
        /// </summary>
        public event EventHandler Moved = null;

        /// <summary>
        /// The speed of the sprite along the Y axis.
        /// </summary>
        public virtual float YSpeed
        {
            get
            {
                return Speed.Y;
            }
            set
            {
                Speed.Y = value;
                UpdateParams.UpdateY = true;
            }
        }

        /// <summary>
        /// The SpriteManager associated with this sprite, if any.
        /// </summary>
        public SpriteManager SpriteManager = null;

        /// <summary>
        /// An event called after every update of this sprite.
        /// </summary>
        public event EventHandler Updated = null;

        /// <summary>
        /// An event called after every draw of this sprite.
        /// </summary>
        public event EventHandler Drawn = null;

        /// <summary>
        /// A cancellable event called before every change of this sprite's position.
        /// </summary>
        public event SpriteMoveEventHandler Move = null;

        /// <summary>
        /// The SpriteBatch used for drawing the sprite.
        /// </summary>
        public SpriteBatch SpriteBatch;

        /// <summary>
        /// The scale at which to render the sprite.
        /// </summary>
        public Vector2 Scale = Vector2.One;

        /// <summary>
        /// Gets the scale-sensitive center of the sprite.
        /// Setting this property will break with OriginType as Center or Custom.
        /// </summary>
        public Vector2 Center
        {
            get
            {
                return new Vector2((X - (Origin.X * Scale.X)) + (Width / 2), (Y - (Origin.Y * Scale.Y)) + (Height / 2));
                //return new Vector2(Rectangle.Center.X, Rectangle.Center.Y);
            }
            set
            {
                Vector2 proposition = new Vector2(value.X - Width / 2, value.Y - Height / 2);
                if (!IsMoveEventCanceled(proposition))
                {
                    MoveSprite(proposition);
                }

            }
        }
        
        /// <summary>
        /// The current X coordinate of the sprite.
        /// </summary>
        public virtual float X
        {
            get
            {
                return Position.X;
            }
            set
            {
                if (!IsMoveEventCanceled(new Vector2(value, _pos.Y)))
                {
                    _pos.X = value;
                    if (Moved != null)
                    {
                        Moved(this, new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// The current Y coordinate of the sprite.
        /// </summary>
        public virtual float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                if (!IsMoveEventCanceled(new Vector2(_pos.X, value)))
                {
                    _pos.Y = value;
                    if (Moved != null)
                    {
                        Moved(this, new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// A scale-sensitive width. Use Texture.Width to not account for scale.
        /// </summary>
        public virtual float Width
        {
            get
            {
                return Texture.Width * Scale.X;
            }
            set
            {
                Scale.X = value / Texture.Width;
            }
        }

        /// <summary>
        /// A scale-sensitive height. Use Texture.Height to not account for scale.
        /// </summary>
        public virtual float Height
        {
            get
            {
                return Texture.Height * Scale.Y;
            }
            set
            {
                Scale.Y = value / Texture.Height;
            }
        }

        /// <summary>
        /// The current rotation of the sprite.
        /// </summary>
        public SpriteRotation Rotation = new SpriteRotation();

        private Texture2D _texture;

        /// <summary>
        /// The texture of the sprite.
        /// </summary>
        public virtual Texture2D Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }
        
        private Vector2 _pos;

        /// <summary>
        /// The current position of the sprite.
        /// </summary>
        public virtual Vector2 Position
        {
            get
            {
                return _pos;
            }
            set
            {
                if (!IsMoveEventCanceled(value))
                {
                    _pos = value;
                    if (Moved != null)
                    {
                        Moved(this, new EventArgs());
                    }
                }
            }
        }

        /// <summary>
        /// Move this Sprite to the specified position, calling the Moved event, but not Move.
        /// </summary>
        /// <param name="newPos">The new position of the Sprite.</param>
        protected void MoveSprite(Vector2 newPos)
        {
            _pos = newPos;
            if (Moved != null)
            {
                Moved(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Calls the Move event and returns whether or not it was canceled.
        /// </summary>
        /// <remarks>
        /// Calls an event; call only when neccesary.
        /// </remarks>
        /// <param name="newPos">The position to call the move event with.</param>
        /// <returns>Whether or not the called move event was cancelled.</returns>
        protected bool IsMoveEventCanceled(Vector2 newPos)
        {
            bool cancel = false;
            if (Move != null)
            {
                SpriteMoveEventArgs args = new SpriteMoveEventArgs(_pos, newPos);
                foreach (SpriteMoveEventHandler tmp in Move.GetInvocationList())
                {
                    tmp(this, args);
                    cancel = args.Cancel;
                }
            }
            return cancel;
        }

        /// <summary>
        /// The color of which to tint the sprite. Defaults to white.
        /// </summary>
        public Color Color = Color.White;

        /// <summary>
        /// The <see cref="UpdateParamaters">UpdateParamaters</see> used to update the sprite.
        /// </summary>
        public UpdateParamaters UpdateParams = new UpdateParamaters(true,true);

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        public Sprite(Texture2D texture, Vector2 pos, SpriteBatch sb)
        {
            _pos = pos;
            this.SpriteBatch = sb;
            this.Texture = texture;
        }

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        public Sprite(Texture2D texture, Vector2 pos, Color color, SpriteBatch sb) : this(texture, pos, sb)
        {
            this.Color = color;
        }

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        public Sprite(Texture2D texture, Vector2 pos, Color color, SpriteBatch sb, UpdateParamaters up) : this(texture, pos, color, sb)
        {
            this.UpdateParams = up;
        }

        /// <summary>
        /// Create a new Sprite.
        /// </summary>
        public Sprite(Texture2D texture, Vector2 pos, SpriteBatch sb, UpdateParamaters up) : this(texture, pos, sb)
        {
            this.UpdateParams = up;
        }

        /// <summary>
        /// Draws the sprite.
        /// Automatically begins the SpriteBatch before you draw the sprite and ends the SpriteBatch after you draw the sprite.
        /// </summary>
        public void Draw()
        {
            try
            {
                SpriteBatch.Begin();
            }
            catch { }
            DrawNonAuto();
            SpriteBatch.End();
        }

        /// <summary>
        /// Gets an approximate Rectangle representing the area covered by this Sprite.
        /// </summary>
        public Rectangle Rectangle
        {
            get
            {
                Vector2 usedPos = Position;
                usedPos -= Origin * Scale;
                return new Rectangle(Convert.ToInt32(usedPos.X), Convert.ToInt32(usedPos.Y), Convert.ToInt32(Width), Convert.ToInt32(Height));
            }
        }

        #region Origin settings
        private Vector2 _customOrigin = Vector2.Zero;

        private SpriteOriginType _originType = SpriteOriginType.Zero;

        /// <summary>
        /// Gets or sets an enum representing the type of the origin of this Sprite.
        /// </summary>
        public SpriteOriginType OriginType
        {
            get { return _originType; }
            set { _originType = value; }
        }


        /// <summary>
        /// Gets or sets the origin of the Sprite.
        /// </summary>
        public virtual Vector2 Origin
        {
            get
            {
                if (_originType == SpriteOriginType.Custom)
                {
                    return _customOrigin;
                }
                else
                {
                    return _originType == SpriteOriginType.Center ? new Vector2(Texture.Width / 2, Texture.Height / 2) : Vector2.Zero;
                }
            }
            set
            {
                if (value == Vector2.Zero)
                {
                    _originType = SpriteOriginType.Zero;
                }
                else if (value == new Vector2(Texture.Width / 2, Texture.Height / 2))
                {
                    _originType = SpriteOriginType.Center;
                }
                else
                {
                    _originType = SpriteOriginType.Custom;
                    _customOrigin = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether or not to use the center of the Sprite as the origin.
        /// </summary>
        /// <remarks>
        /// When Origin is set, it will only affect this value if set precisely to the center of the Sprite (no scale accounting).
        /// If this is set to false, the Origin will not be changed.
        /// </remarks>
        public bool UseCenterAsOrigin
        {
            get
            {
                return _originType == SpriteOriginType.Center;
            }
            set
            {
                _originType = value ? SpriteOriginType.Center : (_originType == SpriteOriginType.Center ? SpriteOriginType.Zero : _originType);
            }
        }
        #endregion

        /// <summary>
        /// Draws the sprite.
        /// Requires you to begin the SpriteBatch before you draw the sprite, and to end the SpriteBatch after you draw the sprite.
        /// </summary>
        public virtual void DrawNonAuto()
        {
            if (Visible)
            {
                SpriteBatch.Draw(this);
                CallDrawn();
            }
        }

        /// <summary>
        /// Call the drawn event after drawing of the Sprite.
        /// </summary>
        protected void CallDrawn()
        {
            if (Drawn != null)
            {
                Drawn(this, EventArgs.Empty);
            }
        }


        #region Intersection
        /// <summary>
        /// Checks whether the given point intersects with the sprite.
        /// </summary>
        /// <param name="pos">The position to check.</param>
        /// <returns>Whether or not the specified position intersects with this Sprite.</returns>
        public bool Intersects(Vector2 pos)
        {
            if (!Visible)
            {
                return false;
            }
            float realX = X;
            float realY = Y;
            realX -= Origin.X * Scale.X;
            realY -= Origin.Y * Scale.Y;

            return pos.X <= realX + Width && pos.X >= realX && pos.Y >= realY && pos.Y <= realY + Height;
        }

        /// <summary>
        /// Checks whether the given rectangle intersects with this sprite.
        /// </summary>
        /// <param name="r">The rectangle to check intersection against</param>
        /// <returns>Whether or not the specified rectangle intersects with this Sprite.</returns>
        public bool Intersects(Rectangle r)
        {
            return Visible && Rectangle.Intersects(r);
        }

        /// <summary>
        /// Checks whether the given sprite intersects with this sprite.
        /// </summary>
        /// <param name="s">The sprite to check intersection against.</param>
        /// <returns>Whether or not the rectangle of the specified Sprite intersects with this Sprite.</returns>
        public bool Intersects(Sprite s)
        {
            return Visible && s.Visible && Intersects(s.Rectangle);
        }
        #endregion

        /// <summary>
        /// Determines the edges which this sprite has points past, if any.
        /// </summary>
        /// <returns>An array of directions representing the edges this sprite is past. Empty if none.</returns>
        public Direction[] EdgesPast()
        {
            Viewport vp = SpriteBatch.GraphicsDevice.Viewport;
            if (UsedViewport.HasValue)
            {
                vp = UsedViewport.Value;
            }
            List<Direction> allEdges = new List<Direction>();
            float realX = X;
            float realY = Y;
            realX -= Origin.X * Scale.X;
            realY -= Origin.Y * Scale.Y;
            if (realX < 0)
            {
                allEdges.Add(Direction.Left);
            }
            if (realY < 0)
            {
                allEdges.Add(Direction.Top);
            }
            if (realX + Width > vp.Width)
            {
                allEdges.Add(Direction.Right);
            }
            if (realY + Height > vp.Height)
            {
                allEdges.Add(Direction.Bottom);
            }
            return allEdges.ToArray();
        }

        /// <summary>
        /// If not null, the viewport to use in viewport-requiring operation.
        /// </summary>
        public Viewport? UsedViewport = null;

        /// <summary>
        /// Remove this Sprite from it's associated SpriteManager.
        /// </summary>
        /// <exception cref="System.NullReferenceException">If there is no associated SpriteManager (the SpriteManager property is null).</exception>
        public void RemoveFromManager()
        {
            SpriteManager.RemoveSelf(this);
        }

        private Direction[] _pastDirections;

        /// <summary>
        /// Logically update this sprite. This can also be done in the Updated event.
        /// </summary>
        public virtual void Update()
        {
            if (UpdateParams.UpdateX)
            {
                X += XSpeed;
            }
            if (UpdateParams.UpdateY)
            {
                Y += YSpeed;
            }
            if (UpdateParams.FixEdgeOff)
            {
                _pastDirections = EdgesPast();
                if (_pastDirections.Contains(Direction.Left) || _pastDirections.Contains(Direction.Right))
                {
                        XSpeed *= -1;
                } if (_pastDirections.Contains(Direction.Top) || _pastDirections.Contains(Direction.Bottom))
                {
                        YSpeed *= -1;
                }
            }
            /*
            if (UpdateParams.FollowMouse)
            {
                MouseState mouse = Mouse.GetState();
                Vector2 target = new Vector2(mouse.X, mouse.Y);

                Vector2 direction = target - Position;
                float acceleration = direction.Length() / 10;

                if (direction.LengthSquared() > 1)
                {
                    direction.Normalize();

                    direction += new Vector2(UpdateParams.MouseFollowSpeed);
                    direction *= acceleration;
                    Position += direction;
                    Rotation.Radians = VectorToAngle(direction, Rotation.Radians);
                }
            }
            */

            if (Updated != null)
            {
                Updated(this, EventArgs.Empty);
            }
        }
    }
}
