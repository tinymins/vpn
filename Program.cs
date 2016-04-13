using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotRas;
using System.Diagnostics;

namespace VPN {
class Program {
    static void Main(string[] args) {
        // default command is --start
        if (args.Length == 0) args = new string[] { "--start" };

        // if app root exist config file then use it, else use user's personal root
        string file = AppDomain.CurrentDomain.BaseDirectory + "vpn.bin";
        if (!System.IO.File.Exists(file))
            file = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "\\vpn.bin";

        // try to get config file
        Config cfg;
        try {
            cfg = new Config(file);
        } catch (Exception ex) {
            Console.WriteLine("Open config file failed, please check if config file has been occupied by other application.");
            return;
        }

        Console.Title = "VPN Tool";
        // process command
        switch (args[0]) {
        /// help
        case "-h":
        case "--help":
            Console.WriteLine("This application writes for connecting vpns with the function\nof auto switch account.");
            Console.WriteLine("source: https://github.com/tinymins/vpn");
            Console.WriteLine("usage: vpn [--help] [<command>] [<args>]");
            Console.WriteLine("");
            Console.WriteLine("Commands:");
            Console.WriteLine("  1. To config your vpn adapter name:");
            Console.WriteLine("     vpn [-e|--set-entry] YOUR_VPN_CONNECTION_NAME");
            Console.WriteLine("  2. To add or update password of vpn account list:");
            Console.WriteLine("     vpn [-a|--add-account] HOSTNAME USERNAME PASSWORD");
            Console.WriteLine("  3. To remove vpn account:");
            Console.WriteLine("     vpn [-d|--delete-account] HOSTNAME USERNAME");
            Console.WriteLine("  4. To remove all vpn account:");
            Console.WriteLine("     vpn [-c|--clear-account]");
            Console.WriteLine("  5. To list vpn account:");
            Console.WriteLine("     vpn [-l|--list-account]");;
            Console.WriteLine("  6. To connect vpn:");
            Console.WriteLine("     vpn [-s|--start]");
            Console.WriteLine("");
            Console.WriteLine("Samples:");
            Console.WriteLine("  vpn --set-entry MyVPN");
            Console.WriteLine("  vpn --add-account vpn.sample.com root password");
            Console.WriteLine("  vpn --start");
            Console.WriteLine("");
            Console.WriteLine("NOTICE: always remember to input enough arguments.");
            break;
        // set entry
        case "-e":
        case "--set-entry":
            if (CheckArgument(args, 2)) {
                cfg.EntryName = args[1];
            }
            break;
        // add account record
        case "-a":
        case "--add-account":
            if (CheckArgument(args, 4)) {
                cfg.AddVPN(args[1], args[2], args[3]);
            }
            break;
        // delete account record
        case "-d":
        case "--delete-account":
            if (CheckArgument(args, 3)) {
                cfg.DelVPN(args[1], args[2]);
            }
            break;
        // list account record
        case "-l":
        case "--list-account":
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("{0,-30}{1,-16}", "Host", "Username");
            Console.WriteLine("----------------------------------------------");
            foreach (Config.VPN vpn in cfg.GetVPNList()) {
                Console.WriteLine("{0,-30}{1,-16}", vpn.Hostname, vpn.Username);
            }
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine("Entry Name: " + cfg.EntryName);
            Console.WriteLine("----------------------------------------------");
            break;
        // delete all account (clear)
        case "-c":
        case "--clear-account":
            cfg.DelAllVPN();
            break;
        /// start checking connection status and auto connect
        case "-s":
        case "--start":
            if (cfg.EntryName == "" || cfg.GetVPNList().Length == 0) {
                Console.WriteLine("Please config your adapter and vpn account first!");
                Console.WriteLine("Try command --help to see samples.");
            } else {
                Console.WriteLine("Start monitoring vpn connect successfully.");
                VPNHelper vpn = new VPNHelper();
                while (true) {
                    bool bDisconnected = true;
                    foreach (RasConnection conn in RasConnection.GetActiveConnections()) {
                        if (conn.EntryName == cfg.EntryName) bDisconnected = false;
                        // RasConnectionStatus status = conn.GetConnectionStatus();
                        // Console.WriteLine(conn.EntryName);
                        // Console.WriteLine(status.ConnectionState);
                        // Do something useful.
                    }
                    if (bDisconnected) {
                        foreach (Config.VPN rec in cfg.GetVPNList()) {
                            Console.WriteLine("Connecting via " + cfg.EntryName + " to " + rec.Hostname + " with account " + rec.Username + "...");
                            // check if entry is busy
                            while (vpn.IsEntryBusy(cfg.EntryName)) {
                                Console.WriteLine("Entry busy... Retry in 10 sec...");
                                // ipconfig /release
                                // ipconfig /renew
                                Process p = new Process();
                                p.StartInfo.FileName = "cmd.exe"; //exe,bat and so on
                                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                p.StartInfo.Arguments = "/c ipconfig /release && ipconfig /renew";
                                System.Threading.Thread.Sleep(10000);
                            }
                            // try to connect
                            string msg = vpn.Connect(cfg.EntryName, rec.Hostname, rec.Username, rec.Password);
                            if (msg == "") {
                                Console.WriteLine("Connected...");
                                break;
                            } else {
                                Console.WriteLine("Connect failed with information: " + msg );
                            }
                        }
                    }
                    System.Threading.Thread.Sleep(5000);
                }
            }
            break;
        default:
            Console.WriteLine("Bad argument!");
            Console.WriteLine("Try command --help to see samples.");
            break;
        }
    }

    private static bool CheckArgument(string[] args, int p) {
        if (args.Length != p) {
            Console.WriteLine("Wrong number of arguments for command '" + args[0] + "': " + (p - 1) + " expect, " + (args.Length - 1) + " received.");
        }
        return args.Length == p;
    }
}
}
