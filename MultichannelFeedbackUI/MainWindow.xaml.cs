﻿using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultichannelFeedbackUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class ChannelCountChangeHandler
            : IMMNotificationClient
        {
            private static readonly Guid PKEY_THX_fmtid = new Guid("D5E8F0AB-4DE6-4D91-AB21-68868DDA6A4A");
            internal static readonly PropertyKey _PKEY_THX_Content_MaxChannelCount = new PropertyKey(PKEY_THX_fmtid, 11);
            internal static readonly PropertyKey _PKEY_THX_ProcessingChannelCount = new PropertyKey(PKEY_THX_fmtid, 12);

            private readonly string _deviceId;

            public event Action<string>? PropertyValueChanged;

            private uint _ContentMaxChannelCount = 0;
            private uint _ProcessingChannelCount = 0;

            public ChannelCountChangeHandler(MMDevice device)
            {
                _deviceId = device.ID;
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
            }

            public void OnDeviceAdded(string pwstrDeviceId)
            {
            }

            public void OnDeviceRemoved(string deviceId)
            {
            }

            public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
            {
            }

            public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
            {
                if (pwstrDeviceId != _deviceId)
                {
                    return;
                }


                MMDevice device = new MMDeviceEnumerator().GetDevice(_deviceId);

                // Retrieve the PKEY_THX_Content_MaxChannelCount property
                var propValue = device.Properties[key];

                uint val = (propValue == null) ? 0 : (uint)propValue.Value;

                if (_PKEY_THX_Content_MaxChannelCount.Equals(key))
                {
                    _ContentMaxChannelCount = (uint)device.Properties[key].Value;
                }
                else if (_PKEY_THX_ProcessingChannelCount.Equals(key))
                {
                    _ProcessingChannelCount = (uint)device.Properties[key].Value;
                }
                else
                {
                    return;
                }

                string text = $"Content: ({_ContentMaxChannelCount}) {ChannelStatus[_ContentMaxChannelCount]}\nProcessing: ({_ProcessingChannelCount}) {ChannelStatus[_ProcessingChannelCount]}";

                PropertyValueChanged?.Invoke(text);
            }
        }

        public class ViewModel : INotifyPropertyChanged
        {
            private string _statusTextLabel = "Select an audio device";

            public string StatusTextLabel
            {
                get => _statusTextLabel;
                set
                {
                    _statusTextLabel = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusTextLabel)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        private MMDeviceEnumerator _enumerator = new MMDeviceEnumerator();

        private ChannelCountChangeHandler? _notificationClient;

        private static string[] ChannelStatus =
        [
            "No channel information available",
            "Mono",
            "Stereo",
            "2.1",
            "3.1",
            "5.0",
            "5.1",
            "7.0",
            "7.1"
        ];

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModel();
            var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            RenderEndpointSelector.ItemsSource = devices;
        }

        private void RenderEndpointSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_notificationClient != null)
            {
                _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
            }

            MMDevice selectedDevice = (MMDevice)RenderEndpointSelector.SelectedItem;
            
            _notificationClient = new ChannelCountChangeHandler(selectedDevice);
            
            _notificationClient.PropertyValueChanged += (text) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ((ViewModel)this.DataContext).StatusTextLabel = text;
                });
            };
            
            _notificationClient.OnPropertyValueChanged(selectedDevice.ID,
                ChannelCountChangeHandler._PKEY_THX_Content_MaxChannelCount);
            _notificationClient.OnPropertyValueChanged(selectedDevice.ID,
                ChannelCountChangeHandler._PKEY_THX_ProcessingChannelCount);

            _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
        }
    }
}
