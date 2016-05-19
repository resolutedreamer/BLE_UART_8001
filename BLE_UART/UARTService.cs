using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime; // extension method byte[].AsBuffer()
using BLE_UART;


namespace BLE_UART
{
    public class UARTElement
    {
        //idk for now
        public string sent_text { get; set; }

        public override string ToString()
        {
            return sent_text.ToString();
        }
    }

    public delegate void ValueChangeCompletedHandler(UARTElement uartdata);
    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);

    public class UARTService
    {
        // UART Constants
        // 6E400001-B5A3-F393-E0A9-E50E24DCCA9E //for the Service
        // 6E400002-B5A3-F393-E0A9-E50E24DCCA9E //for the TX Characteristic
        // 6E400003-B5A3-F393-E0A9-E50E24DCCA9E //for the RX Characteristic


        // The Characteristic we want to obtain measurements for is the UART characteristic

        public Guid SERVICE_UUID = new Guid("{6E400001-B5A3-F393-E0A9-E50E24DCCA9E}");
        private Guid TX_CHARACTERISTIC_UUID = new Guid("{6E400002-B5A3-F393-E0A9-E50E24DCCA9E}");
        private Guid RX_CHARACTERISTIC_UUID = new Guid("{6E400003-B5A3-F393-E0A9-E50E24DCCA9E}");



        // nRF UART devices has two UART characteristics.

        private const int TX_CHARACTERISTIC_INDEX = 0;
        private const int RX_CHARACTERISTIC_INDEX = 1;


        // Save the last known backpack state into settings when app closes
        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;


        // The UART specification requires that the UART TX characteristic is write without response.
        // The UART specification requires that the UART RX characteristic is notifiable.
        private const GattClientCharacteristicConfigurationDescriptorValue RX_CHARACTERISTIC_NOTIFICATION_TYPE = 
            GattClientCharacteristicConfigurationDescriptorValue.Notify;

        
        private static UARTService instance = new UARTService();
        private GattDeviceService service;

        private GattCharacteristic rx_characteristic;
        private GattCharacteristic tx_characteristic;


        private List<UARTElement> datapoints;
        private PnpObjectWatcher watcher;
        private String deviceContainerId;

        public event ValueChangeCompletedHandler ValueChangeCompleted;
        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;

        public static UARTService Instance
        {
            get { return instance; }
        }

        public bool IsServiceInitialized { get; set; }

        public GattDeviceService Service
        {
            get { return service; }
        }

        public UARTElement[] DataPoints
        {
            get
            {
                UARTElement[] retval;
                lock (datapoints)
                {
                    retval = datapoints.ToArray();
                }

                return retval;
            }
        }


        
        private UARTService()
        {
            datapoints = new List<UARTElement>();
        }

        
        private void App_Resuming(object sender, object e)
        {
            // Since the Windows Runtime will close resources to the device when the app is suspended,
            // the device needs to be reinitialized when the app is resumed.
        }

        private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            IsServiceInitialized = false;

            // This is an appropriate place to save to persistent storage any datapoint the application cares about.
            // Let us save the last known state of the backpack

            var lastItem = datapoints.Last();
            localSettings.Values["last-backpack-state"] = lastItem.ToString();

            // Then clear the information
            datapoints.Clear();

            // Allow the GattDeviceService to get cleaned up by the Windows Runtime.
            // The Windows runtime will clean up resources used by the GattDeviceService object when the application is
            // suspended. The GattDeviceService object will be invalid once the app resumes, which is why it must be 
            // marked as invalid, and reinitalized when the application resumes.
            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            if (rx_characteristic != null)
            {
                rx_characteristic = null;
            }

            if (tx_characteristic != null)
            {
                tx_characteristic = null;
            }



            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }





        }
        
        public async Task InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";
                
                service = await GattDeviceService.FromIdAsync(device.Id);
                if (service != null)
                {
                    IsServiceInitialized = true;
                    await ConfigureServiceForNotificationsAsync();
                }
                else
                {
                    throw new Exception("Something Wrong in 'InitializeServiceAsync' ");
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Configure the Bluetooth device to send notifications whenever the Characteristic value changes
        /// </summary>
        private async Task ConfigureServiceForNotificationsAsync()
        {
            try
            {
                //*** replace this *********************

                // Obtain the characteristic for which notifications are to be received
                /*var characteristic_list = service.GetAllCharacteristics();
                int num_characteristics_found = characteristic_list.Count;
                if (num_characteristics_found == 0)
                {
                    throw new Exception("No Characteristics for this service");
                }
                else
                {
                    var enumeratorable_list = characteristic_list.GetEnumerator();
                    enumeratorable_list.MoveNext();
                    for (int i = 0; i < num_characteristics_found; i++)
                    {
                        var current_characteristic = enumeratorable_list.Current;
 
                        if (i == 0)
                        {
                            tx_characteristic = current_characteristic;
 
                            // do not try to set the gatt protection level on a write characterisitc
                            // bad things will happen to you
                            //tx_characteristic.ProtectionLevel = GattProtectionLevel.Plain;
                        }
                        else if (i == 1)
                        {
                            rx_characteristic = current_characteristic;
                            // now for this notify characteristic, it seems to the the case that it is
                            // a good idea for the level to be plain
                            rx_characteristic.ProtectionLevel = GattProtectionLevel.Plain;
                        }
                        enumeratorable_list.MoveNext();
                    }
                }*/

                //*** with this ***************

                tx_characteristic = service.GetCharacteristics(new Guid("6E400002-B5A3-F393-E0A9-E50E24DCCA9E")).First();
                rx_characteristic = service.GetCharacteristics(new Guid("6E400003-B5A3-F393-E0A9-E50E24DCCA9E")).First();

                await rx_characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                //*****************************

                try
                {
                    // Register the event handler for receiving notifications
                    rx_characteristic.ValueChanged += Characteristic_ValueChanged;
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                }

                // In order to avoid unnecessary communication with the device, determine if the device is already
                // correctly configured to send notifications.
                // By default ReadClientCharacteristicConfigurationDescriptorAsync will attempt to get the current
                // value from the system cache and communication with the device is not typically required.
                var currentDescriptorValue = await rx_characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();

                bool a = (currentDescriptorValue.Status != GattCommunicationStatus.Success);
                bool b = (currentDescriptorValue.ClientCharacteristicConfigurationDescriptor != RX_CHARACTERISTIC_NOTIFICATION_TYPE);

                if (a || b)
                {
                    // Set the Client Characteristic Configuration Descriptor to enable the device to send notifications
                    // when the Characteristic value changes
                    GattCommunicationStatus status =
                        await rx_characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        RX_CHARACTERISTIC_NOTIFICATION_TYPE);

                    if (status == GattCommunicationStatus.Unreachable)
                    {
                        // Register a PnpObjectWatcher to detect when a connection to the device is established,
                        // such that the application can retry device configuration.
                        StartDeviceConnectionWatcher();
                    }
                }
            }
            catch (Exception)
            {

            }
       }

        /// <summary>
        /// Register to be notified when a connection is established to the Bluetooth device
        /// </summary>
        private void StartDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer,
                new string[] { "System.Devices.Connected" }, String.Empty);

            watcher.Updated += DeviceConnection_Updated;
            watcher.Start();
        }

        /// <summary>
        /// Invoked when a connection is established to the Bluetooth device
        /// </summary>
        /// <param name="sender">The watcher object that sent the notification</param>
        /// <param name="args">The updated device object properties</param>
        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var connectedProperty = args.Properties["System.Devices.Connected"];
            bool isConnected = false;
            if ((deviceContainerId == args.Id) && Boolean.TryParse(connectedProperty.ToString(), out isConnected) &&
                isConnected)
            {
                var status = await rx_characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    RX_CHARACTERISTIC_NOTIFICATION_TYPE);

                if (status == GattCommunicationStatus.Success)
                {
                    IsServiceInitialized = true;

                    // Once the Client Characteristic Configuration Descriptor is set, the watcher is no longer required
                    watcher.Stop();
                    watcher = null;
                }

                // Notifying subscribers of connection state updates
                if (DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(isConnected);
                }
            }
        }

        /// <summary>
        /// Invoked when Windows receives data from your Bluetooth device.
        /// </summary>
        /// <param name="sender">The GattCharacteristic object whose value is received.</param>
        /// <param name="args">The new characteristic value sent by the device.</param>
        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];

            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            // Process the raw data received from the device.
            var astring = UART_Data_Converter.RX(data);

            var value = new UARTElement
            {
                sent_text = astring
            };

            lock (datapoints)
            {
                datapoints.Add(value);
            }

            if (ValueChangeCompleted != null)
            {
                ValueChangeCompleted(value);
            }
        }

        // Set the alert-level characteristic on the remote device
        public async Task UART_Transmit(string user_input, int inputlength)
        {
            // try-catch block protects us from the race where the device disconnects
            // just after we've determined that it is connected.
            try
            {
                // use the converter class to convert the string into bytes
                byte[] data = UART_Data_Converter.TX(user_input, inputlength);
                await tx_characteristic.WriteValueAsync(data.AsBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            catch (Exception)
            {
                // ignore exception
            }
        }



    }

    public sealed class UART_Data_Converter
    {
        public static byte[] TX(string transmit_this, int length)
        {
            byte[] data = null;
            data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                char letter = transmit_this[i];
                data[i] = Convert.ToByte(letter);
            }
            return data;
        }
        public static string RX(byte[] recived_this)
        {
            int length = recived_this.Length;
            string datastring = "";

            for (int i = 0; i < length; i++)
            {
                char temp = (char)recived_this[i];
                datastring += temp;
            }
            return datastring;
        }        
    }
}
