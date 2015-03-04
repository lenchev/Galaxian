using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// Screen buffer class

namespace GalaxianGame
{
    public class ScreenBuffer
    {

        // game window dimensions
        public short bufferWidth = 80;
        public short bufferHeight = 35;
        public SafeFileHandle bufferHandle;
        public CharInfo[] buffer;


        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutput(
          SafeFileHandle hConsoleOutput,
          CharInfo[] lpBuffer,
          Coord dwBufferSize,
          Coord dwBufferCoord,
          ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)]
            public char UnicodeChar;
            [FieldOffset(0)]
            public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)]
            public CharUnion Char;
            [FieldOffset(2)]
            public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        public ScreenBuffer(short bufferWidth, short bufferHeight)
        {
            this.bufferWidth = bufferWidth;
            this.bufferHeight = bufferHeight;

            this.bufferHandle = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (bufferHandle.IsInvalid)
            {
                Console.WriteLine("Buffer handle error!");
                Environment.Exit(0);
            }

            this.buffer = new CharInfo[this.bufferWidth * this.bufferHeight];
        }

        public void AddToBuffer(string strToAdd, short x = 0, short y = 0, ConsoleColor color = ConsoleColor.Gray)
        {
            string newStr = new String(' ', bufferWidth * y + x);
            newStr += strToAdd;
            newStr += new String(' ', this.bufferHeight * this.bufferWidth - x * y - strToAdd.Length);

            for (int i = 0; i < this.bufferHeight * this.bufferWidth; i++)
            {
                // prevents overwrite over the old buffer if not empty
                // not sure if it works correctly
                if (buffer[i].Char.AsciiChar != 0 && newStr[i] == ' ')
                {
                    continue;
                }
                buffer[i].Attributes = (short)color;
                buffer[i].Char.AsciiChar = (byte)newStr[i];
            }

        }

        public void ClearBuffer()
        {
            this.buffer = new CharInfo[this.bufferWidth * this.bufferHeight];
        }

        public void DrawBuffer()
        {
            if (!this.bufferHandle.IsInvalid)
            {
                SmallRect bufferRect = new SmallRect() { Left = 0, Top = 0, Right = this.bufferWidth, Bottom = this.bufferHeight };

                bool drawBuffer = WriteConsoleOutput(this.bufferHandle, this.buffer,
                    new Coord() { X = bufferWidth, Y = this.bufferHeight },
                    new Coord() { X = 0, Y = 0 },
                    ref bufferRect);
            }
        }


    }
}