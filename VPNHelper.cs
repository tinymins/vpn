using DotRas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VPN {
class VPNHelper {
    private RasDialer Dialer;
    private RasPhoneBook AllUsersPhoneBook;
    public VPNHelper() {
        this.Dialer = new RasDialer();
        this.AllUsersPhoneBook = new RasPhoneBook();
    }

    /// <summary>
    /// Connect vpn
    /// </summary>
    /// <param name="entryname"></param>
    /// <param name="hostname"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public string Connect(string entryname, string hostname, string username, string password) {
        SetEntry(entryname, hostname);
        this.Dialer.EntryName = entryname;
        this.Dialer.PhoneBookPath = AllUsersPhoneBook.Path; //RasPhoneBook.GetPhoneBookPath(RasPhoneBookType.AllUsers);
        try {
            // Set the credentials the dialer should use.
            this.Dialer.Credentials = new NetworkCredential(username, password);

            // NOTE: The entry MUST be in the phone book before the connection can be dialed.
            // Begin dialing the connection; this will raise events from the dialer instance.
            this.Dialer.Dial();
        } catch (Exception ex) {
            return ex.Message;
        }
        return "";
    }

    /// <summary>
    /// Check if entry is busy now.
    /// </summary>
    /// <param name="entryname"></param>
    /// <returns></returns>
    public bool IsEntryBusy(string entryname) {
        // this.Dialer.EntryName = entryname;
        // this.Dialer.PhoneBookPath = AllUsersPhoneBook.Path;
        return this.Dialer.IsBusy;
    }

    /// <summary>
    /// create/update vpn entry
    /// </summary>
    /// <param name="name">name of the connection</param>
    /// <param name="ip">ip address of connection</param>
    public void SetEntry(string name, string ip) {
        // This opens the phonebook so it can be used. Different overloads here will determine where the phonebook is opened/created.
        this.AllUsersPhoneBook.Open(true);
        // check if this entry has already exist
        if (!this.AllUsersPhoneBook.Entries.Contains(name)) {
            // create entry
            if (ip == "") ip = IPAddress.Loopback.ToString();
            // Create the entry that will be used by the dialer to dial the connection. Entries can be created manually, however the static methods on
            // the RasEntry class shown below contain default information matching that what is set by Windows for each platform.
            RasEntry entry = RasEntry.CreateVpnEntry(name, ip, RasVpnStrategy.PptpFirst,
                             RasDevice.GetDeviceByName("(PPTP)", RasDeviceType.Vpn));
            // Add the new entry to the phone book.
            this.AllUsersPhoneBook.Entries.Add(entry);
        } else if (ip != "") {
            // update entry
            this.AllUsersPhoneBook.Entries[name].PhoneNumber = ip;
            this.AllUsersPhoneBook.Entries[name].Update();
        }
    }
}
}
