﻿#if iOS || ANDROID
#if iOS
using Foundation;
using ObjCRuntime;
using OpenGLES;
#endif

using OpenTK;
#else
using OpenTK;
#endif
using System;
using osum.Input;
using osum.Input.Sources;
using osum.Support;

namespace osum.Graphics.Sprites
{
    public partial class pDrawable : IDrawable, IDisposable
    {
        internal bool IsClickable => onClick != null;

        internal bool HandleClickOnUp;

        private bool handleInput;

        internal bool HandleInput
        {
            get => handleInput;
            set
            {
                handleInput = value;

                if (IsHovering && handleInput)
                //might have a pending unhover state animation to apply.
                {
                    IsHovering = false;
                    onHoverLost?.Invoke(this, null);
                }
            }
        }

        private event EventHandler onClick;

        internal event EventHandler OnClick
        {
            add
            {
                onClick += value;
                HandleInput = true;
            }
            remove => onClick -= value;
        }

        private event EventHandler onHover;

        internal event EventHandler OnHover
        {
            add
            {
                onHover += value;
                HandleInput = true;
            }
            remove => onHover -= value;
        }

        private event EventHandler onHoverLost;

        internal event EventHandler OnHoverLost
        {
            add => onHoverLost += value;
            remove => onHoverLost -= value;
        }

        internal virtual void UnbindAllEvents()
        {
            onClick = null;
            onHover = null;
            onHoverLost = null;
            IsHovering = false;
            handleInput = false;
        }

        internal bool IsHovering;

        internal int ClickableMargin = 0;

        protected virtual bool checkHover(Vector2 position)
        {
            if (Alpha == 0 || Bypass)
                return false;

            Box2 rect = DisplayRectangle;
            return rect.Left - ClickableMargin < position.X &&
                   rect.Right + ClickableMargin >= position.X &&
                   rect.Top - ClickableMargin < position.Y &&
                   rect.Bottom + ClickableMargin >= position.Y;
        }

        private void inputUpdateHoverState(TrackingPoint trackingPoint)
        {
            if (!handleInput)
                return;

            bool thisIsPreviouslyHovered = trackingPoint.HoveringObject == this;

            bool isNowHovering = (thisIsPreviouslyHovered || !trackingPoint.HoveringObjectConfirmed) && checkHover(trackingPoint.BasePosition);

            if (isNowHovering)
            {
                trackingPoint.HoveringObjectConfirmed = true;
                trackingPoint.HoveringObject = this;
            }
            else if (trackingPoint.HoveringObject == this)
                trackingPoint.HoveringObject = null;

            if (isNowHovering != IsHovering)
            {
                IsHovering = isNowHovering;

                if (IsHovering)
                {
                    onHover?.Invoke(this, null);
                }
                else
                {
                    onHoverLost?.Invoke(this, null);
                }
            }
        }

        private float acceptableUpClick;

        internal virtual void HandleOnMove(InputSource source, TrackingPoint trackingPoint)
        {
            if (!HandleInput) return;

            inputUpdateHoverState(trackingPoint);

            if (acceptableUpClick > 0)
                acceptableUpClick -= Math.Abs(trackingPoint.WindowDelta.X) + Math.Abs(trackingPoint.WindowDelta.Y);
        }

        //todo: make different for different screen resolutions?
        private const float HANDLE_UP_MOVEMENT_ALLOWANCE = 30;

        internal virtual void HandleOnDown(InputSource source, TrackingPoint trackingPoint)
        {
            if (!HandleInput) return;

            inputUpdateHoverState(trackingPoint);

            if (IsHovering)
            {
                if (!HandleClickOnUp)
                    Click();
                else
                    acceptableUpClick = HANDLE_UP_MOVEMENT_ALLOWANCE;
            }
        }

        internal virtual void HandleOnUp(InputSource source, TrackingPoint trackingPoint)
        {
            if (!HandleInput || !IsHovering) return;

            if (acceptableUpClick > 0)
                Click();

            if (HandleInput)
            //check HandleInput again here so we can cancel the unhover for the time being.
            {
                IsHovering = false;
                onHoverLost?.Invoke(this, null);
            }
        }

        internal void Click(bool forceClick = true)
        {
            if (!IsHovering && forceClick)
            {
                //force hovering. this is necessary if a click is manually triggered, to get animations etc.
                IsHovering = true;
                onHover?.Invoke(this, null);
            }

            onClick?.Invoke(this, null);

            acceptableUpClick = 0;
        }
    }
}