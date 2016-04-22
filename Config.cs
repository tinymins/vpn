using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VPN
{
    class Config
    {
        private const int ADAPTER_MAX_LENGTH = 128;
        private const int HOSTNAME_MAX_LENGTH = 256;
        private const int USERNAME_MAX_LENGTH = 128;
        private const int PASSWORD_MAX_LENGTH = 128;
        private System.IO.FileStream file;
        private string _EntryName = "";
        private VPN[] VPNs = new VPN[] { };
        public string EntryName
        {
            set
            {
                _EntryName = value;
                WriteConfig();
            }
            get
            {
                return _EntryName;
            }
        }
        public Config(string file)
        {
            this.file = new System.IO.FileStream(
                file,
                System.IO.FileMode.OpenOrCreate,
                System.IO.FileAccess.ReadWrite,
                System.IO.FileShare.None
            );
            ReadConfig();
        }

        /// <summary>
        /// Add or update one vpn config.
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void SetupVPN(string hostname, string username, string password)
        {
            // if exist
            for (int i = 0; i < this.VPNs.Length; i++)
            {
                if (this.VPNs[i].Hostname == hostname && this.VPNs[i].Username == username)
                {
                    this.VPNs[i].Password = password;
                    return;
                }
            }
            // else append record
            VPN[] VPNs = new VPN[this.VPNs.Length + 1];
            this.VPNs.CopyTo(VPNs, 0);
            VPNs[VPNs.Length - 1].Hostname = hostname;
            VPNs[VPNs.Length - 1].Username = username;
            VPNs[VPNs.Length - 1].Password = password;
            this.VPNs = VPNs;
            // save change
            WriteConfig();
        }

        /// <summary>
        /// Delete VPN Config
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="username"></param>
        public void RemoveVPN(string hostname, string username)
        {
            // count exist record
            int nCount = 0;
            for (int i = 0; i < this.VPNs.Length; i++)
            {
                if (this.VPNs[i].Hostname == hostname && this.VPNs[i].Username == username)
                {
                    nCount++;
                }
            }
            // remove exist record
            if (nCount != 0)
            {
                VPN[] VPNs = new VPN[this.VPNs.Length - nCount];
                int n = 0;
                for (int i = 0; i < this.VPNs.Length; i++)
                {
                    if (this.VPNs[i].Hostname != hostname || this.VPNs[i].Username != username)
                    {
                        VPNs[n] = this.VPNs[i];
                        n++;
                    }
                }
                this.VPNs = VPNs;
            }
            // save change
            WriteConfig();
        }

        /// <summary>
        /// Change VPN Index
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="username"></param>
        /// <param name="newindex"></param>
        public void ChangeVPNIndex(string hostname, string username, int newindex)
        {
            // search vpn
            int oldindex = -1;
            for (int i = 0; i < this.VPNs.Length; i++)
            {
                if (this.VPNs[i].Hostname == hostname && this.VPNs[i].Username == username)
                {
                    oldindex = i;
                }
            }
            // change index
            if (oldindex >= 0)
            {
                VPN vpn = this.VPNs[oldindex];
                newindex = Math.Max(Math.Min(newindex, 0), this.VPNs.Length);
                if (oldindex < newindex)
                {
                    for (int i = oldindex; i < newindex; i++)
                        this.VPNs[i] = this.VPNs[i + 1];
                    this.VPNs[newindex] = vpn;
                }
                else if (oldindex > newindex)
                {
                    for (int i = oldindex; i > newindex; i--)
                        this.VPNs[i] = this.VPNs[i - 1];
                    this.VPNs[newindex] = vpn;
                }
            }
            // save change
            WriteConfig();
        }

        /// <summary>
        /// Delete All VPN Config
        /// </summary>
        public void RemoveAllVPN()
        {
            // delete all account
            this.VPNs = new VPN[0];
            // save change
            WriteConfig();
        }

        public VPN[] GetAllVPN()
        {
            return VPNs;
        }

        /// <summary>
        /// load config from config file
        /// </summary>
        private void ReadConfig()
        {
            // read data
            file.Seek(0, SeekOrigin.Begin);
            byte[] byteAll = new byte[file.Length];
            file.Read(byteAll, 0, (int)file.Length); // it's .. not safe ... mark here
            // check signature
            byteAll = DesignArray(byteAll);
            // decrypt data
            byteAll = DecryptArray(byteAll);

            // parse data
            if (byteAll.Length >= ADAPTER_MAX_LENGTH)
            {
                // entry name
                byte[] byteAdapter = new byte[ADAPTER_MAX_LENGTH];
                for (int i = 0; i < byteAdapter.Length; i++)
                {
                    byteAdapter[i] = byteAll[i];
                }
                this._EntryName = System.Text.Encoding.Default.GetString(byteAdapter).Trim(new char[] { '\r', '\n', ' ', '\t', '\0' });
                // accounts
                VPN[] VPNs = new VPN[(byteAll.Length - ADAPTER_MAX_LENGTH) / (HOSTNAME_MAX_LENGTH + USERNAME_MAX_LENGTH + PASSWORD_MAX_LENGTH)];
                for (int i = 0; i < VPNs.Length; i++)
                {
                    int offset = ADAPTER_MAX_LENGTH + i * (HOSTNAME_MAX_LENGTH + USERNAME_MAX_LENGTH + PASSWORD_MAX_LENGTH);
                    byte[] byteHostname = new byte[HOSTNAME_MAX_LENGTH];
                    byte[] byteUsername = new byte[USERNAME_MAX_LENGTH];
                    byte[] bytePassword = new byte[PASSWORD_MAX_LENGTH];
                    for (int j = 0; j < HOSTNAME_MAX_LENGTH; j++)
                        byteHostname[j] = byteAll[offset + j];
                    for (int j = 0; j < USERNAME_MAX_LENGTH; j++)
                        byteUsername[j] = byteAll[offset + HOSTNAME_MAX_LENGTH + j];
                    for (int j = 0; j < PASSWORD_MAX_LENGTH; j++)
                        bytePassword[j] = byteAll[offset + HOSTNAME_MAX_LENGTH + PASSWORD_MAX_LENGTH + j];
                    VPNs[i].Hostname = System.Text.Encoding.Default.GetString(byteHostname).Trim(new char[] { '\r', '\n', ' ', '\t', '\0' });
                    VPNs[i].Username = System.Text.Encoding.Default.GetString(byteUsername).Trim(new char[] { '\r', '\n', ' ', '\t', '\0' });
                    VPNs[i].Password = System.Text.Encoding.Default.GetString(bytePassword).Trim(new char[] { '\r', '\n', ' ', '\t', '\0' });
                }
                this.VPNs = VPNs;
            }
        }

        /// <summary>
        /// save config into config file
        /// </summary>
        private void WriteConfig()
        {
            // clear file
            file.SetLength(0);
            file.Seek(0, SeekOrigin.Begin);
            byte[] buff = new byte[] { };
            // save adapter name
            buff = ConcatByteArray(buff, FillByteArray(System.Text.Encoding.Default.GetBytes(this.EntryName), ADAPTER_MAX_LENGTH));
            // save vpn accounts
            foreach (VPN vpn in this.VPNs)
            {
                buff = ConcatByteArray(buff, FillByteArray(System.Text.Encoding.Default.GetBytes(vpn.Hostname), HOSTNAME_MAX_LENGTH));
                buff = ConcatByteArray(buff, FillByteArray(System.Text.Encoding.Default.GetBytes(vpn.Username), USERNAME_MAX_LENGTH));
                buff = ConcatByteArray(buff, FillByteArray(System.Text.Encoding.Default.GetBytes(vpn.Password), PASSWORD_MAX_LENGTH));
            }
            // encrypt data before save
            buff = EncryptArray(buff);
            // add signature
            buff = EnsignArray(buff);
            // write and flush data
            file.Write(buff, 0, buff.Length);
            file.Flush();
        }

        /// <summary>
        /// Concat two byte array into one
        /// </summary>
        /// <param name="arr1"></param>
        /// <param name="arr2"></param>
        /// <returns></returns>
        private byte[] ConcatByteArray(byte[] arr1, byte[] arr2)
        {
            byte[] arr = new byte[arr1.Length + arr2.Length];
            arr1.CopyTo(arr, 0);
            arr2.CopyTo(arr, arr1.Length);
            return arr;
        }
        /// <summary>
        /// Fill byte array to fixed length
        /// </summary>
        /// <param name="arr">origin array</param>
        /// <param name="len">fixed length</param>
        /// <param name="fill">fill with this char</param>
        /// <returns></returns>
        private byte[] FillByteArray(byte[] arr, int len, byte fill = 0)
        {
            byte[] buff = new byte[len];
            for (int i = 0; i < len; i++)
            {
                buff[i] = fill;
            }
            for (int i = 0; i < Math.Min(arr.Length, len); i++)
            {
                buff[i] = arr[i];
            }
            return buff;
        }

        /// <summary>
        /// The salt to encrypt/decrypt data.
        /// </summary>
        private byte[] CRYPTION_SALT = System.Text.Encoding.Default.GetBytes("WHATTHEFUCKISTHAT!");
        /// <summary>
        /// Encrypt array data before saving to file.
        /// </summary>
        /// <param name="arr">origin array</param>
        /// <returns>encrypted array</returns>
        private byte[] EncryptArray(byte[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int tmp = arr[i];
                tmp = (tmp + CRYPTION_SALT[i % CRYPTION_SALT.Length] + i) % (byte.MaxValue + 1);
                arr[i] = (byte)tmp;
            }
            return arr;
        }

        /// <summary>
        /// Decrypt array data when loading from file.
        /// </summary>
        /// <param name="arr">encrypted array</param>
        /// <returns>origin array</returns>
        private byte[] DecryptArray(byte[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int tmp = arr[i];
                tmp = (tmp - CRYPTION_SALT[i % CRYPTION_SALT.Length] - i) % (byte.MaxValue + 1);
                arr[i] = (byte)tmp;
            }
            return arr;
        }

        /// <summary>
        /// Sign bytes length
        /// </summary>
        private const int SIGN_LENGTH = 4;
        /// <summary>
        /// Sign an array
        /// </summary>
        /// <param name="arr">origin array</param>
        /// <returns>signed array</returns>
        private byte[] EnsignArray(byte[] arr)
        {
            // add signature bits
            return ConcatByteArray(GetSignArray(arr), arr);
        }

        /// <summary>
        /// Check array signature and return origin array, NOTICE if sign check failed, an empty array will be returned.
        /// </summary>
        /// <param name="arr">signed array</param>
        /// <returns>origin array</returns>
        private byte[] DesignArray(byte[] arr)
        {
            if (arr.Length > SIGN_LENGTH)
            {
                // check signature bits
                byte[] buff = new byte[arr.Length - SIGN_LENGTH];
                for (int i = 0; i < buff.Length; i++)
                {
                    buff[i] = arr[i + SIGN_LENGTH];
                }
                byte[] sign = GetSignArray(buff);
                // compare signature array
                bool match = true;
                for (int i = 0; i < sign.Length; i++)
                {
                    if (arr[i] != sign[i])
                    {
                        match = false;
                        break;
                    }
                }
                // if signature matched
                if (match)
                {
                    return buff;
                }
            }
            return new byte[0];
        }

        /// <summary>
        /// Gene signature array from any byte array.
        /// </summary>
        /// <param name="arr">byte array</param>
        /// <returns>signature array</returns>
        private byte[] GetSignArray(byte[] arr)
        {
            // gene signature bits
            byte[] sign = new byte[SIGN_LENGTH];
            for (int i = 0; i < arr.Length; i++)
            {
                sign[i % sign.Length] ^= arr[i];
            }
            return sign;
        }

        /// <summary>
        /// A struct which save one vpn record ip, username and password
        /// </summary>
        public struct VPN
        {
            public string Hostname;
            public string Username;
            public string Password;
        }
    }
}
