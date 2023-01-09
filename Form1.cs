using TTestApp.Decomposers;
using TTestApp.Enums;

namespace TTestApp
{
    public partial class Form1 : Form, IMessageHandler
    {
        USBserialPort USBPort;
        DataArrays? DataA;
        ByteDecomposer Decomposer;
        WaveDetector? Detector;
        CurvesPainter Painter;
        BufferedPanel BufPanel;
        TTestConfig Cfg;
        StreamWriter TextWriter;
        string CurrentFile;
        Patient CurrentPatient;
        int CurrentFileSize;
        const string TmpDataFile = "tmpdata.t";
        int MaxValue = 150;   // Для ADS1115
        bool ViewMode = false;
        int ViewShift;
        double ScaleY = 1;
        List<int[]> VisirList;
        PressureMeasurementStatus PressureMeasStatus = PressureMeasurementStatus.Ready;

        double MaxPressure = 0;
        double MinPressure = 300;

        double MaxDerivValue;
        int BreathVisibleDelay = 50;
        int BreathVisibleCounter;

        public event Action<Message> WindowsMessage;

        public Form1()
        {
            InitializeComponent();
            BufPanel = new BufferedPanel(0);
            BufPanel.MouseMove += BufPanel_MouseMove;
            Cfg = TTestConfig.GetConfig();
            if (Cfg.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                WindowState = FormWindowState.Normal;
                Width = Cfg.WindowWidth;
                Height = Cfg.WindowHeight;
            }
            panelGraph.Dock = DockStyle.Fill;
            panelGraph.Controls.Add(BufPanel);
            BufPanel.Dock = DockStyle.Fill;
            BufPanel.Paint += bufferedPanel_Paint;
            VisirList = new List<int[]>();
            InitArraysForFlow();
            Detector = new WaveDetector();
            Detector.OnWaveDetected += NewWaveDetected;
            USBPort = new USBserialPort(this, Decomposer.BaudRate);
            USBPort.ConnectionOk += OnConnectionOk;
            USBPort.Connect();
        }

        private void NewWaveDetected(object? sender, WaveDetectorEventArgs e)
        {
            BreathVisibleCounter = BreathVisibleDelay;
            double interval = e.Interval; 
            interval /= Decomposer.SamplingFrequency;
            double BreatingRate = e.BreathFreq;
            BreatingRate /= Decomposer.SamplingFrequency;
            BreatingRate = 60 / BreatingRate;
            labBreathFreq.Text = "Interval : " + interval.ToString("0.#");// + " Breathing rate : " + BreatingRate.ToString("0.#");
        }

        private void InitArraysForFlow()
        {
            DataA = new DataArrays(ByteDecomposer.DataArrSize);
            Decomposer = new ByteDecomposerADS1115(DataA);
            Decomposer.OnDecomposePacketEvent += OnPacketReceived;
            Painter = new CurvesPainter(BufPanel, Decomposer);
        }

        private void OnConnectionOk()
        {
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_DEVICECHANGE = 0x0219;
            if (WindowsMessage != null)
            {
                if (m.Msg == WM_DEVICECHANGE)
                {
                    WindowsMessage(m);
                }
            }
            base.WndProc(ref m);
        }


        private void butStopRecord_Click(object sender, EventArgs e)
        {
            progressBarRecord.Visible = false;
            Decomposer.OnDecomposePacketEvent -= OnPacketReceived;
            Decomposer.RecordStarted = false;
            TextWriter?.Dispose();
            ViewMode = true;
            timerPaint.Enabled = !ViewMode;
            timerRead.Enabled = false;
            PressureMeasStatus = (int)PressureMeasurementStatus.Ready;
            CurrentFileSize = Decomposer.PacketCounter;
            labRecordSize.Text = "Record size : " + (CurrentFileSize / Decomposer.SamplingFrequency).ToString() + " s";
            UpdateScrollBar(CurrentFileSize);

            FormPatientData formPatientData = new FormPatientData(CurrentPatient);
            formPatientData.SetStateAfterRecord();
            if (formPatientData.ShowDialog() == DialogResult.OK)
            {
                CurrentPatient = formPatientData.patient;
                SaveFile();
            }
            formPatientData.Dispose();
            BufPanel.Refresh();
        }

