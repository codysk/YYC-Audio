using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Threading;
using Windows.Storage.Streams;

namespace YYC_Audio
{
    interface ILogger
    {
        void Log(string str);
    }
    struct ShockInfo
    {
        public int Strong;
        public int Freq;
    }
    class YYCController
    {
        private ILogger logger;
        private BluetoothLEDevice yycDevice;
        private string yycDeviceId;
        private SemaphoreSlim​​ singleEntryLock = new SemaphoreSlim​​(1);

        private GattCharacteristic shockCharacteristic;
        public YYCController(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task Connect()
        {
            if (shockCharacteristic != null)
            {
                return;
            }
            await singleEntryLock.WaitAsync();
            DeviceWatcher deviceWatcher = DeviceInformation.CreateWatcher(
                BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                null,
                DeviceInformationKind.AssociationEndpoint
            );
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Start();
            logger.Log("Searching Device YYC-DJ...");

            await singleEntryLock.WaitAsync();

            logger.Log($"Connecting Device YYC-DJ [{yycDeviceId}]...");
            yycDevice = await BluetoothLEDevice.FromIdAsync(yycDeviceId);
            logger.Log("Connected");

            logger.Log("Requesting services...");
            GattDeviceServicesResult result = await yycDevice.GetGattServicesAsync();
            if (result.Status != GattCommunicationStatus.Success)
            {
                throw new Exception("Request services Failed");
            }
            var services = result.Services.ToArray();
            var availableServices = services[1];

            logger.Log("Requesting Characteristics...");
            GattCharacteristicsResult ctrResult = await availableServices.GetCharacteristicsAsync();
            if (ctrResult.Status != GattCommunicationStatus.Success)
            {
                throw new Exception("Request Characteristics Failed");
            }
            var characteristics = ctrResult.Characteristics.ToArray();
            shockCharacteristic = characteristics[0];

            logger.Log("Device Ready!");
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            if (args.Name != "YYC-DJ")
            {
                return;
            }

            logger.Log($"YYC-DJ Found! deviceid: {args.Id}");
            yycDeviceId = args.Id;
            singleEntryLock.Release();
        }

        public async Task Shock(ShockInfo channelA, ShockInfo channelB)
        {
            if (shockCharacteristic == null)
            {
                return;
            }
            // channel A
            var channelAStrongHex = channelA.Strong.ToString("X4");
            var channelAFreqHex = channelA.Freq.ToString("X2");
            string channelAHex = $"35110101{channelAStrongHex}11{channelAFreqHex}32";
            var writerA = new DataWriter();
            writerA.WriteBytes(StringToByteArray(channelAHex));

            // channel B
            var channelBStrongHex = channelB.Strong.ToString("X4");
            var channelBFreqHex = channelB.Freq.ToString("X2");
            string channelBHex = $"35110201{channelBStrongHex}11{channelBFreqHex}32";
            var writerB = new DataWriter();
            writerB.WriteBytes(StringToByteArray(channelBHex));

            logger.Log($"{channelAHex}, {channelBHex}");

            await shockCharacteristic.WriteValueAsync(writerA.DetachBuffer());
            await shockCharacteristic.WriteValueAsync(writerB.DetachBuffer());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

    }
}
