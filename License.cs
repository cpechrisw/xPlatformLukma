using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace xPlatformLukma
{
    public class License
    {
        //-----
        //-----Globals
        //-----
        private DateTime CurrentDate;
        private string LicFile = "license.ini";
        private DateTime ExpirationDate;
        private string AESKey = "KNugentCWebb2024";

        //-----
        //-----Basic public functions
        //-----

        public License(string licFileDirectory) 
        {
            CurrentDate = DateTime.Today;
            ExpirationDate = CurrentDate;
            ReadLicense(licFileDirectory);

        }
        public void ReadLicense(string licFileDirectory)
        {
            string licFileFullPath = System.IO.Path.Combine(licFileDirectory, LicFile);
            string tmpLicString = "";
            if (File.Exists(licFileFullPath))
            {
                using (StreamReader sr = new(licFileFullPath))
                {
                    while (!sr.EndOfStream)
                    {
                        tmpLicString = sr.ReadLine();
                    }
                }
                ExpirationDate = DecryptLic(tmpLicString);
            }

            

        }

        public bool IsLicValid()
        {
            bool isValid = false;
            int result = DateTime.Compare(ExpirationDate, CurrentDate);
            if (result > 0)
            {
                isValid = true;
            }

            return isValid;
        }

        public void WriteNewLicenseFile(string licFileDirectory, int year)
        {
            string licFileFullPath = System.IO.Path.Combine(licFileDirectory, LicFile);
            string tmpLicString = EncryptLic(year);

            File.WriteAllText(licFileFullPath, tmpLicString);
        }

        public string GetExpirationDate()
        {
            return ExpirationDate.ToShortDateString();
        }
        //-----
        //-----Heler private functions
        //-----
        

        private string EncryptLic(int year)
        {
            ExpirationDate = new DateTime(year, 12, 1);
            string returnString = EncryptString(AESKey, ExpirationDate.ToString());

            return returnString;
        }

        private DateTime DecryptLic(string license)
        {
            DateTime returnDateTime;
            string tmpString = DecryptString(AESKey, license);
            try
            {
                returnDateTime = DateTime.Parse(tmpString);
            }
            catch
            {
                //Not a valid dateTime
                returnDateTime = CurrentDate;
            }

            return returnDateTime;
        }
        public static string EncryptString(string key, string plainText)
        {
            byte[] iv = new byte[16];
            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new())
                {
                    using (CryptoStream cryptoStream = new((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new((Stream)cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        array = memoryStream.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(array);
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] iv = new byte[16];
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new(buffer))
                {
                    using (CryptoStream cryptoStream = new((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new((Stream)cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }




    }
}
