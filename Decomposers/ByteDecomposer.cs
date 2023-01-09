﻿using System.Diagnostics;

namespace TTestApp.Decomposers
{
    abstract class ByteDecomposer
    {
        private int _zeroLine;

        public const int DataArrSize = 0x100000;
        public int ZeroLine 
        { 
            get { return _zeroLine; } 
            set { _zeroLine = value; }
        }
        public abstract int StartSearchMaxLevel { get; } // Для алгоритма управления накачкой
        public abstract int StopPumpingLevel { get; }    // Для алгоритма управления накачкой
        public abstract int SamplingFrequency { get; }
        public abstract int BaudRate { get; }
        public abstract int BytesInPacket { get; } // Размер посылки
        public abstract int MaxNoDataCounter { get; }

        protected const byte marker1 = 0x19; // Если маркер - 1 байт, используется этот. Если больше, то объявлять свои в наследнике

        protected DataArrays Data;

        public event EventHandler<PacketEventArgs>? OnDecomposePacketEvent;

        public uint MainIndex = 0;
        public int PacketCounter = 0;

        public bool RecordStarted;
        public bool DeviceTurnedOn;

        public int tmpZero;

        protected int tmpValue;

        protected int noDataCounter;

        protected int byteNum;

        //Очереди для усреднения скользящим окном
        protected Queue<int> QueuePressureForDC;
        protected Queue<int> QueuePressureForAC;
        protected Queue<int> QueuePressureForZero;
        protected Queue<int> QueueTemperatureForDC;
        protected Queue<int> QueueTemperatureForAC;
        protected Queue<int> QueueTemperatureForZero;

        protected int sizeQForZero = 10;

        public ByteDecomposer(DataArrays data, int sizeQForDC, int sizeQForAC)
        {
            Data = data;
            RecordStarted = false;
            DeviceTurnedOn = true;
            MainIndex = 0;
            noDataCounter = 0;
            byteNum = 0;
            QueuePressureForDC = new Queue<int>(sizeQForDC);
            QueuePressureForAC = new Queue<int>(sizeQForAC);
            QueuePressureForZero = new Queue<int>(sizeQForZero);
            QueueTemperatureForDC = new Queue<int>(sizeQForDC);
            QueueTemperatureForAC = new Queue<int>(sizeQForAC);
            QueueTemperatureForZero = new Queue<int>(sizeQForZero);
        }

        protected virtual void OnDecomposeLineEvent()
        {
            OnDecomposePacketEvent?.Invoke(
                this,
                new PacketEventArgs
                {
                    DCValue = Data.DCArray[MainIndex],
                    RealTimeValue = Data.RealTimePressureArray[MainIndex],
                    PressureViewValue = Data.PressureViewArray[MainIndex],
                    PacketCounter = PacketCounter,
                    MainIndex = MainIndex
                });
        }

        public int Decompos(USBserialPort usbport, StreamWriter saveFileStream)
        {
            return Decompos(usbport, null, saveFileStream);
        }

        public abstract int Decompos(USBserialPort usbport, Stream saveFileStream, StreamWriter txtFileStream);
        //Возвращает число прочитанных и обработанных байт
    }
}
