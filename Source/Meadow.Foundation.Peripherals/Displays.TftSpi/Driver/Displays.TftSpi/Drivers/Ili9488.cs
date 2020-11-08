﻿using System.Threading;
using Meadow.Hardware;

namespace Meadow.Foundation.Displays.Tft
{
    public class Ili9488 : DisplayTftSpiBase
    {
        public Ili9488(IIODevice device, ISpiBus spiBus, IPin chipSelectPin, IPin dcPin, IPin resetPin,
            uint width = 320, uint height = 480) : base(device, spiBus, chipSelectPin, dcPin, resetPin, width, height)
        {
            Initialize();

            SetRotation(Rotation.Normal);
        }

        protected override void Initialize()
        {
            SendCommand(0xE0); // Positive Gamma Control
            SendData(0x00);
            SendData(0x03);
            SendData(0x09);
            SendData(0x08);
            SendData(0x16);
            SendData(0x0A);
            SendData(0x3F);
            SendData(0x78);
            SendData(0x4C);
            SendData(0x09);
            SendData(0x0A);
            SendData(0x08);
            SendData(0x16);
            SendData(0x1A);
            SendData(0x0F);

            SendCommand(0XE1); // Negative Gamma Control
            SendData(0x00);
            SendData(0x16);
            SendData(0x19);
            SendData(0x03);
            SendData(0x0F);
            SendData(0x05);
            SendData(0x32);
            SendData(0x45);
            SendData(0x46);
            SendData(0x04);
            SendData(0x0E);
            SendData(0x0D);
            SendData(0x35);
            SendData(0x37);
            SendData(0x0F);

            SendCommand(0XC0); // Power Control 1
            SendData(0x17);
            SendData(0x15);

            SendCommand(0xC1); // Power Control 2
            SendData(0x41);

            SendCommand(0xC5); // VCOM Control
            SendData(0x00);
            SendData(0x12);
            SendData(0x80);

            SendCommand(TFT_MADCTL); // Memory Access Control
            SendData(0x48);          // MX, BGR

            SendCommand(0x3A); // Pixel Interface Format
            /*
             * #if defined (TFT_PARALLEL_8_BIT)
             //   SendData(0x55);  // 16 bit colour for parallel
            #else
            Not currently supported
             */
            SendData(0x66);  // 18 bit colour for SPI

            SendCommand(0xB0); // Interface Mode Control
            SendData(0x00);

            SendCommand(0xB1); // Frame Rate Control
            SendData(0xA0);

            SendCommand(0xB4); // Display Inversion Control
            SendData(0x02);

            SendCommand(0xB6); // Display Function Control
            SendData(0x02);
            SendData(0x02);
            SendData(0x3B);

            SendCommand(0xB7); // Entry Mode Set
            SendData(0xC6);

            SendCommand(0xF7); // Adjust Control 3
            SendData(0xA9);
            SendData(0x51);
            SendData(0x2C);
            SendData(0x82);

            SendCommand(TFT_SLPOUT);  //Exit Sleep
            Thread.Sleep(120);

            SendCommand(TFT_DISPON);  //Display on
            Thread.Sleep(25);

        }

        protected override void SetAddressWindow(uint x0, uint y0, uint x1, uint y1)
        {
            SendCommand((byte)LcdCommand.CASET);  // column addr set
            dataCommandPort.State = Data;
            Write((byte)(x0 >> 8));
            Write((byte)(x0 & 0xff));   // XSTART 
            Write((byte)(x1 >> 8));
            Write((byte)(x1 & 0xff));   // XEND

            SendCommand((byte)LcdCommand.RASET);  // row addr set
            dataCommandPort.State = Data;
            Write((byte)(y0 >> 8));
            Write((byte)(y0 & 0xff));    // YSTART
            Write((byte)(y1 >> 8));
            Write((byte)(y1 & 0xff));    // YEND

            SendCommand((byte)LcdCommand.RAMWR);  // write to RAM
        }

        public void SetRotation(Rotation rotation)
        {
            SendCommand(TFT_MADCTL);

            switch (rotation)
            {
                case Rotation.Normal:
                    SendData(TFT_MAD_MX | TFT_MAD_BGR);
                    break;
                case Rotation.Rotate_90:
                    SendData(TFT_MAD_MV | TFT_MAD_BGR);
                    break;
                case Rotation.Rotate_180:
                    SendData(TFT_MAD_BGR | TFT_MAD_MY);
                    break;
                case Rotation.Rotate_270:
                    SendData(TFT_MAD_BGR | TFT_MAD_MV | TFT_MAD_MX | TFT_MAD_MY);
                    break;
            }
        }

        const byte TFT_NOP = 0x00;
        const byte TFT_SWRST = 0x01;
        const byte TFT_SLPIN = 0x10;
        const byte TFT_SLPOUT = 0x11;
        const byte TFT_INVOFF = 0x20;
        const byte TFT_INVON = 0x21;
        const byte TFT_DISPOFF = 0x28;
        const byte TFT_DISPON = 0x29;
        const byte TFT_CASET = 0x2A;
        const byte TFT_PASET = 0x2B;
        const byte TFT_RAMWR = 0x2C;
        const byte TFT_RAMRD = 0x2E;
        const byte TFT_MADCTL = 0x36;
        const byte TFT_MAD_MY = 0x80;
        const byte TFT_MAD_MX = 0x40;
        const byte TFT_MAD_MV = 0x20;
        const byte TFT_MAD_ML = 0x10;
        const byte TFT_MAD_RGB = 0x00;
        const byte TFT_MAD_BGR = 0x08;
        const byte TFT_MAD_MH = 0x04;
        const byte TFT_MAD_SS = 0x02;
        const byte TFT_MAD_GS = 0x01;

    }
}