        private void butStartRecord_Click(object sender, EventArgs e)
        {
            FormPatientData formPatientData = new(null);
            formPatientData.SetStateBeforeRecord();
            if (formPatientData.ShowDialog() == DialogResult.OK)
            {
                CurrentPatient = formPatientData.patient;
                TextWriter = new StreamWriter(Cfg.DataDir + TmpDataFile);
                Decomposer.PacketCounter = 0;
                Decomposer.MainIndex = 0;
                progressBarRecord.Visible = true;
                FileNum++;
                PressureMeasStatus = PressureMeasurementStatus.Calibration;
            }
            formPatientData.Dispose();
        }
        private void butFlow_Click(object sender, EventArgs e)
        {
            ViewMode = !ViewMode;
            timerRead.Enabled = !ViewMode;
            timerPaint.Enabled = !ViewMode;
            if (!ViewMode)
            {
                InitArraysForFlow();
                hScrollBar1.Visible = false;
            }
        }
        private void hScrollBar1_ValueChanged(object? sender, EventArgs e)
        {
            ViewShift = hScrollBar1.Value;
            BufPanel.Refresh();
        }
        private void trackBarAmp_ValueChanged(object? sender, EventArgs e)
        {
            double a = trackBarAmp.Value;
            ScaleY = Math.Pow(2, a / 2);
            BufPanel.Refresh();
        }
        private void SaveFile()
        {
            Cfg.DataFileNum++;
            TTestConfig.SaveConfig(Cfg);
            CurrentFile = Cfg.Prefix + Cfg.DataFileNum.ToString().PadLeft(5, '0') + ".txt";
            if (File.Exists(Cfg.DataDir + CurrentFile))
            {
                File.Delete(Cfg.DataDir + CurrentFile);
            }
            var DataStrings = File.ReadAllLines(Cfg.DataDir + TmpDataFile);
            File.WriteAllLines(Cfg.DataDir + CurrentFile, CurrentPatient.ToArray());
            File.AppendAllLines(Cfg.DataDir + CurrentFile, DataStrings);
            Text = "Pulse wave recorder. File : " + CurrentFile;
        }
        private void bufferedPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (DataA == null)
            {
                return;
            }
            var ArrayList = new List<double[]>();
            if (ViewMode)
            {
                    ArrayList.Add(DataA.PressureViewArray);
            }
            else
            {
                ArrayList.Add(DataA.PressureViewArray);
                ArrayList.Add(DataA.TemperatureViewArray);
                ArrayList.Add(DataA.ViewArray);
            }
            Painter.Paint(ViewMode, ViewShift, ArrayList, VisirList, ScaleY, MaxValue, e);
        }

        private void UpdateScrollBar(int size)
        {
            hScrollBar1.Maximum = size;
            hScrollBar1.LargeChange = panelGraph.Width - 50;
            hScrollBar1.SmallChange = panelGraph.Width / 10;
            hScrollBar1.AutoSize = true;
            hScrollBar1.Value = 0;
            hScrollBar1.Visible = hScrollBar1.Maximum > hScrollBar1.Width;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            Cfg.Maximized = WindowState == FormWindowState.Maximized;
            Cfg.WindowWidth = Width;
            Cfg.WindowHeight = Height;
            TTestConfig.SaveConfig(Cfg);
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (DataA == null)
            {
                return;
            }
            if (!ViewMode)
            {
                return;
            }
            BufPanel.Refresh();
        }

        private void BufPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!ViewMode)
            {
                return;
            }
            if (DataA == null)
            {
                return;
            }
            double x = e.X + ViewShift;
            if (x > DataA.Size - 1)
            {
                return;
            }

            int index = e.X + ViewShift;
            double sec = index / Decomposer.SamplingFrequency;
        }

        int FileNum = 0;
        private void OnPacketReceived(object? sender, PacketEventArgs e)
        {
            labBreath.Visible = BreathVisibleCounter != 0;
            if (BreathVisibleCounter != 0)
            {
                BreathVisibleCounter--;
            }

            uint currentIndex = (e.MainIndex - 1) & (ByteDecomposer.DataArrSize - 1);
            double CurrentPressure = e.RealTimeValue;
            MaxPressure = (int)Math.Max(CurrentPressure, MaxPressure);            
            if (currentIndex > 0)
            {
                labCurrentPressure.Text = "Current value: " +
                                           e.RealTimeValue.ToString() + " " +
                                           e.PacketCounter.ToString();
//                labCurrentPressure.Text = "Current : " + (DataA.RealTimeArray[Decomposer.MainIndex - 1]).ToString() + " Max : " + 
//                    MaxPressure.ToString();
            }
            int level = (int)Detector.Detect(DataA.PressureViewArray, (int)currentIndex);
            if (PressureMeasStatus == PressureMeasurementStatus.Calibration)
            {
                Decomposer.ZeroLine = Decomposer.tmpZero;
//                Decomposer.RecordStarted = true;
                PressureMeasStatus = PressureMeasurementStatus.Ready;// Measurement;
            }
            if (Decomposer.RecordStarted)
            {
                labRecordSize.Text = "Record size : " + (e.PacketCounter / Decomposer.SamplingFrequency).ToString() + " c";
            }
        }

        private void butCalibration_Click(object sender, EventArgs e)
        {
            PressureMeasStatus = PressureMeasurementStatus.Calibration;
        }
    }
}