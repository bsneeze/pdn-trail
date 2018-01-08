using System;
using System.Collections.Generic;
using System.Drawing;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;

namespace pyrochild.effects.trail
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class Trail : PropertyBasedEffect
    {
        private double Direction;
        private int Spacing;
        private double SpacingX;
        private double SpacingY;
        private bool Fade;
        private UserBlendOp blendOp;
        private int Distance;
        private double DistanceX;
        private double DistanceY;
        private int DirectionX;
        private bool Bilinear;
        private int DirectionY;

        public enum PropertyNames
        {
            Distance,
            Direction,
            Spacing,
            Fade,
            Bilinear
        }

        public static string StaticName
        {
            get
            {
                string s = "Trail";
#if DEBUG
                s += " BETA";
#endif
                return s;
            }
        }
        public static Image Icon { get { return new Bitmap(typeof(Trail), "icon.png"); } }
        public static string Submenu { get { return "Object"; } }
        public static string WindowName { get { return StaticName + " by pyrochild"; } }

        public Trail()
            : base(StaticName, Icon, Submenu, EffectFlags.Configurable)
        {

        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            var props = new List<Property>();

            props.Add(new Int32Property(PropertyNames.Distance, 25, 1, 500));
            props.Add(new Int32Property(PropertyNames.Spacing, 1, 1, 100));
            props.Add(new DoubleProperty(PropertyNames.Direction, 0, 0, 360));
            props.Add(new BooleanProperty(PropertyNames.Fade, true));
            props.Add(new BooleanProperty(PropertyNames.Bilinear, true));

            return new PropertyCollection(props);
        }

        protected unsafe override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            Surface dst = DstArgs.Surface;
            Surface src = SrcArgs.Surface;
            Rectangle bounds = SrcArgs.Bounds;
            dst.CopySurface(src, renderRects);

            foreach (Rectangle rect in renderRects)
            {
                for (
                    double xoffset = 0, yoffset = 0;
                    xoffset <= DistanceX && yoffset <= DistanceY;
                    xoffset += SpacingX, yoffset += SpacingY)
                {
                    float xdirectedoffset = -(float)xoffset * DirectionX;
                    float ydirectedoffset = -(float)yoffset * DirectionY;

                    if (IsCancelRequested) return;

                    for (int y = rect.Top; y < rect.Bottom; ++y)
                    {
                        float srcy = y + ydirectedoffset;
                        if (!(bounds.Top <= srcy && srcy < bounds.Bottom))
                        {
                            continue;
                        }
                        ColorBgra* dstptr = dst.GetPointAddressUnchecked(rect.Left, y);
                        ColorBgra* srcptr = src.GetPointAddressUnchecked(0, (int)srcy);

                        for (int x = rect.Left; x < rect.Right; ++x)
                        {
                            float srcx = x + xdirectedoffset;
                            if (!(bounds.Left <= srcx && srcx < bounds.Right))
                            {
                                ++dstptr;
                                continue;
                            }

                            ColorBgra srcpixel;
                            if (Bilinear)
                            {
                                srcpixel = src.GetBilinearSample(srcx, srcy);
                            }
                            else
                            {
                                srcpixel = *(srcptr + (int)srcx);
                            }

                            if (Fade)
                            {
                                srcpixel.A = (byte)((srcpixel.A) * (1 - xoffset / DistanceX));
                            }

                            *dstptr = blendOp.Apply(srcpixel, *dstptr);
                            ++dstptr;
                        }
                    }
                }
            }
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);

            this.Direction = newToken.GetProperty<DoubleProperty>(PropertyNames.Direction).Value * Math.PI / 180.0;
            this.Distance = newToken.GetProperty<Int32Property>(PropertyNames.Distance).Value;
            this.Spacing = newToken.GetProperty<Int32Property>(PropertyNames.Spacing).Value;
            this.Fade = newToken.GetProperty<BooleanProperty>(PropertyNames.Fade).Value;
            this.Bilinear = newToken.GetProperty<BooleanProperty>(PropertyNames.Bilinear).Value;

            this.SpacingX = Spacing * Math.Abs(Math.Cos(Direction));
            this.SpacingY = Spacing * Math.Abs(Math.Sin(Direction));
            this.DistanceX = Distance * Math.Abs(Math.Cos(Direction));
            this.DistanceY = Distance * Math.Abs(Math.Sin(Direction));

            this.DirectionX = Math.Sign(Math.Cos(Direction));
            this.DirectionY = -Math.Sign(Math.Sin(Direction));

            this.blendOp = new UserBlendOps.NormalBlendOp();
        }

        protected override void OnCustomizeConfigUIWindowProperties(PropertyCollection props)
        {
            props[ControlInfoPropertyNames.WindowTitle].Value = WindowName;
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo ci = CreateDefaultConfigUI(props);

            ci.SetPropertyControlType(PropertyNames.Direction, PropertyControlType.AngleChooser);
            ci.SetPropertyControlValue(PropertyNames.Bilinear, ControlInfoPropertyNames.DisplayName, "");
            ci.SetPropertyControlValue(PropertyNames.Bilinear, ControlInfoPropertyNames.Description, "High quality");
            ci.SetPropertyControlValue(PropertyNames.Fade, ControlInfoPropertyNames.DisplayName, "");
            ci.SetPropertyControlValue(PropertyNames.Fade, ControlInfoPropertyNames.Description, "Fade out");

            return ci;
        }
    }
}