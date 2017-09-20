﻿using System;
using System.Drawing;
using System.IO;

namespace TMC
{
    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Tile
    {
        public int TilesetIndex { get; set; } 
        public int PaletteIndex { get; set; }
        public bool FlipX { get; set; }
        public bool FlipY { get; set; }
        
        public Tile()
        {
        	TilesetIndex=0;
        	PaletteIndex=0;
        	FlipX=false;
        	FlipY=false;
        }
    }

    public enum TilemapFormat
    {
        /// <summary>
        /// Text mode, 4 bits per pixel
        /// </summary>
        Text4 = 0x40,
        /// <summary>
        /// Text mode, 8 bits per pixel
        /// </summary>
        Text8 = 0x80,
        /// <summary>
        /// Rotation/scaling mode
        /// </summary>
        RotationScaling,
    }

    // a basic resizable Tilemap
    public class Tilemap
    {
        Tile[] tiles;
        int width, height;

        public Tilemap(int width, int height)
        {
            this.width = width;
            this.height = height;

            tiles = new Tile[width * height];
            for (int i = 0; i < width * height; i++)
                tiles[i] = new Tile();
        }

        public Tilemap(string filename, TilemapFormat format, int width)
        {
            using (var fs = File.OpenRead(filename))
            using (var br = new BinaryReader(fs))
            {
                // --------------------------------
                // number of tiles stored in this file
                int tileCount = (int)br.BaseStream.Length / (format == TilemapFormat.RotationScaling ? 1 : 2);
                Tile t;
                // --------------------------------
                // size of tilemap
                // some tiles could be lost
                this.width = width;
                this.height = tileCount / width;

                // --------------------------------
                tiles = new Tile[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // --------------------------------
                        // Read tile
                        t = new Tile();

                        if (format == TilemapFormat.Text4)
                        {
                            var u = br.ReadUInt16();
                            t.TilesetIndex = u & 0x3FF;
                            t.FlipX = ((u >> 10) & 1) == 1;
                            t.FlipY = ((u >> 11) & 1) == 1;
                            t.PaletteIndex = (u >> 12) & 0xF;
                        }
                        else if (format == TilemapFormat.Text8)
                        {
                            var u = br.ReadUInt16();
                            t.TilesetIndex = u & 0x3FF;
                            t.FlipX = ((u >> 10) & 1) == 1;
                            t.FlipY = ((u >> 11) & 1) == 1;
                        }
                        else
                            t.TilesetIndex = br.ReadByte();

                        tiles[x + y * width] = t;
                    }
                }
            }
        }

        public Tile this[int index]
        {
            get { return tiles[index]; }
            set { tiles[index] = value; }
        }

        public Tile this[int x, int y]
        {
            get { return tiles[x + y * width]; }
            set { tiles[x + y * width] = value; }
        }

        public Tile this[Point p]
        {
            get { return this[p.X, p.Y]; }
            set { this[p.X, p.Y] = value; }
        }

