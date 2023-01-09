namespace TTestApp.Decomposers
{
    internal class ByteDecomposerADS1115 : ByteDecomposer
    {
        public override int SamplingFrequency => 240;
        public override int BaudRate => 9600;
        public override int BytesInPacket => 3;
        public override int MaxNoDataCounter => 10;
        public override int StartSearchMaxLevel => 20;
        public override int StopPumpingLevel => 30;

        private const int _queueForACSize = 6;
        private const int _queueForDCSize = 60;

        public ByteDecomposerADS1115(DataArrays data) : base(data, _queueForDCSize, _queueForACSize)
        {
            ZeroLine = 960;
        }

        public override int Decompos(USBserialPort usbport, Stream saveFileStream, StreamWriter txtFileStream)
        {
            int bytes = usbport.BytesRead;
            if (bytes == 0)
            {
                noDataCounter++;
                if (noDataCounter > MaxNoDataCounter)
                {
                    DeviceTurnedOn = false;
                }
                return 0;
            }
            DeviceTurnedOn = true;
            if (saveFileStream != null && RecordStarted)
            {
                try
                {
                    saveFileStream.Write(usbport.PortBuf, 0, bytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Save file stream error" + ex.Message);
                }
            }
            for (int i = 0; i < bytes; i++)
            {
                switch (byteNum)
                {
                    case 0:// Marker
                        if (usbport.PortBuf[i] == marker1)
                        {
                            byteNum = 1;
                        }
                        break;
                    case 1:
                        tmpValue = usbport.PortBuf[i];
                        byteNum = 2;
                        break;
                    case 2:
                        tmpValue += 0x100 * usbport.PortBuf[i];
                        if ((tmpValue & 0x8000) != 0)
                        {
                            tmpValue -= 0x10000;
                        }

                        QueuePressureForZero.Enqueue(tmpValue);
                        if (QueuePressureForZero.Count > sizeQForZero)
                        {
                            QueuePressureForZero.Dequeue();
                            tmpZero = (int)QueuePressureForZero.Average();
                        }

                        tmpValue -= ZeroLine;
                        //Очередь для выделения постоянной составляющей
                        QueuePressureForDC.Enqueue(tmpValue);
                        if (QueuePressureForDC.Count > _queueForDCSize)
                        {
                            QueuePressureForDC.Dequeue();
                        }

                        Data.RealTimePressureArray[MainIndex] = tmpValue;
                        //Массив постоянной составляющей
                        Data.DCArray[MainIndex] = (int)QueuePressureForDC.Average();

                        //Очередь - переменная составляющая
                        QueuePressureForAC.Enqueue(tmpValue - (int)QueuePressureForDC.Average());
                        if (QueuePressureForAC.Count > _queueForACSize)
                        {
                            QueuePressureForAC.Dequeue();
                        }

                        //Массив переменной составляющей
                        Data.PressureViewArray[MainIndex] = (int)QueuePressureForAC.Average();
                        byteNum = 3;
                        break;
                    case 3:
                        tmpValue = usbport.PortBuf[i];
                        byteNum = 4;
                        break;
                    case 4:
                        tmpValue += 0x100 * usbport.PortBuf[i];
                        if ((tmpValue & 0x8000) != 0)
                        {
                            tmpValue -= 0x10000;
                        }

                        QueueTemperatureForZero.Enqueue(tmpValue);
                        if (QueueTemperatureForZero.Count > sizeQForZero)
                        {
                            QueueTemperatureForZero.Dequeue();
                            tmpZero = (int)QueueTemperatureForZero.Average();
                        }

                        tmpValue -= ZeroLine;
                        //Очередь для выделения постоянной составляющей
                        QueueTemperatureForDC.Enqueue(tmpValue);
                        if (QueueTemperatureForDC.Count > _queueForDCSize)
                        {
                            QueueTemperatureForDC.Dequeue();
                        }

                        Data.RealTimeTemperatureArray[MainIndex] = tmpValue;
                        //Массив постоянной составляющей
                        Data.DCArray[MainIndex] = (int)QueueTemperatureForDC.Average();

                        //Очередь - переменная составляющая
                        QueueTemperatureForAC.Enqueue(tmpValue - (int)QueueTemperatureForDC.Average());
                        if (QueueTemperatureForAC.Count > _queueForACSize)
                        {
                            QueueTemperatureForAC.Dequeue();
                        }

                        //Массив переменной составляющей
                        Data.TemperatureViewArray[MainIndex] = (int)QueueTemperatureForAC.Average();

                        Data.ViewArray[MainIndex] = Data.PressureViewArray[MainIndex] - Data.TemperatureViewArray[MainIndex];
                        byteNum = 0;

                        if (RecordStarted)
                        {
                            txtFileStream.WriteLine(Data.GetDataString(MainIndex));
                        }
                        OnDecomposeLineEvent();
                        PacketCounter++;
                        MainIndex++;
                        MainIndex &= DataArrSize - 1;
                        break;
                }
            }
            usbport.BytesRead = 0;
            return bytes;
        }
    }
}
