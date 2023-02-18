using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

/*
* HardwareHelperLib
* ===========================================================
* Windows XP SP2, VS2005 C#.NET, DotNet 2.0
* HH Lib is a hardware helper for library for C#.
* It can be used for notifications of hardware add/remove
* events, retrieving a list of hardware currently connected,
* and enabling or disabling devices.
* 
* Updated to fit the purpose of working specifically with GPUs.
* Some functionality changed, exception text changed, unneeded
* functionality removed. Tested in .NET Framework 4.8.1
* ===========================================================
* LOG:      Who?    When?       What?
* (v)1.0.0  WJF     11/26/07    Original Implementation
* (v)2.0.0  CMH     02/15/23    Changes/updates to code
*/

namespace WinGPU_Toggle.Utils
{
    public static class HardwareHelperLib
    {
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid gClass, UInt32 iEnumerator, IntPtr hParent, UInt32 nFlags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int SetupDiDestroyDeviceInfoList(IntPtr lpInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr lpInfoSet, UInt32 dwIndex, SP_DEVINFO_DATA devInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryProperty(IntPtr lpInfoSet, SP_DEVINFO_DATA DeviceInfoData, UInt32 Property, UInt32 PropertyRegDataType, StringBuilder PropertyBuffer, UInt32 PropertyBufferSize, IntPtr RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, IntPtr ClassInstallParams, int ClassInstallParamsSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern Boolean SetupDiCallClassInstaller(UInt32 InstallFunction, IntPtr DeviceInfoSet, IntPtr DeviceInfoData);

        // devInst is an uint32 - this matters on 64-bit
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_DevNode_Status(out UInt32 status, out UInt32 probNum, UInt32 devInst, int flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiChangeState(IntPtr deviceInfoSet, [In] ref SP_DEVINFO_DATA deviceInfoData);

        //SP_DEVINFO_DATA
        [StructLayout(LayoutKind.Sequential)]
        public class SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid classGuid;
            public uint devInst;
            public ulong reserved;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class SP_DEVINSTALL_PARAMS
        {
            public int cbSize;
            public int Flags;
            public int FlagsEx;
            public IntPtr hwndParent;
            public IntPtr InstallMsgHandler;
            public IntPtr InstallMsgHandlerContext;
            public IntPtr FileQueue;
            public IntPtr ClassInstallReserved;
            public int Reserved;
            [MarshalAs(UnmanagedType.LPTStr)] public string DriverPath;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
            public int StateChange;
            public int Scope;
            public int HwProfile;
        };

        [StructLayout(LayoutKind.Sequential)]
        public class SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        };

        //PARMS
        public const int DIGCF_ALLCLASSES = (0x00000004);
        public const int DIGCF_PRESENT = (0x00000002);
        public const int INVALID_HANDLE_VALUE = -1;
        public const int SPDRP_DEVICEDESC = (0x00000000);
        public const int SPDRP_HARDWAREID = (0x00000001);
        public const int SPDRP_FRIENDLYNAME = (0x0000000C);
        public const int MAX_DEV_LEN = 1000;
        public const int DIF_PROPERTYCHANGE = (0x00000012);
        public const int DICS_FLAG_GLOBAL = (0x00000001);
        public const int DICS_FLAG_CONFIGSPECIFIC = (0x00000002);
        public const int DICS_ENABLE = (0x00000001);
        public const int DICS_DISABLE = (0x00000002);

        public const int DN_ROOT_ENUMERATED = 0x00000001;   /* Was enumerated by ROOT */
        public const int DN_DRIVER_LOADED = 0x00000002; /* Has Register_Device_Driver */
        public const int DN_ENUM_LOADED = 0x00000004;   /* Has Register_Enumerator */
        public const int DN_STARTED = 0x00000008;   /* Is currently configured */
        public const int DN_MANUAL = 0x00000010;    /* Manually installed */
        public const int DN_NEED_TO_ENUM = 0x00000020;  /* May need reenumeration */
        public const int DN_NOT_FIRST_TIME = 0x00000040;    /* Has received a config */
        public const int DN_HARDWARE_ENUM = 0x00000080; /* Enum generates hardware ID */
        public const int DN_LIAR = 0x00000100;  /* Lied about can reconfig once */
        public const int DN_HAS_MARK = 0x00000200;  /* Not CM_Create_DevNode lately */
        public const int DN_HAS_PROBLEM = 0x00000400;   /* Need device installer */
        public const int DN_FILTERED = 0x00000800;  /* Is filtered */
        public const int DN_MOVED = 0x00001000; /* Has been moved */
        public const int DN_DISABLEABLE = 0x00002000;   /* Can be rebalanced */
        public const int DN_REMOVABLE = 0x00004000; /* Can be removed */
        public const int DN_PRIVATE_PROBLEM = 0x00008000;   /* Has a private problem */
        public const int DN_MF_PARENT = 0x00010000; /* Multi function parent */
        public const int DN_MF_CHILD = 0x00020000;  /* Multi function child */
        public const int DN_WILL_BE_REMOVED = 0x00040000;   /* Devnode is being removed */

        public const int CR_SUCCESS = 0x00000000;

        public enum DeviceStatus
        {
            Unknown,
            Enabled,
            Disabled
        }

        public struct DEVICE_INFO
        {
            public string name;
            public string friendlyName;
            public string hardwareId;
            public string statusstr;
            public DeviceStatus status;
        }

        public static List<DEVICE_INFO> GetGPUs()
        {
            List<DEVICE_INFO> HWList = new List<DEVICE_INFO>();

            try
            {
                Guid myGUID = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318"); // Display adapter class GUID
                IntPtr hDevInfo = SetupDiGetClassDevs(ref myGUID, 0, IntPtr.Zero, DIGCF_PRESENT); // DIGCF_PRESENT
                if (hDevInfo.ToInt64() == INVALID_HANDLE_VALUE)
                    throw new Exception("Invalid Handle");
                SP_DEVINFO_DATA DeviceInfoData = new SP_DEVINFO_DATA();

                //for 32-bit, IntPtr.Size = 4
                //for 64-bit, IntPtr.Size = 8
                if (IntPtr.Size == 4)
                    DeviceInfoData.cbSize = 28;
                else if (IntPtr.Size == 8)
                    DeviceInfoData.cbSize = 32;

                DeviceInfoData.devInst = 0;
                DeviceInfoData.classGuid = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318");
                DeviceInfoData.reserved = 0;
                UInt32 i;
                StringBuilder DeviceName = new StringBuilder("");
                StringBuilder DeviceFriendlyName = new StringBuilder("");
                StringBuilder DeviceHardwareId = new StringBuilder("");
                DeviceName.Capacity = DeviceFriendlyName.Capacity = DeviceHardwareId.Capacity = MAX_DEV_LEN;

                for (i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, DeviceInfoData); i++)
                {
                    DeviceName.Length = DeviceFriendlyName.Length = DeviceHardwareId.Length = 0;

                    if (!SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_DEVICEDESC, 0, DeviceName, MAX_DEV_LEN, IntPtr.Zero))
                        continue;
                    SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_FRIENDLYNAME, 0, DeviceFriendlyName, MAX_DEV_LEN, IntPtr.Zero);
                    SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_HARDWAREID, 0, DeviceHardwareId, MAX_DEV_LEN, IntPtr.Zero);

                    UInt32 status, problem;
                    string dstatustr = "";
                    DeviceStatus deviceStatus = DeviceStatus.Unknown;
                    if (CM_Get_DevNode_Status(out status, out problem, DeviceInfoData.devInst, 0) == CR_SUCCESS)
                        deviceStatus = ((status & DN_STARTED) > 0) ? DeviceStatus.Enabled : DeviceStatus.Disabled;

                    HWList.Add(new DEVICE_INFO { name = DeviceName.ToString(), friendlyName = DeviceFriendlyName.ToString(), hardwareId = DeviceHardwareId.ToString(), status = deviceStatus, statusstr = dstatustr });
                }
                SetupDiDestroyDeviceInfoList(hDevInfo);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to enumerate device tree!", ex);
            }
            return HWList;
        }

        public static bool SetDeviceState(DEVICE_INFO deviceToChangeState, bool bEnable)
        {
            Guid myGUID = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318"); // Display adapter class GUID
            IntPtr hDevInfo = SetupDiGetClassDevs(ref myGUID, 0, IntPtr.Zero, DIGCF_PRESENT); // DIGCF_PRESENT
            if (hDevInfo.ToInt64() == INVALID_HANDLE_VALUE)
                throw new Exception("Could retrieve handle for device!");
            SP_DEVINFO_DATA DeviceInfoData = new SP_DEVINFO_DATA();

            //for 32-bit, IntPtr.Size = 4
            //for 64-bit, IntPtr.Size = 8
            if (IntPtr.Size == 4)
                DeviceInfoData.cbSize = 28;
            else if (IntPtr.Size == 8)
                DeviceInfoData.cbSize = 32;

            //is devices exist for class
            DeviceInfoData.devInst = 0;
            DeviceInfoData.classGuid = new Guid("4D36E968-E325-11CE-BFC1-08002BE10318");
            DeviceInfoData.reserved = 0;
            UInt32 i;
            StringBuilder DeviceHardwareId = new StringBuilder("");
            StringBuilder DeviceFriendlyName = new StringBuilder("");
            DeviceHardwareId.Capacity = DeviceFriendlyName.Capacity = MAX_DEV_LEN;

            for (i = 0; SetupDiEnumDeviceInfo(hDevInfo, i, DeviceInfoData); i++)
            {
                DeviceFriendlyName.Length = DeviceHardwareId.Length = 0;

                //Declare vars
                SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_HARDWAREID, 0, DeviceHardwareId, MAX_DEV_LEN, IntPtr.Zero);
                SetupDiGetDeviceRegistryProperty(hDevInfo, DeviceInfoData, SPDRP_FRIENDLYNAME, 0, DeviceFriendlyName, MAX_DEV_LEN, IntPtr.Zero);

                if (DeviceHardwareId.ToString().IndexOf(deviceToChangeState.hardwareId, StringComparison.OrdinalIgnoreCase) >= 0 && DeviceFriendlyName.ToString().IndexOf(deviceToChangeState.friendlyName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool couldChangeState = ChangeIt(hDevInfo, DeviceInfoData, bEnable);
                    if (!couldChangeState)
                        throw new Exception("Unable to change " + DeviceHardwareId + " device state, make sure you have administrator privileges or are using 64-bit Windows!");
                    break;
                }
            }

            SetupDiDestroyDeviceInfoList(hDevInfo);

            return true;
        }

        private static bool ChangeIt(IntPtr hDevInfo, SP_DEVINFO_DATA devInfoData, bool bEnable)
        {
            try
            {
                //Marshalling vars
                int szOfPcp;
                IntPtr ptrToPcp;
                int szDevInfoData;
                IntPtr ptrToDevInfoData;

                SP_PROPCHANGE_PARAMS pcp = new SP_PROPCHANGE_PARAMS();

                if (bEnable)
                {
                    pcp.ClassInstallHeader.cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                    pcp.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                    pcp.StateChange = DICS_ENABLE;
                    pcp.Scope = DICS_FLAG_GLOBAL;
                    pcp.HwProfile = 0;

                    //Marshal the params
                    szOfPcp = Marshal.SizeOf(pcp);
                    ptrToPcp = Marshal.AllocHGlobal(szOfPcp);
                    Marshal.StructureToPtr(pcp, ptrToPcp, true);
                    szDevInfoData = Marshal.SizeOf(devInfoData);
                    ptrToDevInfoData = Marshal.AllocHGlobal(szDevInfoData);

                    if (SetupDiSetClassInstallParams(hDevInfo, ptrToDevInfoData, ptrToPcp, Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS))))
                    {
                        SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ptrToDevInfoData);
                    }
                    pcp.ClassInstallHeader.cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                    pcp.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                    pcp.StateChange = DICS_ENABLE;
                    pcp.Scope = DICS_FLAG_CONFIGSPECIFIC;
                    pcp.HwProfile = 0;
                }
                else
                {
                    pcp.ClassInstallHeader.cbSize = Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                    pcp.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                    pcp.StateChange = DICS_DISABLE;
                    pcp.Scope = DICS_FLAG_CONFIGSPECIFIC;
                    pcp.HwProfile = 0;
                }
                //Marshal the params
                szOfPcp = Marshal.SizeOf(pcp);
                ptrToPcp = Marshal.AllocHGlobal(szOfPcp);
                Marshal.StructureToPtr(pcp, ptrToPcp, true);
                szDevInfoData = Marshal.SizeOf(devInfoData);
                ptrToDevInfoData = Marshal.AllocHGlobal(szDevInfoData);
                Marshal.StructureToPtr(devInfoData, ptrToDevInfoData, true);

                bool rslt1 = SetupDiSetClassInstallParams(hDevInfo, ptrToDevInfoData, ptrToPcp, Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS)));
                bool rstl2 = SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ptrToDevInfoData);
                if ((!rslt1) || (!rstl2))
                    throw new Exception("Unable to change device state!");
                else
                    return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
