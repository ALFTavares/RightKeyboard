using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace RightKeyboard
{
    public partial class MainForm : Form
    {
        private const string LOAD_CONFIG_ERROR_MESSAGE = "Could not load the configuration. Reason: ";

        private IntPtr _hCurrentDevice = IntPtr.Zero;
        private bool _selectingLayout;
        private ushort _currentLayout;

        private readonly LayoutSelectionDialog _layoutSelectionDialog = new LayoutSelectionDialog();
        private readonly Dictionary<IntPtr, ushort> _languageMappings = new Dictionary<IntPtr, ushort>();
        private readonly Dictionary<string, IntPtr> _devicesByName = new Dictionary<string, IntPtr>();

        public MainForm()
        {
            InitializeComponent();

            RAWINPUTDEVICE rawInputDevice = new RAWINPUTDEVICE(1, 6, API.RIDEV_INPUTSINK, this);
            var ok = API.RegisterRawInputDevices(rawInputDevice);

            if (!ok)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            Debug.Assert(ok);

            WindowState = FormWindowState.Minimized;

            LoadDeviceList();
            LoadConfiguration();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SaveConfiguration();
        }

        private void SaveConfiguration()
        {
            try
            {
                var configFilePath = GetConfigFilePath();
                using (TextWriter output = File.CreateText(configFilePath))
                {
                    foreach (var entry in _devicesByName)
                    {
                        if (_languageMappings.TryGetValue(entry.Value, out var layout))
                        {
                            output.WriteLine("{0}={1:X04}", entry.Key, layout);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessages(e.Message);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                var configFilePath = GetConfigFilePath();

                if (!File.Exists(configFilePath))
                    return;

                using (TextReader input = File.OpenText(configFilePath))
                {
                    string line;
                    while ((line = input.ReadLine()) != null)
                    {
                        var parts = line.Split('=');

                        Debug.Assert(parts.Length == 2);

                        var deviceName = parts[0];
                        var layout = ushort.Parse(parts[1], NumberStyles.HexNumber);

                        if (_devicesByName.TryGetValue(deviceName, out var deviceHandle))
                        {
                            _languageMappings.Add(deviceHandle, layout);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorMessages(e.Message);
            }
        }

        private void ErrorMessages(string message)
        {
            MessageBox.Show(LOAD_CONFIG_ERROR_MESSAGE + message);

        }

        private static string GetConfigFilePath()
        {
            string configFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard");
            if (!Directory.Exists(configFileDir))
            {
                Directory.CreateDirectory(configFileDir);
            }

            return Path.Combine(configFileDir, "config.txt");
        }

        private void LoadDeviceList()
        {
            foreach (API.RAWINPUTDEVICELIST rawInputDevice in API.GetRawInputDeviceList())
            {
                if (rawInputDevice.dwType == API.RIM_TYPEKEYBOARD)
                {
                    IntPtr deviceHandle = rawInputDevice.hDevice;
                    string deviceName = API.GetRawInputDeviceName(deviceHandle);
                    _devicesByName.Add(deviceName, deviceHandle);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Hide();
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case API.WM_INPUT:
                    if (!_selectingLayout)
                        ProcessInputMessage(message);
                    break;

                case API.WM_POWERBROADCAST:
                    ProcessPowerMessage(message);
                    break;

                default:
                    base.WndProc(ref message);
                    break;
            }
        }

        private void ProcessPowerMessage(Message message)
        {
            switch (message.WParam.ToInt32())
            {
                case API.PBT_APMQUERYSUSPEND:
                    Debug.WriteLine("PBT_APMQUERYSUSPEND");
                    break;

                case API.PBT_APMQUERYSTANDBY:
                    Debug.WriteLine("PBT_APMQUERYSTANDBY");
                    break;

                case API.PBT_APMQUERYSUSPENDFAILED:
                    Debug.WriteLine("PBT_APMQUERYSUSPENDFAILED");
                    break;

                case API.PBT_APMQUERYSTANDBYFAILED:
                    Debug.WriteLine("PBT_APMQUERYSTANDBYFAILED");
                    break;

                case API.PBT_APMSUSPEND:
                    Debug.WriteLine("PBT_APMSUSPEND");
                    break;

                case API.PBT_APMSTANDBY:
                    Debug.WriteLine("PBT_APMSTANDBY");
                    break;

                case API.PBT_APMRESUMECRITICAL:
                    Debug.WriteLine("PBT_APMRESUMECRITICAL");
                    break;

                case API.PBT_APMRESUMESUSPEND:
                    Debug.WriteLine("PBT_APMRESUMESUSPEND");
                    break;

                case API.PBT_APMRESUMESTANDBY:
                    Debug.WriteLine("PBT_APMRESUMESTANDBY");
                    break;

                case API.PBT_APMBATTERYLOW:
                    Debug.WriteLine("PBT_APMBATTERYLOW");
                    break;

                case API.PBT_APMPOWERSTATUSCHANGE:
                    Debug.WriteLine("PBT_APMPOWERSTATUSCHANGE");
                    break;

                case API.PBT_APMOEMEVENT:
                    Debug.WriteLine("PBT_APMOEMEVENT");
                    break;

                case API.PBT_APMRESUMEAUTOMATIC:
                    Debug.WriteLine("PBT_APMRESUMEAUTOMATIC");
                    break;
            }
        }

        private void ProcessInputMessage(Message message)
        {
            var result = API.GetRawInputData(message.LParam, API.RID_HEADER, out var header);
            Debug.Assert(result != uint.MaxValue);
            if (result == uint.MaxValue)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            if (header.hDevice == _hCurrentDevice)
                return;

            _hCurrentDevice = header.hDevice;
            CurrentDeviceChanged(_hCurrentDevice);
        }

        private void CurrentDeviceChanged(IntPtr currentDevice)
        {
            if (!_languageMappings.TryGetValue(currentDevice, out var layout))
            {
                _selectingLayout = true;
                _layoutSelectionDialog.ShowDialog();
                _selectingLayout = false;
                layout = _layoutSelectionDialog.Layout.Identifier;
                _languageMappings.Add(currentDevice, layout);
            }
            SetCurrentLayout(layout);
            SetDefaultLayout(layout);
        }

        private void SetCurrentLayout(ushort layout)
        {
            if (layout == _currentLayout || layout == 0)
                return;

            _currentLayout = layout;
            var recipients = API.BSM_APPLICATIONS;
            API.BroadcastSystemMessage(API.BSF_POSTMESSAGE, ref recipients, API.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, new IntPtr(layout));
        }

        private static void SetDefaultLayout(ushort layout)
        {
            var hkl = new IntPtr(unchecked((int)((uint)layout << 16 | (uint)layout)));

            var ok = API.SystemParametersInfo(API.SPI_SETDEFAULTINPUTLANG, 0, new[] { hkl }, API.SPIF_SENDCHANGE);

            Debug.Assert(ok);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();
    }
}