        public void Resize(int newWidth, int newHeight)
        {
            // declare new tile array
            Tile[] newTiles = new Tile[newWidth * newHeight];
            int copyWidth;
            int copyHeight;
            for (int i = 0; i < newWidth * newHeight; i++)
                newTiles[i] = new Tile();

            // the data needed to be copied
             copyWidth = Math.Min(width, newWidth);
             copyHeight = Math.Min(height, newHeight);
            
            // copy tiles over
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    // new (x, y) = old (x, y)
                    newTiles[x + y * newWidth] = tiles[x + y * width];
                }
            }

            // all done
            tiles = newTiles;
            width = newWidth;
            height = newHeight;
        }

        /// <summary>
        /// Save this <see cref="Tilemap"/> in raw GBA format.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        /// <param name="extraBytes"></param>
        public void Save(string filename, TilemapFormat format, int extraBytes = 0)
        {
            // http://problemkaputt.de/gbatek.htm#lcdvrambgscreendataformatbgmap

            // text mode:
            // 0x3FF tiles
            // rotation/scaling mode:
            // 0xFF tiles

            using (var fs = File.Create(filename))
            using (var bw = new BinaryWriter(fs))
            {
                // --------------------------------
                // save tiles
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var t = this[x, y];

                        if (format == TilemapFormat.Text4)
                            bw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11) |
                                (t.PaletteIndex << 12)
                                ));
                        else if (format == TilemapFormat.Text8)
                            bw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11)
                                ));
                        else
                            bw.Write((byte)(t.TilesetIndex & 0xFF));
                    }
                }

                // --------------------------------
                // save extra bytes
                for (int i = 0; i < extraBytes; i++)
                    bw.Write(byte.MinValue);
            }
        }

        /// <summary>
        /// Save this <see cref="Tilemap"/> as C array.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        /// <param name="extraBytes"></param>
        void Save2(string filename, TilemapFormat format, int extraBytes = 0)
        {
            string variableName = Path.GetFileNameWithoutExtension(filename).ToLower().Replace(' ', '_');
            Tile t;
            using (var sw = File.CreateText(filename))
            {
            	sw.Write(String.Format("unsigned char {0}[] = ",variableName));
                sw.Write("{");

                // --------------------------------
                // save tiles
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                         t = this[x, y];

                        if (format == TilemapFormat.Text4)
                            sw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11) |
                                (t.PaletteIndex << 12)
                                ));
                        else if (format == TilemapFormat.Text8)
                            sw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11)
                                ));
                        else
                            sw.Write((byte)(t.TilesetIndex & 0xFF));
                    }
                }

                // --------------------------------
                // save extra bytes
                for (int i = 0; i < extraBytes; i++)
                    sw.Write(byte.MinValue);
            }
        }

        void SaveNSCR(string filename, TilemapFormat bitDepth, int extraBytes)
        {
            // http://llref.emutalk.net/docs/?file=xml/nscr.xml#xml-doc
            // there are actually a lot of options for this format
            // a separate editor may be better, honestly
            Tile t;
            using (var fs = File.Create(filename))
            using (var bw = new BinaryWriter(fs))
            {
                // NSCR header
                bw.Write(0x4E534352);
                bw.Write(0x0100FEFF);
                bw.Write(0);
                bw.Write(16);
                bw.Write(1);

                // NRCS section
                bw.Write(0x4E524353);
                bw.Write(20 + width * height * 2);
                bw.Write((ushort)(width << 3));     // width in pixels
                bw.Write((ushort)(height << 3));    // height in pixels -- max size = 0x1FFF by 0x1FFF
                bw.Write((ushort)0);
                bw.Write((ushort)0);
                bw.Write(width * height * 2);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        t = this[x, y];

                        if (bitDepth == TilemapFormat.Text4)
                            bw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11) |
                                (t.PaletteIndex << 12)
                                ));
                        else
                            bw.Write((ushort)(
                                (t.TilesetIndex & 0x3FF) |
                                (t.FlipX ? 1 : 0 << 10) |
                                (t.FlipY ? 1 : 0 << 11)
                                ));
                    }
                }

                // adjust file size in header
                bw.BaseStream.Position = 8L;
                bw.Write(bw.BaseStream.Length);
            }
        }

        public void ShiftUp()
        {
            for (int y = 1; y < height; y++)
                for (int x = 0; x < width; x++)
                    this[x, y - 1] = this[x, y];

            for (int x = 0; x < width; x++)
                this[x, height - 1] = new Tile();
        }

        public void ShiftDown()
        {
            for (int y = height - 2; y >= 0; y--)
                for (int x = 0; x < width; x++)
                    this[x, y + 1] = this[x, y];

            for (int x = 0; x < width; x++)
                this[x, 0] = new Tile();
        }

        public void ShiftLeft()
        {
            for (int y = 0; y < height; y++)
                for (int x = 1; x < width; x++)
                    this[x - 1, y] = this[x, y];

            for (int y = 0; y < height; y++)
                this[width - 1, y] = new Tile();
        }

        public void ShiftRight()
        {
            for (int y = 0; y < height; y++)
                for (int x = width - 2; x >= 0; x--)
                    this[x + 1, y] = this[x, y];

            for (int y = 0; y < height; y++)
                this[0, y] = new Tile();
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }
    }
}
