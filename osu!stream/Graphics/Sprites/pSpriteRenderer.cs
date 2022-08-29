using OpenTK;
using OpenTK.Graphics;
using osum.Helpers;
namespace osum.Graphics.Sprites
{
    internal class pSpriteRenderer : pSprite
    {
        internal pSpriteRenderer(pTexture texture, FieldTypes field, OriginTypes origin, ClockTypes clocking, Vector2 position,
                         float depth, bool alwaysDraw, Color4 colour)
            : base(texture, field, origin, clocking, position, depth, alwaysDraw, colour)
        {
        }

        public event VoidDelegate OnDraw;

        public override bool Draw()
        {
            OnDraw?.Invoke();
            return true;
        }
    }
}

