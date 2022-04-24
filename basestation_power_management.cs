using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Valve.VR;
using static Crayon.Output;

namespace basestation_power_management
{
    class Core
    {
        // Put your lighthouse MAC addresses in the array below seperated by commas.
        private static readonly string[] LH_Addresses = new string[] {"D1:57:E0:68:C5:9F","D4:F7:08:5A:48:76"};

        private static bool DEBUG = true;
        private static readonly int RetryAttempts = 2;
        private static int Attempts = 0;
        private static bool CheckEvents = false;


        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int CW_HIDE = 0;
        const int CW_SHOW = 5;

        static void Main(string[] args)
        {
            Console.Title = "Base Station Power Management Console";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            if(args.Contains("debug")) DEBUG = true;
            if(DEBUG == false) ShowWindow(GetConsoleWindow(),CW_HIDE);

            InjectApplication();
            Console.WriteLine("Changing base stations to WAKE state.");
            LighthouseState(true);

            CheckEvents = true;

            while(CheckEvents)
            {
                var vrEvent = new VREvent_t();
                uint eventSize = (uint)Marshal.SizeOf(vrEvent);
                OpenVR.System.PollNextEvent(ref vrEvent,eventSize);

                if((EVREventType)vrEvent.eventType == EVREventType.VREvent_Quit)
                {
                    OpenVR.System.AcknowledgeQuit_Exiting();
                    PrintSuccess("Acknowledge OpenVR quit,","exiting.");
                    OpenVR.Shutdown();
                    Console.WriteLine("Changing base stations to SLEEP state.");
                    LighthouseState(false);
                    CheckEvents = false;
                }
            }

            Console.WriteLine($"{Bold(Magenta("note"))}: OpenVR Session closed, Press ENTER to close progam.");
            if(DEBUG == true) Console.ReadLine();
        }

        private static void PrintError(string process,string status,string error_type)
        {
            Console.WriteLine(Bold().Red($"  error{White(": mainthread-exited-with-error\n")}"));
            Console.WriteLine(Red($"  \u00d7 {process} {White(status)}"));
            Console.WriteLine(Red($"\u00d7 {White($"{error_type}")}\n"));
        }

        private static void PrintSuccess(string process,string status)
        {
            Console.WriteLine(Green($"\u2713 {process} {White(status)}"));
        }

        private static void PrintProcessError(string cmd,string args,int exitcode,string output)
        {
            //ShowWindow(GetConsoleWindow(),CW_SHOW);
            Console.WriteLine(Bold().Red($"  error{White(": subprocess-exited-with-error\n")}"));
            Console.WriteLine(Red($"  \u00d7 {Green($"Running {cmd} {args} {White("did not run successfully.")}")}"));
            Console.WriteLine(Red($"  │ {White($"exit code: {Cyan($"{exitcode}")}")}"));
            Console.WriteLine(Red($"  \u2570\u2500> [{Regex.Matches(output,"\n").Count} lines of output]"));
            Console.WriteLine(output.Replace("\n","\n"+"      "));
            Console.WriteLine(Red("      [end of output]\n"));
            Console.WriteLine(Red($"\u00d7 {White("Encountered error while trying to change basestation state.\n")}"));
            Console.WriteLine(Cyan($"{Bold("hint")}{White(": See above for output from the failure.")}"));
        }

        private static void PrintProcessSuccess(string cmd,string args,int exitcode)
        {
            Console.WriteLine(Bold().Green($"  success{White(": subprocess-exited-without-error\n")}"));
            Console.WriteLine(Green($"  \u2713 Running {cmd} {args} {White("ran successfully.")}"));
            Console.WriteLine(White($"    exit code: {Cyan($"{exitcode}")}"));
        }

        private static void InjectApplication()
        {
            var error = EVRInitError.None;
            OpenVR.Init(ref error,EVRApplicationType.VRApplication_Overlay);

            if(!OpenVR.Applications.IsApplicationInstalled("titus.basestation_power_management"))
            {
                PrintError("OpenVR application manifest","\"titus.basestation_power_management\" does not exist!","Encountered error while checking for application manifest.");
                Console.WriteLine("Installing OpenVR application manifest");
                OpenVR.Applications.AddApplicationManifest(Path.GetFullPath("./app.vrmanifest"),false);
                OpenVR.Applications.SetApplicationAutoLaunch("titus.basestation_power_management",true);
                PrintSuccess("OpenVR application manifest","has been installed.");
            }

            PrintSuccess("OpenVR Application manifest","already installed.");
        }

        private static async void LighthouseState(bool State)
        {
            if(Attempts > RetryAttempts) return;

            for (int i = 0; i < LH_Addresses.GetLength(0); i++)
            {
                Console.WriteLine("Lighthouse ("+LH_Addresses[i]+") state changed to " + (State == true ? "UP" : "DOWN"));
            }

            using (Process Manager = new Process())
            {
                Manager.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                Manager.StartInfo.FileName = @"python3.exe";
                Manager.StartInfo.Arguments = @".\include\lighthouse\lighthouse-v2-manager.py" + (State == true ? " on " : " off ") + String.Join(" ", LH_Addresses);
                Manager.StartInfo.UseShellExecute = false;
                Manager.StartInfo.RedirectStandardOutput = true;
                Manager.StartInfo.CreateNoWindow = true;
                Manager.Start();

                string Stream = Manager.StandardOutput.ReadToEnd();
                Manager.WaitForExit();

                if(Stream.Contains("ERROR")){
                    PrintProcessError($"python3",Manager.StartInfo.Arguments,Manager.ExitCode,Stream);
                    Console.WriteLine((Attempts < RetryAttempts) ? $"{Bold(Magenta("note"))}: Attempting to retry state change. {RetryAttempts-Attempts} attempt(s) remaining." : $"{Bold(Magenta("note"))}: No retry attempts remaining. Ensure your basestations are plugged in, their MAC addresses are correct, and that you have a compatible Bluetooth module installed.");
                    await Task.Delay(1000);
                    Attempts++;
                    LighthouseState(State);
                }
                else
                {
                    PrintProcessSuccess($"python3",Manager.StartInfo.Arguments,Manager.ExitCode);
                    Console.WriteLine("");
                    for (int i = 0; i < LH_Addresses.GetLength(0); i++)
                    {
                        PrintSuccess("Lighthouse ("+LH_Addresses[i]+")","successfully changed state to " + (State == true ? "wake." : "sleep."));
                    }
                }

                Manager.Kill();
            };
        }
    }
}