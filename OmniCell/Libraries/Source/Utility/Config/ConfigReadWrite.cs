#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace Utility.Config
{
    #region Usings ...

    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public class ConfigReadWrite
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        private static ConfigReadWrite _instance;

        #endregion

        #region Fields

        /// <summary>
        /// </summary>
        private Config _config;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// </summary>
        private ConfigReadWrite()
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public static ConfigReadWrite Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ConfigReadWrite();
                }

                return _instance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// The file that overrides the one in the repository, when it exists.
        /// </summary>
        /// <remarks>
        /// Config.xml is tracked in git and holds placeholders. A running
        /// server needs a database password and the address players connect
        /// to, and neither belongs in a repository - so if this file exists
        /// beside it, it is read instead.
        ///
        /// It is in .gitignore, so the working copy of the tracked file stays
        /// clean and a real password cannot be committed by forgetting. That is
        /// worth a few lines: this repository carried a live database password
        /// in its working copy for weeks, kept out of commits only by a
        /// skip-worktree bit that no clone would have inherited.
        /// </remarks>
        public const string LocalConfig = "Config.local.xml";

        /// <summary>
        /// Which file the settings came from, so the engines can say so.
        /// </summary>
        public string ConfigSource { get; private set; }

        public Config CurrentConfig
        {
            get
            {
                try
                {
                    if (this._config == null)
                    {
                        string path = File.Exists(LocalConfig) ? LocalConfig : "Config.xml";
                        this.ConfigSource = path;

                        this._config =
                            (Config)
                                new XmlSerializer(typeof(Config)).Deserialize(
                                    new MemoryStream(File.ReadAllBytes(path)));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing configuration: {0}", ex.Message);
                    this._config = new Config();
                }

                return this._config;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Saves the current config back to the file
        /// </summary>
        /// <returns>true, if successful</returns>
        public bool SaveConfig()
        {
            if (this._config == null)
            {
                return false;
            }

            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(Config));
                MemoryStream ms = new MemoryStream();
                ser.Serialize(ms, this._config);
                // Back to whichever file it came from. This used to write
                // "config.xml" whatever had been read, so saving on a machine
                // with a local override wrote the operator's settings into the
                // tracked file instead - and the lowercase name is a second
                // file on any disk that cares about case.
                File.WriteAllText(
                    this.ConfigSource ?? "Config.xml",
                    Encoding.UTF8.GetString(ms.GetBuffer()));
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}