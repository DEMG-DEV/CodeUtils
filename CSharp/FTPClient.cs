using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Utils
{
    class FTPClient
    {
        private String _remoteHost;
        private String _remoteUser;
        private String _remotePass;
        private String _remotePrefixFile;
        private String _fileDirectory;

        public FTPClient(string remotehost, string remoteUser, string remotePass, string remotePrefixFile, string fileDirectory)
        {
            _remoteHost = remotehost;
            _remoteUser = remoteUser;
            _remotePass = remotePass;
            _remotePrefixFile = remotePrefixFile;
            _fileDirectory = fileDirectory;
        }

        public FTPClient()
        {
        }

        /// <summary>
        /// List files and folders in a given folder on the server with prefix specified
        /// </summary>
        /// <returns></returns>
        public List<FileDetails> DirectoryListingLastFileWithPrefix()
        {
            List<FileDetails> result = new List<FileDetails>();
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                string pattern = @"^(\d+-\d+-\d+\s+\d+:\d+(?:AM|PM))\s+(<DIR>|\d+)\s+(.+)$";
                Regex regex = new Regex(pattern);
                IFormatProvider culture = CultureInfo.GetCultureInfo("en-US");

                while (!reader.EndOfStream)
                {
                    FileDetails fd = new FileDetails();
                    string line = reader.ReadLine();
                    Match match = regex.Match(line);
                    DateTime modified =
                        DateTime.ParseExact(
                            match.Groups[1].Value, "MM-dd-yy  hh:mmtt", culture, DateTimeStyles.None);
                    long size = (match.Groups[2].Value != "<DIR>") ? long.Parse(match.Groups[2].Value) : 0;
                    string name = match.Groups[3].Value;

                    DateTime now = DateTime.Now.AddMinutes(-15);

                    if (size > 0 && name.Contains(_remotePrefixFile) && modified >= now)
                    {
                        fd.Name = name;
                        fd.Size = size.ToString();
                        fd.DateModified = modified;

                        result.Add(fd);
                    }
                }

                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        /// <summary>
        /// Download a file from the FTP server to the destination
        /// </summary>
        /// <param name="file">filename and path to the file, e.g. public_html/test.zip</param>
        public void Download(string file)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + file);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);

                StreamWriter writer = new StreamWriter(_fileDirectory + file);
                writer.Write(reader.ReadToEnd());

                writer.Close();
                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Get a list of files and folders on the FTP server
        /// </summary>
        /// <returns></returns>
        public List<string> DirectoryListing()
        {
            return DirectoryListing(string.Empty);
        }

        /// <summary>
        /// List files and folders in a given folder on the server
        /// </summary>
        /// <param name="folder">Folder name</param>
        /// <returns></returns>
        public List<string> DirectoryListing(string folder)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + folder);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            List<string> result = new List<string>();

            while (!reader.EndOfStream)
            {
                result.Add(reader.ReadLine());
            }

            reader.Close();
            response.Close();
            return result;
        }

        /// <summary>
        /// Download a file from the FTP server to the destination
        /// </summary>
        /// <param name="filename">filename and path to the file, e.g. public_html/test.zip</param>
        /// <param name="destination">The location to save the file, e.g. c:test.zip</param>
        public void Download(string filename, string destination)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + filename);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            StreamWriter writer = new StreamWriter(destination);
            writer.Write(reader.ReadToEnd());

            writer.Close();
            reader.Close();
            response.Close();
        }

        /// <summary>
        /// Remove a file from the server.
        /// </summary>
        /// <param name="filename">filename and path to the file, e.g. public_html/test.zip</param>
        public void DeleteFileFromServer(string filename)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + filename);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(_remoteUser, _remotePass);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            response.Close();
        }

        /// <summary>
        /// Upload a file to the server
        /// </summary>
        /// <param name="source">Full path to the source file e.g. c:test.zip</param>
        /// <param name="destination">destination folder and filename e.g. public_html/test.zip</param>
        public void UploadFile(string source, string destination)
        {
            string filename = Path.GetFileName(source);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_remoteHost + destination);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(_remoteUser, _remotePass);

            StreamReader sourceStream = new StreamReader(source);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());

            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            response.Close();
            requestStream.Close();
            sourceStream.Close();
        }
    }
}
