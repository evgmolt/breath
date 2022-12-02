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
        protected Queue<int> QueueForDC;
        protected Queue<int> QueueForAC;
        protected Queue<int> QueueForZero;

        protected int sizeQForZero = 10;

        public ByteDecomposer(DataArrays data, int sizeQForDC, int sizeQForAC)
        {
            Data = data;
            RecordStarted = false;
            DeviceTurnedOn = true;
            MainIndex = 0;
            noDataCounter = 0;
            byteNum = 0;
            QueueForDC = new Queue<int>(sizeQForDC);
            QueueForAC = new Queue<int>(sizeQForAC);
            QueueForZero = new Queue<int>(sizeQForZero);
        }

        protected virtual void OnDecomposeLineEvent()
        {
            OnDecomposePacketEvent?.Invoke(
                this,
                new PacketEventArgs
                {
                    DCValue = Data.DCArray[MainIndex],
                    RealTimeValue = Data.RealTimeArray[MainIndex],
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
