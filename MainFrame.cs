using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Dsp;
using Windows.Devices.Bluetooth;

namespace YYC_Audio
{
    public enum WidgetStatus
    {
        Running,
        Stopped
    }
    public partial class MainFrame : Form, ILogger
    {
        private WasapiLoopbackCapture cap;
        private YYCController yycController;
        public MainFrame()
        {
            InitializeComponent();
        }

        private void MainFrame_Load(object sender, EventArgs e)
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
            deviceBox.DataSource = devices;
            actButton.Text = actButton_Text;
        }

        public String actButton_Text
        {
            get
            {
                switch (currentStatus)
                {
                    case WidgetStatus.Running:
                        return "Stop";
                    default:
                        return "Start";
                }
            }
        }

        private WidgetStatus currentStatus = WidgetStatus.Stopped;
        private async void actButton_Click(object sender, EventArgs e)
        {
            actButton.Enabled = false;
            switch (currentStatus)
            {

                case WidgetStatus.Running:
                    stop();
                    break;
                case WidgetStatus.Stopped:
                    await start();
                    break;
            }
            actButton.Enabled = true;
        }

        private async Task start()
        {
            currentStatus = WidgetStatus.Running;
            actButton.Text = actButton_Text;

            // connect device
            if (yycController == null)
            {
                yycController = new YYCController(this);
                await yycController.Connect();
            }

            // start capture;
            cap = new WasapiLoopbackCapture(deviceBox.SelectedItem as MMDevice);
            cap.DataAvailable += (sender, e) =>      // 录制数据可用时触发此事件, 参数中包含音频数据
            {
                float[] allSamples = Enumerable      // 提取数据中的采样
                    .Range(0, e.BytesRecorded / 4)   // 除以四是因为, 缓冲区内每 4 个字节构成一个浮点数, 一个浮点数是一个采样
                    .Select(i => BitConverter.ToSingle(e.Buffer, i * 4))  // 转换为 float
                    .ToArray();    // 转换为数组
                                   // 获取采样后, 在这里进行详细处理
                if (allSamples.Length < 2)
                {
                    yycController.Shock(new ShockInfo { Freq = 0, Strong = 0 }, new ShockInfo { Freq = 0, Strong = 0 }).Wait();
                    return;
                }

                int channelCount = cap.WaveFormat.Channels;   // WasapiLoopbackCapture 的 WaveFormat 指定了当前声音的波形格式, 其中包含就通道数
                float[][] channelSamples = Enumerable
                    .Range(0, channelCount)
                    .Select(channel => Enumerable
                        .Range(0, allSamples.Length / channelCount)
                        .Select(i => allSamples[channel + i * channelCount])
                        .ToArray())
                    .ToArray();
                float[] leftChannelSamples = channelSamples[0];
                float[] rightChannelSamples = channelSamples[1];

                var leftFreqs = CalcFreqsDatas(cap, leftChannelSamples);
                var leftVolume = leftFreqs.Max();
                var leftFreq = Array.IndexOf(leftFreqs, leftVolume);
                leftFreq = freqTransformation(leftFreq);
                var leftStrong = strongTransformation(leftVolume);

                var rightFreqs = CalcFreqsDatas(cap, rightChannelSamples);
                var rightVolume = rightFreqs.Max();
                var rightFreq = Array.IndexOf(rightFreqs, rightVolume);
                rightFreq = freqTransformation(rightFreq);
                var rightStrong = strongTransformation(rightVolume);

                Log($"LF: {leftFreq}, LV: {leftStrong}, RF: {rightFreq}, RV: {rightStrong}");
                yycController.Shock(
                    new ShockInfo { Freq = leftFreq, Strong = leftStrong }, 
                    new ShockInfo { Freq = rightFreq, Strong = rightStrong }
                ).Wait();
            };
            cap.StartRecording();
        }

        private int freqTransformation(int freq)
        {
            var ret = freq + 15;
            ret = (ret < 50) ? 50 : ret;
            ret = (ret > 100) ? 100 : ret;
            return ret;
        }

        private int strongTransformation(float strong)
        {
            var ret = Math.Floor(strong / 10);
            return (int)ret;
        }

        public void Log(string str)
        {
            if (logBox.InvokeRequired)
            {
                Action<string> actionDelegate = (x) => { this.logBox.AppendText(x.ToString() + "\r\n"); };
                this.logBox.Invoke(actionDelegate, str);
            }
            else
            {
                this.logBox.AppendText(str.ToString() + "\r\n");
            }
        }

        private float[] CalcFreqsDatas(WasapiLoopbackCapture cap, float[] AverageSamples)
        {
            // 因为对于快速傅里叶变换算法, 需要数据长度为 2 的 n 次方, 这里进行
            int log = (int)Math.Ceiling(Math.Log(AverageSamples.Length, 2));   // 取对数并向上取整
            int newLen = (int)Math.Pow(2, log);                             // 计算新长度
            if (newLen == 0)
            {
                return new float[] { };
            }
            float[] filledSamples = new float[newLen];
            Array.Copy(AverageSamples, filledSamples, AverageSamples.Length);   // 拷贝到新数组
            Complex[] complexSrc = filledSamples
                .Select(v => new Complex() { X = v })        // 将采样转换为复数
                .ToArray();
            FastFourierTransform.FFT(false, log, complexSrc);   // 进行傅里叶变换

            Complex[] halfData = complexSrc
            .Take(complexSrc.Length/2)
            .ToArray();    // 一半的数据
            float[] dftData = halfData
                .Select(v => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y))  // 取复数的模
                .ToArray();    // 将复数结果转换为我们所需要的频率幅度

            // 其实, 到这里你完全可以把这些数据绘制到窗口上, 这已经算是频域图象了, 但是对于音乐可视化来讲, 某些频率的数据我们完全不需要
            // 例如 10000Hz 的频率, 我们完全没必要去绘制它, 取 最小频率 ~ 2500Hz 足矣
            // 对于变换结果, 每两个数据之间所差的频率计算公式为 采样率/采样数, 那么我们要取的个数也可以由 2500 / (采样率 / 采样数) 来得出
            int count = 1100/(cap.WaveFormat.SampleRate / filledSamples.Length);
            float[] finalData = dftData.Take(count).ToArray();
            return finalData;
        }

        private void stop()
        {
            currentStatus = WidgetStatus.Stopped;
            actButton.Text = actButton_Text;

            cap.StopRecording();
        }
    }
}
