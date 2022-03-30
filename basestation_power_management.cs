using System.Diagnostics;
using System.Runtime.InteropServices;
using Valve.VR;

namespace basestation_power_management
{
    class Core
    {
        // Put your lighthouse MAC addresses in the array below seperated by commas.
        private static readonly string[] LH_Addresses = new string[] {"D1:57:E0:68:C5:9F","D4:F7:08:5A:48:76"};
        private static bool DEBUG = false;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int CW_HIDE = 0;
        const int CW_SHOW = 5;

        static void Main(string[] args)
        {
            var ConsoleWindow = GetConsoleWindow();
            Console.Title = "Base Station Power Management Console";
            if(args.Contains("debug")) DEBUG = true;
            if(DEBUG == false) ShowWindow(ConsoleWindow,CW_HIDE);

            InjectApplication();
            PrintColor("Putting base stations to WAKE state.",ConsoleColor.Blue);
            LighthouseState(true);
            
            var CheckEvents = true;

            while(CheckEvents)
            {
                var vrEvent = new VREvent_t();
                uint eventSize = (uint)Marshal.SizeOf(vrEvent);
                OpenVR.System.PollNextEvent(ref vrEvent,eventSize);

                if((EVREventType)vrEvent.eventType == EVREventType.VREvent_Quit)
                {
                    OpenVR.System.AcknowledgeQuit_Exiting();
                    PrintColor("[OPENVR] Acknowledge Quit, Exiting.",ConsoleColor.Green);
                    OpenVR.Shutdown();
                    PrintColor("Putting base stations to SLEEP state.",ConsoleColor.Blue);
                    LighthouseState(false);
                    CheckEvents = false;
                }
            }

            PrintColor("Shutting down.. Press ENTER to close.",ConsoleColor.Gray);
            if(DEBUG == true) Console.ReadLine();
        }

        private static void PrintColor(string text,ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private static void InjectApplication()
        {
            var error = EVRInitError.None;
            OpenVR.Init(ref error,EVRApplicationType.VRApplication_Overlay);

            if(!OpenVR.Applications.IsApplicationInstalled("titus.basestation_power_management"))
            {
                PrintColor("[OPENVR] OpenVR application manifest: \"titus.basestation_power_management\" doesnt exist!",ConsoleColor.Red);
                OpenVR.Applications.AddApplicationManifest(Path.GetFullPath("./app.vrmanifest"),false);
                OpenVR.Applications.SetApplicationAutoLaunch("titus.basestation_power_management",true);
                PrintColor("[OPENVR] Injected OpenVR application manifest!",ConsoleColor.Green);
            }

            PrintColor("[OPENVR] Application manifest already injected.",ConsoleColor.Green);
        }

        private static void LighthouseState(bool State)
        {
            for (int i = 0; i < LH_Addresses.GetLength(0); i++)
			{
                PrintColor("[LH] ("+LH_Addresses[i]+") state changed to " + (State == true ? "UP" : "DOWN"),ConsoleColor.Blue);
			}

            using (Process Manager = new Process())
            {
                Manager.StartInfo.FileName = "python3 ./include/lighthouse/lighthouse-v2-manager.py";
                Manager.StartInfo.Arguments = (State == true ? "on " : "off ") + String.Join(" ", LH_Addresses);
                Manager.StartInfo.UseShellExecute = false;
                Manager.StartInfo.RedirectStandardOutput = true;
                Manager.StartInfo.CreateNoWindow = true;
                Manager.Start();

                PrintColor(Manager.StandardOutput.ReadToEnd(),ConsoleColor.Blue);

                Manager.WaitForExit();
            };
        }
    }
}