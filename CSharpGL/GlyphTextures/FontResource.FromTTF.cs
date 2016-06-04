﻿using SharpFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Linq;

namespace CSharpGL
{
    /// <summary>
    /// 含有字形贴图及其配置信息的单例类型。
    /// </summary>
    public sealed partial class FontResource
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ttfFilename"></param>
        /// <param name="pixelSize">The desired size of the font, in pixels.</param>
        /// <returns></returns>
        public static FontResource Load(string ttfFilename,
            char[] content, int pixelSize = 32)
        {
            FontResource fontResource;
            Load(ttfFilename, content, pixelSize, out fontResource);
            return fontResource;
        }

        private static void Load(string ttfFilename, char[] content, int pixelSize, out FontResource fontResource)
        {
            InitStandardWidths();

            var targets = (from item in content select item).Distinct();

            fontResource = LoadFromSomeChars(ttfFilename, pixelSize, targets);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ttfFilename"></param>
        /// <param name="pixelSize">The desired size of the font, in pixels.</param>
        /// <returns></returns>
        public static FontResource Load(string ttfFilename,
            string content, int pixelSize = 32)
        {
            FontResource fontResource;
            Load(ttfFilename, content, pixelSize, out fontResource);
            return fontResource;
        }

        private static void Load(string ttfFilename, string content, int pixelSize, out FontResource fontResource)
        {
            InitStandardWidths();

            var targets = (from item in content select item).Distinct();

            fontResource = LoadFromSomeChars(ttfFilename, pixelSize, targets);
        }

        private static FontResource LoadFromSomeChars(string ttfFilename, int pixelSize, IEnumerable<char> targets)
        {
            FontResource fontResource;

            int count = targets.Count();
            int maxWidth = GetMaxWidth(pixelSize, count);

            fontResource = new FontResource();
            fontResource.FontHeight = pixelSize;
            var dict = new FullDictionary<char, CharacterInfo>(CharacterInfo.Default);
            fontResource.CharInfoDict = dict;
            var bitmap = new Bitmap(maxWidth, maxWidth, PixelFormat.Format24bppRgb);
            int currentX = 0, currentY = 0;
            Graphics g = Graphics.FromImage(bitmap);
            /*
            this.FontHeight = int.Parse(config.Attribute(strFontHeight).Value);
            this.CharInfoDict = CharacterInfoDictHelper.Parse(
                config.Element(CharacterInfoDictHelper.strCharacterInfoDict));
             */
            using (var file = File.OpenRead(ttfFilename))
            {
                var typeface = new FontFace(file);

                foreach (char c in targets)
                {
                    BlitCharacter(pixelSize, maxWidth, dict, ref currentX, ref currentY, g, typeface, c);
                }
            }
            g.Dispose();
            ShortenBitmap(pixelSize, fontResource, maxWidth, bitmap, currentY);
            bitmap.Dispose();
            return fontResource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ttfFilename"></param>
        /// <param name="pixelSize">The desired size of the font, in pixels.</param>
        /// <returns></returns>
        public static FontResource Load(string ttfFilename,
            char firstChar, char lastChar, int pixelSize = 32)
        {
            FontResource fontResource;
            Load(ttfFilename, firstChar, lastChar, pixelSize, out fontResource);
            return fontResource;
        }

        static int[] standardWidths;
        private static void Load(string ttfFilename, char firstChar, char lastChar, int pixelSize, out FontResource fontResource)
        {
            InitStandardWidths();

            int count = lastChar - firstChar + 1;
            int maxWidth = GetMaxWidth(pixelSize, count);

            fontResource = new FontResource();
            fontResource.FontHeight = pixelSize;
            var dict = new FullDictionary<char, CharacterInfo>(CharacterInfo.Default);
            fontResource.CharInfoDict = dict;
            var bitmap = new Bitmap(maxWidth, maxWidth, PixelFormat.Format24bppRgb);
            int currentX = 0, currentY = 0;
            Graphics g = Graphics.FromImage(bitmap);
            /*
            this.FontHeight = int.Parse(config.Attribute(strFontHeight).Value);
            this.CharInfoDict = CharacterInfoDictHelper.Parse(
                config.Element(CharacterInfoDictHelper.strCharacterInfoDict));
             */
            using (var file = File.OpenRead(ttfFilename))
            {
                var typeface = new FontFace(file);

                for (char c = firstChar; c <= lastChar; c++)
                {
                    BlitCharacter(pixelSize, maxWidth, dict, ref currentX, ref currentY, g, typeface, c);

                    if (c == char.MaxValue) { break; }
                }
            }
            g.Dispose();
            ShortenBitmap(pixelSize, fontResource, maxWidth, bitmap, currentY);
            bitmap.Dispose();
        }

        private static void ShortenBitmap(int pixelSize, FontResource fontResource, int maxWidth, Bitmap bitmap, int currentY)
        {
            var finalBitmap = new Bitmap(maxWidth, currentY + pixelSize + (pixelSize / 10 > 1 ? pixelSize / 10 : 1));
            var g = Graphics.FromImage(finalBitmap);
            g.DrawImage(bitmap, 0, 0);
            g.Dispose();
            //finalBitmap.Save("Test.bmp");
            fontResource.InitTexture(finalBitmap);
            finalBitmap.Dispose();
        }

        private static void BlitCharacter(int pixelSize, int maxWidth, FullDictionary<char, CharacterInfo> dict, ref int currentX, ref int currentY, Graphics g, FontFace typeface, char c)
        {
            if (c == ' ')
            {
                int width = pixelSize / 4;
                if (currentX + width >= maxWidth)
                {
                    currentX = 0;
                    currentY += pixelSize;
                    if (currentY + pixelSize >= maxWidth)
                    { throw new Exception("Texture Size not big enough for reuqired characters."); }
                }
                Bitmap glyphBitmap = new Bitmap(width, pixelSize);
                //float yoffset = pixelSize * 3 / 4 - glyph.HorizontalMetrics.Bearing.Y;
                g.DrawImage(glyphBitmap, currentX, currentY);
                CharacterInfo info = new CharacterInfo(currentX, currentY, width, pixelSize);
                dict.Add(c, info);
                glyphBitmap.Dispose();
                currentX += width;
            }
            else
            {
                Surface surface; Glyph glyph;
                if (RenderGlyph(typeface, c, pixelSize, out surface, out glyph))
                {
                    if (currentX + surface.Width >= maxWidth)
                    {
                        currentX = 0;
                        currentY += pixelSize;
                        if (currentY + pixelSize >= maxWidth)
                        { throw new Exception("Texture Size not big enough for reuqired characters."); }
                    }
                    Bitmap glyphBitmap = GetGlyphBitmap(surface);
                    //float yoffset = pixelSize * 3 / 4 - glyph.HorizontalMetrics.Bearing.Y;
                    g.DrawImage(glyphBitmap, currentX, currentY + pixelSize * 3 / 4 - glyph.HorizontalMetrics.Bearing.Y);
                    CharacterInfo info = new CharacterInfo(currentX, currentY, surface.Width, surface.Height);
                    dict.Add(c, info);
                    glyphBitmap.Dispose();
                    currentX += surface.Width;
                }

                surface.Dispose();
            }
        }

        private static int GetMaxWidth(int pixelSize, int count)
        {
            if (count < 1) { throw new ArgumentException(); }
            int maxWidth = (int)(Math.Sqrt(count) * pixelSize);
            if (maxWidth < pixelSize)
            {
                maxWidth = pixelSize;
            }
            for (int i = 0; i < standardWidths.Length; i++)
            {
                if (maxWidth <= standardWidths[i])
                {
                    maxWidth = standardWidths[i];
                    break;
                }
            }
            return maxWidth;
        }

        private static void InitStandardWidths()
        {
            if (standardWidths == null)
            {
                int[] maxTextureSize = new int[2];
                OpenGL.GetInteger(GetTarget.MaxTextureSize, maxTextureSize);
                if (maxTextureSize[0] == 0) { maxTextureSize[0] = (int)Math.Pow(2, 14); }
                int i = 2;
                List<int> widths = new List<int>();
                while (Math.Pow(2, i) <= maxTextureSize[0])
                {
                    widths.Add((int)Math.Pow(2, i));
                    i++;
                }
                standardWidths = widths.ToArray();
            }
        }


        static unsafe Bitmap GetGlyphBitmap(Surface surface)
        {
            if (surface.Width > 0 && surface.Height > 0)
            {
                var bitmap = new Bitmap(surface.Width, surface.Height, PixelFormat.Format24bppRgb);
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, surface.Width, surface.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                for (int y = 0; y < surface.Height; y++)
                {
                    var dest = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;
                    var src = (byte*)surface.Bits + y * surface.Pitch;

                    for (int x = 0; x < surface.Width; x++)
                    {
                        var b = *src++;
                        *dest++ = b;
                        *dest++ = b;
                        *dest++ = b;
                    }
                }

                bitmap.UnlockBits(bitmapData);
                return bitmap;
            }
            else
            {
                return null;
            }
        }

        private static unsafe bool RenderGlyph(FontFace typeface, char c, int pixelSize, out Surface surface, out Glyph glyph)
        {
            bool result = false;

            glyph = typeface.GetGlyph(c, pixelSize);
            if (glyph != null)
            {
                surface = new Surface
                {
                    Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
                    Width = glyph.RenderWidth,
                    Height = glyph.RenderHeight,
                    Pitch = glyph.RenderWidth
                };

                var stuff = (byte*)surface.Bits;
                // todo: this is not needed?
                for (int i = 0; i < surface.Width * surface.Height; i++)
                    *stuff++ = 0;

                glyph.RenderTo(surface);

                result = true;
            }
            else
            {
                surface = new Surface();
            }

            return result;
        }

    }
}
