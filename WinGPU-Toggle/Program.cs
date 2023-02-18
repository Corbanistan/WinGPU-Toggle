using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Principal;

using WinGPU_Toggle.Utils;

namespace WinGPU_Toggle
{
    internal static class Program
    {
        static readonly string appName = Assembly.GetExecutingAssembly().GetName().Name;
        static readonly Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
        static readonly bool isRunningAsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        static void Main(string[] args)
        {
            PrintBuildInfo();
            PrintHelp();

            while (true)
            {
                Console.Write("Enter command > ");

                switch (Console.ReadLine().ToLower().TrimEnd())
                {
                    case "help":
                        PrintHelp();
                        break;
                    case "gpu":
                        PrintGPUs();
                        break;
                    case "toggle":
                        SelectGPU();
                        break;
                    case "clear":
                        Console.Clear();
                        PrintBuildInfo();
                        break;
                    default:
                        Console.WriteLine("Error: Unknown command!");
                        break;
                }
            }
        }

        static void PrintBuildInfo()
        {
            Console.WriteLine(appName + " " + appVersion + "\n");

            if (!isRunningAsAdmin) { Console.WriteLine("Warning: You must run this program as an administrator to change GPU state!\n"); }
        }

        static void PrintGPUs()
        {
            List<HardwareHelperLib.DEVICE_INFO> gpuList = HardwareHelperLib.GetGPUs();

            Console.WriteLine("\n<< SYSTEM GPUs >>\n");
            foreach (var gpu in gpuList)
            {
                Console.WriteLine(string.Format("\tName: {0}\n\tHardware ID: {1}\n\tStatus: {2}\n", gpu.name, gpu.hardwareId, gpu.status));
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("\n<< HELP >>\n");
            Console.WriteLine("This command line utility allows the user to list and enable / disable the GPUs within their system.");
            Console.WriteLine("\nThe following commands are available:");
            Console.WriteLine("\thelp\tprint help documentation");
            Console.WriteLine("\tgpu\tprint GPUs available in system");
            Console.WriteLine("\ttoggle\ttoggle enable / disable of a GPU");
            Console.WriteLine("\tclear\tclear the console\n");
        }

        static void SelectGPU()
        {
            List<HardwareHelperLib.DEVICE_INFO> gpuList = HardwareHelperLib.GetGPUs();

            int _index = 0;
            HardwareHelperLib.DEVICE_INFO _selectedGPU;

            Console.WriteLine("\n<< SELECT GPU >>\n");

            foreach (var gpu in gpuList)
            {
                Console.WriteLine(string.Format("\t[{0}]\n\tName: {1}\n\tHardware ID: {2}\n\tStatus: {3}\n", _index, gpu.name, gpu.hardwareId, gpu.status));
                _index++;
            }

            if (!isRunningAsAdmin)
            {
                Console.WriteLine("Error: You must run this program as an administrator to change GPU state!\n");
                return;
            }

            if (gpuList.Count < 2)
            {
                Console.WriteLine("Error: GPU count is less than 2! Disabling GPU is not allowed!\n");
                return;
            }

            Console.Write("Selection > ");
            if (int.TryParse(Console.ReadLine(), System.Globalization.NumberStyles.None, null, out int selection) && selection <= gpuList.Count - 1)
            {
                _selectedGPU = gpuList[selection];

                Console.WriteLine("\nYou have selected:\n\t{0}, Status: {1}", _selectedGPU.name, _selectedGPU.status);
                Console.Write("\nEnter 'E' to enable, 'D' to disable, or anything else to cancel: ");

                switch (Console.ReadLine().ToLower().TrimEnd())
                {
                    case "e":
                        SetGPUState(_selectedGPU, true);
                        break;
                    case "d":
                        SetGPUState(_selectedGPU, false);
                        break;
                    default:
                        Console.WriteLine("Operation canceled!");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Error: Invalid selection!");
            }
        }

        static bool SetGPUState(HardwareHelperLib.DEVICE_INFO gpu, bool enable)
        {
            try
            {
                return HardwareHelperLib.SetDeviceState(gpu, enable);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}
