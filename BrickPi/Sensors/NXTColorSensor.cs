﻿//////////////////////////////////////////////////////////
// This code has been originally created by Laurent Ellerbach
// It intend to make the excellent BrickPi from Dexter Industries working
// on a RaspberryPi 2 runing Windows 10 IoT Core in Universal
// Windows Platform.
// Credits:
// - Dexter Industries Code
// - MonoBrick for great inspiration regarding sensors implementation in C#
//
// This code is under https://opensource.org/licenses/ms-pl
//
//////////////////////////////////////////////////////////

using BrickPi.Extensions;
using System;

namespace BrickPi.Sensors
{

    public enum ColorSensorMode
    {
        Color = BrickSensorType.COLOR_FULL,
        Reflection = BrickSensorType.COLOR_RED,
        Green = BrickSensorType.COLOR_GREEN,
        Blue = BrickSensorType.COLOR_BLUE,
        Ambient = BrickSensorType.COLOR_NONE
    }

    /// <summary>
    /// Colors that can be read from the EV3 color sensor
    /// </summary>
    public enum Color
    {
#pragma warning disable
        None = 0, Black = 1, Blue = 2, Green = 3,
        Yellow = 4, Red = 5, White = 6, Brown = 7
#pragma warning restore
    };

    /// <summary>
    /// Class that holds RGB colors
    /// </summary>
    public sealed class RGBColor
    {
        private byte red;
        private byte green;
        private byte blue;
        /// <summary>
        /// Initializes a new instance of the <see cref="BrickPi.Sensors.RGBColor"/> class.
        /// </summary>
        /// <param name='red'>
        /// Red value
        /// </param>
        /// <param name='green'>
        /// Green value
        /// </param>
        /// <param name='blue'>
        /// Blue value
        /// </param>
		public RGBColor(byte red, byte green, byte blue) { this.red = red; this.green = green; this.blue = blue; }

        /// <summary>
        /// Gets the red value
        /// </summary>
        /// <value>
        /// The red value
        /// </value>
        public byte Red { get { return red; } }

        /// <summary>
        /// Gets the green value
        /// </summary>
        /// <value>
        /// The green value
        /// </value>
        public byte Green { get { return green; } }

        /// <summary>
        /// Gets the blue value
        /// </summary>
        /// <value>
        /// The blue value
        /// </value>
        public byte Blue { get { return blue; } }
    }

    class NXTColorSensor : SensorNotificationBase, ISensor
    {
        private Brick brick = null;
        private ColorSensorMode colorMode;

        private const int RedIndex = 0;
        private const int GreenIndex = 1;
        private const int BlueIndex = 2;
        private const int BackgroundIndex = 3;


        //private Int16[] colorValues = new Int16[4];
        private Int16[] rawValues = new Int16[4];


        public NXTColorSensor(BrickPortSensor port):this(port,ColorSensorMode.Color)
        { }

        public NXTColorSensor(BrickPortSensor port, ColorSensorMode mode)
        {
            brick = new Brick();
            Port = port;
            colorMode = mode;
            brick.BrickPi.Sensor[(int)Port].Type = (BrickSensorType)mode;
            brick.SetupSensors();
            
        }

        /// <summary>
        /// Update the sensor and this will raised an event on the interface
        /// </summary>
        public void UpdateSensor()
        {
            this.Value = ReadRaw();
            this.ValueAsString = ReadAsString();
        }

        public ColorSensorMode ColorMode
        {
            get { return colorMode; }
            set {
                if (value != colorMode)
                {
                    colorMode = value;
                    brick.BrickPi.Sensor[(int)Port].Type = (BrickSensorType)colorMode;
                    brick.SetupSensors();
                }
            }
        }


        public BrickPortSensor Port
        {
            get; internal set;
        }

        public string GetSensorName()
        {
            return "NXT Color Sensor";
        }

        private void GetRawValues()
        {
            for (int i = 0; i < rawValues.Length; i++)
                rawValues[i] = (short)brick.BrickPi.Sensor[(int)Port].Array[i];

        }

        public int ReadRaw()
        {
            int val = 0;
            switch (colorMode)
            {
                case ColorSensorMode.Color:
                    val = (int)ReadColor();
                    break;
                case ColorSensorMode.Reflection:
                case ColorSensorMode.Green:
                case ColorSensorMode.Blue:
                    val = CalculateRawAverage();
                    break;
                case ColorSensorMode.Ambient:
                    val = CalculateRawAverage();
                    break;
            }
            return val;
        }

        /// <summary>
        /// Read the intensity of the reflected or ambient light in percent. In color mode the color index is returned
        /// </summary>
        public int Read()
        {
            int val = 0;
            switch (colorMode)
            {
                case ColorSensorMode.Ambient:
                    val = CalculateRawAverageAsPct();
                    break;
                case ColorSensorMode.Color:
                    val = (int)ReadColor();
                    break;
                case ColorSensorMode.Reflection:
                    val = CalculateRawAverageAsPct();
                    break;
                default:
                    val = CalculateRawAverageAsPct();
                    break;
            }
            return val;
        }

        private int CalculateRawAverage()
        {
            if (colorMode == ColorSensorMode.Color)
            {
                GetRawValues();
                return (int)(rawValues[RedIndex] + rawValues[BlueIndex] + rawValues[GreenIndex]) / 3;
            } else
                return brick.BrickPi.Sensor[(int)Port].Value;
        }

        protected int CalculateRawAverageAsPct()
        {
            //Need to find out what is the ADC resolution
            //1023 is probablt not the correct one
            return (CalculateRawAverage() * 100) / 1023;
        }

        //public string ReadTest()
        //{
        //    GetRawValues();
        //    string ret = "";
        //    for (int i = 0; i < rawValues.Length; i++)
        //        ret += " " + rawValues[i];
        //    ret += " " + brick.BrickPi.Sensor[(int)Port].Value;
        //    return ret;

        //}

        public string ReadAsString()
        {
            string s = "";
            switch (colorMode)
            {
                case ColorSensorMode.Color:
                    s = ReadColor().ToString();
                    break;
                case ColorSensorMode.Reflection:
                case ColorSensorMode.Green:
                case ColorSensorMode.Blue:
                    s = Read().ToString();
                    break;
                case ColorSensorMode.Ambient:
                    s = Read().ToString();
                    break;
            }

            return s;
        }

        /// <summary>
        /// Reads the color.
        /// </summary>
        /// <returns>The color.</returns>
        public Color ReadColor()
        {
            Color color = Color.None;
            if (colorMode == ColorSensorMode.Color)
            {
                color = (Color)brick.BrickPi.Sensor[(int)Port].Value;
            }
            return color;
        }

        /// <summary>
        /// Reads the color of the RGB.
        /// </summary>
        /// <returns>The RGB color.</returns>
        public RGBColor ReadRGBColor()
        {
            GetRawValues();
            return new RGBColor((byte)rawValues[RedIndex], (byte)rawValues[GreenIndex], (byte)rawValues[BlueIndex]);
        }

        public void SelectNextMode()
        {
            colorMode = ColorMode.Next();
            return;
        }

        public void SelectPreviousMode()
        {
            colorMode = ColorMode.Previous();
            return;
        }

        public int NumberOfModes()
        {
            return Enum.GetNames(typeof(ColorSensorMode)).Length;
        }

        public string SelectedMode()
        {
            return ColorMode.ToString();
        }
    }
}